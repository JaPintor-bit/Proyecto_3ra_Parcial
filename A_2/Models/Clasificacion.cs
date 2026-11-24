namespace A_2.Models;

using System.ComponentModel.DataAnnotations;

public class Clasificacion
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre de la clasificación es obligatorio")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres")]
    public string Nombre { get; set; } = string.Empty;

    // Para navegación de EF (Relación 1 a Muchos con Herramientas)
    public virtual ICollection<Herramienta>? Herramientas { get; set; }
}
