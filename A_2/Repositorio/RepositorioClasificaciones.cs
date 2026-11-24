namespace A_2.Repositorio;

using Microsoft.EntityFrameworkCore;
using A_2.Data;
using A_2.Models;

// Tema 10 - Diapositiva 6: Implementación del repositorio.
// Cada operación crea su propio DbContext con IDbContextFactory: en Blazor Server un contexto
// "Scoped" vive todo el circuito y arrastraba cambios no guardados entre páginas.
public class RepositorioClasificaciones : IRepositorioClasificaciones
{
    private readonly IDbContextFactory<TallerDbContext> _dbFactory;

    public RepositorioClasificaciones(IDbContextFactory<TallerDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<Clasificacion>> GetAll()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Clasificaciones.AsNoTracking().OrderBy(c => c.Nombre).ToListAsync();
    }

    public async Task<Dictionary<int, int>> ContarHerramientasPorClasificacion()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Herramientas
            .GroupBy(h => h.ClasificacionId)
            .Select(g => new { g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Total);
    }

    public async Task<Clasificacion?> Get(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Clasificaciones.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Clasificacion> Add(Clasificacion entity)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        entity.Nombre = entity.Nombre.Trim();
        await ValidarNombreUnico(db, entity.Nombre, 0);

        await db.Clasificaciones.AddAsync(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task Update(Clasificacion entity)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var actual = await db.Clasificaciones.FindAsync(entity.Id)
            ?? throw new InvalidOperationException("La clasificación ya no existe.");

        var nombre = entity.Nombre.Trim();
        await ValidarNombreUnico(db, nombre, entity.Id);

        actual.Nombre = nombre;
        await db.SaveChangesAsync();
    }

    public async Task Delete(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var clasificacion = await db.Clasificaciones.FindAsync(id);
        if (clasificacion == null) return;

        var total = await db.Herramientas.CountAsync(h => h.ClasificacionId == id);
        if (total > 0)
        {
            throw new InvalidOperationException(
                $"No se puede eliminar \"{clasificacion.Nombre}\" porque tiene {total} herramienta(s). " +
                "Muévelas a otra clasificación primero.");
        }

        db.Clasificaciones.Remove(clasificacion);
        await db.SaveChangesAsync();
    }

    private static async Task ValidarNombreUnico(TallerDbContext db, string nombre, int idActual)
    {
        var existe = await db.Clasificaciones
            .AnyAsync(c => c.Id != idActual && c.Nombre.ToLower() == nombre.ToLower());
        if (existe)
        {
            throw new InvalidOperationException($"Ya existe una clasificación llamada \"{nombre}\".");
        }
    }
}
