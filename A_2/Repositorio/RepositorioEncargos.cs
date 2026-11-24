namespace A_2.Repositorio;

using Microsoft.EntityFrameworkCore;
using A_2.Data;
using A_2.Models;

public class RepositorioEncargos : IRepositorioEncargos
{
    private readonly IDbContextFactory<TallerDbContext> _dbFactory;

    public RepositorioEncargos(IDbContextFactory<TallerDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<Encargo>> GetAll()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Encargos
            .AsNoTracking()
            .Include(e => e.EncargoHerramientas!)
                .ThenInclude(eh => eh.Herramienta)
            .OrderBy(e => e.Fecha)   // columna 'date': orden cronológico real
            .ThenBy(e => e.Hora)     // columna 'time'
            .ToListAsync();
    }

    public async Task<Encargo?> Get(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Encargos
            .AsNoTracking()
            .Include(e => e.EncargoHerramientas!)
                .ThenInclude(eh => eh.Herramienta)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Encargo> Add(Encargo entity, List<(int HerramientaId, int Cantidad)> herramientas)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var nuevas = ADiccionario(herramientas);
        var herrs = await AplicarCambiosDeInventario(db, encargoId: 0,
            viejas: new Dictionary<int, int>(), nuevas: nuevas);

        // El encargo y sus relaciones se insertan juntos en un solo SaveChanges (una sola transacción):
        // EF asigna el EncargoId a cada relación después de insertar el encargo.
        entity.Id = 0;
        entity.IsCompleted = false;
        entity.EncargoHerramientas = nuevas
            .Select(kv => new EncargoHerramienta
            {
                HerramientaId = kv.Key,
                CantidadUsada = herrs[kv.Key].EsMultiple ? kv.Value : 0
            })
            .ToList();

        await db.Encargos.AddAsync(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task Update(Encargo entity, List<(int HerramientaId, int Cantidad)> herramientas)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var actual = await db.Encargos
            .Include(e => e.EncargoHerramientas)
            .FirstOrDefaultAsync(e => e.Id == entity.Id)
            ?? throw new InvalidOperationException("El encargo ya no existe.");

        if (actual.IsCompleted)
        {
            throw new InvalidOperationException("No se puede editar un encargo completado.");
        }

        // Datos generales del encargo (solo escalares; el estado de completado no se toca aquí).
        actual.Cliente = entity.Cliente;
        actual.TipoTrabajo = entity.TipoTrabajo;
        actual.Fecha = entity.Fecha;
        actual.Hora = entity.Hora;

        var relaciones = actual.EncargoHerramientas ??= new List<EncargoHerramienta>();
        var viejas = relaciones.ToDictionary(r => r.HerramientaId, r => r.CantidadUsada);
        var nuevas = ADiccionario(herramientas);

        var herrs = await AplicarCambiosDeInventario(db, actual.Id, viejas, nuevas);

        // Sincroniza la tabla intermedia sin borrar y reinsertar la misma clave:
        // se eliminan las que ya no están, se actualizan las que siguen y se agregan las nuevas.
        foreach (var rel in relaciones.ToList())
        {
            if (nuevas.TryGetValue(rel.HerramientaId, out var cant))
            {
                rel.CantidadUsada = herrs[rel.HerramientaId].EsMultiple ? cant : 0;
            }
            else
            {
                db.EncargoHerramientas.Remove(rel);
            }
        }

        foreach (var (herrId, cant) in nuevas.Where(kv => !viejas.ContainsKey(kv.Key)).Select(kv => (kv.Key, kv.Value)))
        {
            relaciones.Add(new EncargoHerramienta
            {
                EncargoId = actual.Id,
                HerramientaId = herrId,
                CantidadUsada = herrs[herrId].EsMultiple ? cant : 0
            });
        }

        await db.SaveChangesAsync();
    }

    public async Task Delete(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var encargo = await db.Encargos
            .Include(e => e.EncargoHerramientas)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (encargo == null) return;

        // Si el encargo estaba pendiente se liberan sus herramientas:
        // las piezas de objetos múltiples regresan a bodega y las herramientas únicas vuelven a "Disponible".
        // Si ya estaba completado, el material se consumió y no se devuelve.
        if (!encargo.IsCompleted && encargo.EncargoHerramientas != null)
        {
            var viejas = encargo.EncargoHerramientas.ToDictionary(r => r.HerramientaId, r => r.CantidadUsada);
            await AplicarCambiosDeInventario(db, encargo.Id, viejas, new Dictionary<int, int>());
        }

        // Las filas de EncargoHerramientas se eliminan en cascada.
        db.Encargos.Remove(encargo);
        await db.SaveChangesAsync();
    }

    public async Task ToggleComplete(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var encargo = await db.Encargos
            .Include(e => e.EncargoHerramientas!)
                .ThenInclude(eh => eh.Herramienta)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (encargo == null) return;

        var unicas = (encargo.EncargoHerramientas ?? new List<EncargoHerramienta>())
            .Where(eh => eh.Herramienta != null && !eh.Herramienta.EsMultiple)
            .Select(eh => eh.Herramienta!)
            .ToList();

        if (!encargo.IsCompleted)
        {
            // Pendiente -> completado: las herramientas únicas quedan libres.
            // Las piezas de objetos múltiples NO se devuelven porque ya se consumieron.
            foreach (var h in unicas.Where(h => h.Estado == EstadosHerramienta.EnUso))
            {
                h.Estado = EstadosHerramienta.Disponible;
            }
            encargo.IsCompleted = true;
        }
        else
        {
            // Completado -> pendiente: hay que volver a reservar las herramientas únicas,
            // así que se comprueba que nadie más las tenga asignadas.
            var errores = new List<string>();
            await ReservarUnicas(db, encargo.Id, unicas, errores);
            if (errores.Any())
            {
                throw new InvalidOperationException(
                    "No se puede regresar el encargo a pendiente: " + string.Join(" ", errores));
            }
            encargo.IsCompleted = false;
        }

        await db.SaveChangesAsync();
    }

    // ---------------------------------------------------------------------------------------------
    // Lógica de inventario compartida por Add, Update y Delete.
    // Compara las herramientas que tenía el encargo (viejas) con las que tendrá (nuevas) y:
    //   - Objetos múltiples: valida que haya piezas suficientes y ajusta CantidadActual.
    //   - Herramientas únicas: valida que no estén asignadas a otro encargo pendiente ni en un estado
    //     que impida usarlas, y cambia su Estado a "En uso" / "Disponible".
    // Si algo no es válido lanza InvalidOperationException ANTES de guardar, así que no se guarda nada.
    // ---------------------------------------------------------------------------------------------
    private static async Task<Dictionary<int, Herramienta>> AplicarCambiosDeInventario(
        TallerDbContext db, int encargoId, Dictionary<int, int> viejas, Dictionary<int, int> nuevas)
    {
        var ids = viejas.Keys.Union(nuevas.Keys).ToList();
        var herramientas = await db.Herramientas
            .Where(h => ids.Contains(h.Id))
            .ToDictionaryAsync(h => h.Id);

        var errores = new List<string>();
        var unicasPorReservar = new List<Herramienta>();

        foreach (var id in ids)
        {
            bool estabaAsignada = viejas.TryGetValue(id, out var cantVieja);
            bool quedaAsignada = nuevas.TryGetValue(id, out var cantNueva);

            if (!herramientas.TryGetValue(id, out var h))
            {
                if (quedaAsignada) errores.Add($"La herramienta #{id} ya no existe.");
                continue;
            }

            if (h.EsMultiple)
            {
                // Piezas que este encargo puede usar: lo que hay en bodega + lo que ya tenía apartado.
                int disponibles = h.CantidadActual + (estabaAsignada ? cantVieja : 0);

                if (quedaAsignada && cantNueva < 1)
                {
                    errores.Add($"{h.Nombre}: indica al menos 1 pieza.");
                }
                else if (quedaAsignada && cantNueva > disponibles)
                {
                    errores.Add($"{h.Nombre}: se piden {cantNueva} piezas pero solo hay {disponibles} disponibles.");
                }
                else
                {
                    h.CantidadActual = disponibles - (quedaAsignada ? cantNueva : 0);
                }
            }
            else
            {
                if (quedaAsignada && !estabaAsignada)
                {
                    unicasPorReservar.Add(h);
                }
                else if (!quedaAsignada && estabaAsignada && h.Estado == EstadosHerramienta.EnUso)
                {
                    h.Estado = EstadosHerramienta.Disponible;
                }
            }
        }

        await ReservarUnicas(db, encargoId, unicasPorReservar, errores);

        if (errores.Any())
        {
            throw new InvalidOperationException(string.Join(" ", errores));
        }

        return herramientas;
    }

    // Marca herramientas únicas como "En uso" para el encargo indicado, o agrega un error si
    // ya están asignadas a otro encargo pendiente o su estado no permite usarlas.
    private static async Task ReservarUnicas(
        TallerDbContext db, int encargoId, List<Herramienta> unicas, List<string> errores)
    {
        if (!unicas.Any()) return;

        var ids = unicas.Select(h => h.Id).ToList();
        var ocupadas = await db.EncargoHerramientas
            .AsNoTracking()
            .Where(eh => ids.Contains(eh.HerramientaId)
                         && eh.EncargoId != encargoId
                         && !eh.Encargo!.IsCompleted)
            .Select(eh => new { eh.HerramientaId, Cliente = eh.Encargo!.Cliente })
            .ToListAsync();

        foreach (var h in unicas)
        {
            var otro = ocupadas.FirstOrDefault(o => o.HerramientaId == h.Id);
            if (otro != null)
            {
                errores.Add($"{h.Nombre} ya está asignada al encargo de {otro.Cliente}.");
            }
            else if (!EstadosHerramienta.PermiteAsignar(h.Estado))
            {
                errores.Add($"{h.Nombre} no se puede asignar porque está \"{h.Estado}\".");
            }
            else
            {
                h.Estado = EstadosHerramienta.EnUso;
            }
        }
    }

    private static Dictionary<int, int> ADiccionario(List<(int HerramientaId, int Cantidad)> herramientas)
    {
        // Si la misma herramienta llega repetida se suman las cantidades.
        return herramientas
            .GroupBy(x => x.HerramientaId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Cantidad));
    }
}
