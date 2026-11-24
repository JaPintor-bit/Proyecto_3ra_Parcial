namespace A_2.Models;

// Valores posibles del campo Herramienta.Estado.
// "En uso" lo asigna el sistema cuando una herramienta única queda asignada a un encargo pendiente
// y lo retira cuando el encargo se completa, se elimina o deja de usarla.
public static class EstadosHerramienta
{
    public const string Disponible = "Disponible";
    public const string EnUso = "En uso";
    public const string EnReparacion = "En reparación";
    public const string Prestada = "Prestada";
    public const string Perdida = "Perdida";
    public const string Danada = "Dañada";

    public static readonly IReadOnlyList<string> Todos =
        new[] { Disponible, EnUso, EnReparacion, Prestada, Perdida, Danada };

    // Una herramienta única solo puede asignarse a un encargo si está disponible
    // (o marcada "En uso" sin estar realmente asignada a otro encargo pendiente).
    public static bool PermiteAsignar(string? estado) => estado is Disponible or EnUso;
}
