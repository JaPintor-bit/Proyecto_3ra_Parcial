namespace A_2.Repositorio;

using Microsoft.EntityFrameworkCore;
using A_2.Data;
using A_2.Models;

public class RepositorioHerramientas : IRepositorioHerramientas
{
    private readonly IDbContextFactory<TallerDbContext> _dbFactory;

    public RepositorioHerramientas(IDbContextFactory<TallerDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    // Tema 10 - Diapositiva 12: Usar Include para traer la Clasificación vinculada.
    // AsNoTracking: los objetos que recibe la página son copias; editarlos no modifica la BD
    // hasta que se llama explícitamente a Update.
    public async Task<List<Herramienta>> GetAll()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Herramientas
            .AsNoTracking()
            .Include(h => h.Clasificacion)
            .Include(h => h.EncargoHerramientas!)
                .ThenInclude(eh => eh.Encargo)
            .OrderBy(h => h.Nombre)
            .ToListAsync();
    }

    public async Task<Herramienta?> Get(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Herramientas
            .AsNoTracking()
            .Include(h => h.Clasificacion)
            .Include(h => h.EncargoHerramientas!)
                .ThenInclude(eh => eh.Encargo)
            .FirstOrDefaultAsync(h => h.Id == id);
    }

    public async Task<Herramienta> Add(Herramienta entity)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // Una herramienta nueva no está asignada a ningún encargo todavía.
        if (entity.Estado == EstadosHerramienta.EnUso)
        {
            entity.Estado = EstadosHerramienta.Disponible;
        }
        entity.Clasificacion = null;
        entity.EncargoHerramientas = null;

        await db.Herramientas.AddAsync(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task Update(Herramienta entity)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var actual = await db.Herramientas
            .Include(h => h.EncargoHerramientas!)
                .ThenInclude(eh => eh.Encargo)
            .FirstOrDefaultAsync(h => h.Id == entity.Id)
            ?? throw new InvalidOperationException("La herramienta ya no existe.");

        var relaciones = actual.EncargoHerramientas ?? new List<EncargoHerramienta>();

        // Cambiar entre "única" y "múltiple" rompería el cálculo de stock de los encargos ya registrados.
        if (actual.EsMultiple != entity.EsMultiple && relaciones.Any())
        {
            throw new InvalidOperationException(
                "No se puede cambiar el tipo (única / múltiple) de una herramienta que ya se usó en encargos.");
        }

        // Copia solo los valores escalares (nombre, estado, cantidades, etc.), no las navegaciones.
        db.Entry(actual).CurrentValues.SetValues(entity);

        // "En uso" lo controla el sistema según los encargos pendientes.
        if (!actual.EsMultiple)
        {
            bool asignadaAPendiente = relaciones.Any(eh => eh.Encargo != null && !eh.Encargo.IsCompleted);
            if (asignadaAPendiente && actual.Estado == EstadosHerramienta.Disponible)
                actual.Estado = EstadosHerramienta.EnUso;
            else if (!asignadaAPendiente && actual.Estado == EstadosHerramienta.EnUso)
                actual.Estado = EstadosHerramienta.Disponible;
        }

        await db.SaveChangesAsync();
    }

    public async Task Delete(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var herramienta = await db.Herramientas
            .Include(h => h.EncargoHerramientas!)
                .ThenInclude(eh => eh.Encargo)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (herramienta == null) return;

        var relaciones = herramienta.EncargoHerramientas ?? new List<EncargoHerramienta>();
        var clientesPendientes = relaciones
            .Where(eh => eh.Encargo != null && !eh.Encargo.IsCompleted)
            .Select(eh => eh.Encargo!.Cliente)
            .Distinct()
            .ToList();

        if (clientesPendientes.Any())
        {
            throw new InvalidOperationException(
                $"No se puede eliminar \"{herramienta.Nombre}\" porque está asignada a encargos pendientes " +
                $"({string.Join(", ", clientesPendientes)}). Quítala de esos encargos primero.");
        }

        // Solo quedan relaciones con encargos completados: se quitan del historial junto con la herramienta.
        db.EncargoHerramientas.RemoveRange(relaciones);
        db.Herramientas.Remove(herramienta);
        await db.SaveChangesAsync();
    }

    public async Task ActualizarCantidad(int id, int nuevaCantidad)
    {
        if (nuevaCantidad < 0)
        {
            throw new InvalidOperationException("La cantidad no puede ser negativa.");
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var h = await db.Herramientas.FindAsync(id);
        if (h != null)
        {
            h.CantidadActual = nuevaCantidad;
            await db.SaveChangesAsync();
        }
    }
}
