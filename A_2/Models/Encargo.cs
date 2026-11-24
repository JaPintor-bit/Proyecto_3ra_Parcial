namespace A_2.Models;

using System.ComponentModel.DataAnnotations;

public class Encargo
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre del cliente es obligatorio")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    public string Cliente { get; set; } = string.Empty;

    [Required(ErrorMessage = "El trabajo a realizar es obligatorio")]
    [StringLength(200, ErrorMessage = "La descripción del trabajo no puede exceder 200 caracteres")]
    public string TipoTrabajo { get; set; } = string.Empty;

    // Se guardan como tipos de fecha/hora reales (columnas 'date' y 'time' en SQL Server)
    // para que el ordenamiento sea cronológico y no alfabético.
    public DateOnly Fecha { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public TimeOnly Hora { get; set; } = new TimeOnly(DateTime.Now.Hour, DateTime.Now.Minute);

    public bool IsCompleted { get; set; } = false;

    // Relación Muchos a Muchos con Herramientas (Tema 10 - Diapositiva 14)
    public virtual ICollection<EncargoHerramienta>? EncargoHerramientas { get; set; }
}
