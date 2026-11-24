namespace A_2.Models;

// Tabla para relación Muchos a Muchos (Tema 10 - Diapositiva 16)
// Incluye cantidad usada para objetos múltiples
public class EncargoHerramienta
{
    public int EncargoId { get; set; }
    public virtual Encargo? Encargo { get; set; }

    public int HerramientaId { get; set; }
    public virtual Herramienta? Herramienta { get; set; }

    // Cantidad usada en este encargo (para objetos múltiples como tornillos)
    public int CantidadUsada { get; set; } = 0;
}
