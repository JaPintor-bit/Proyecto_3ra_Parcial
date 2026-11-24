namespace A_2.Data;

using Microsoft.EntityFrameworkCore;
using A_2.Models;

// Tema 10 - Diapositiva 3: Modelo del contexto de la BD
public class TallerDbContext : DbContext
{
    public TallerDbContext(DbContextOptions<TallerDbContext> options) : base(options)
    {
    }

    public DbSet<Clasificacion> Clasificaciones { get; set; }
    public DbSet<Herramienta> Herramientas { get; set; }
    public DbSet<Encargo> Encargos { get; set; }
    public DbSet<EncargoHerramienta> EncargoHerramientas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Clave compuesta para la relación Muchos a Muchos (Encargo <-> Herramienta)
        modelBuilder.Entity<EncargoHerramienta>()
            .HasKey(eh => new { eh.EncargoId, eh.HerramientaId });

        modelBuilder.Entity<EncargoHerramienta>()
            .HasOne(eh => eh.Encargo)
            .WithMany(e => e.EncargoHerramientas)
            .HasForeignKey(eh => eh.EncargoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EncargoHerramienta>()
            .HasOne(eh => eh.Herramienta)
            .WithMany(h => h.EncargoHerramientas)
            .HasForeignKey(eh => eh.HerramientaId)
            // Restrict: una herramienta usada en encargos no se puede borrar "en silencio";
            // el repositorio decide qué hacer con sus relaciones antes de eliminarla.
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict: no se puede eliminar una clasificación que todavía tiene herramientas
        // (antes era Cascade y se borraban todas sus herramientas).
        modelBuilder.Entity<Herramienta>()
            .HasOne(h => h.Clasificacion)
            .WithMany(c => c.Herramientas)
            .HasForeignKey(h => h.ClasificacionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Clasificaciones iniciales predefinidas
        modelBuilder.Entity<Clasificacion>().HasData(
            new Clasificacion { Id = 1, Nombre = "Herramientas Manuales" },
            new Clasificacion { Id = 2, Nombre = "Herramientas Eléctricas" },
            new Clasificacion { Id = 3, Nombre = "Tornillería y Fijaciones" },
            new Clasificacion { Id = 4, Nombre = "Consumibles y Accesorios" }
        );
    }
}
