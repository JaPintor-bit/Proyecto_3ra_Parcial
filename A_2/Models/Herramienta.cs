namespace A_2.Models;

using System.ComponentModel.DataAnnotations;

public class Herramienta
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre de la herramienta es obligatorio")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres")]
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public string? RutaFoto { get; set; }

    public string Estado { get; set; } = "Disponible";

    // Campos de objetos múltiples (tornillos, conectores, clavos, etc.)
    public bool EsMultiple { get; set; } = false;

    [Range(0, int.MaxValue, ErrorMessage = "La cantidad no puede ser negativa")]
    public int CantidadActual { get; set; } = 0;

    [Range(0, int.MaxValue, ErrorMessage = "La cantidad mínima no puede ser negativa")]
    public int CantidadMinima { get; set; } = 0;

    public string? MensajeAlerta { get; set; }

    public bool AlertaActiva { get; set; } = true;

    // Relación 1 a Muchos con Clasificación (Tema 10 - Diapositiva 11)
    [Required(ErrorMessage = "Debe seleccionar una clasificación")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una clasificación válida")]
    public int ClasificacionId { get; set; }

    public virtual Clasificacion? Clasificacion { get; set; }

    // Relación Muchos a Muchos con Encargos (Tema 10 - Diapositiva 14)
    public virtual ICollection<EncargoHerramienta>? EncargoHerramientas { get; set; }
}
