namespace A_2.Servicios;

// Guarda y elimina las fotos de las herramientas en wwwroot/uploads/herramientas.
// En la base de datos solo se guarda la ruta relativa (Herramienta.RutaFoto).
public interface IAlmacenFotos
{
    Task<string> Guardar(byte[] contenido, string contentType);
    void Eliminar(string? rutaRelativa);
}

public class AlmacenFotos : IAlmacenFotos
{
    public const long TamanoMaximo = 5 * 1024 * 1024; // 5 MB

    public static readonly IReadOnlyDictionary<string, string> TiposPermitidos = new Dictionary<string, string>
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif"
    };

    private const string Carpeta = "uploads/herramientas";
    private readonly string _carpetaFisica;

    public AlmacenFotos(IWebHostEnvironment env)
    {
        _carpetaFisica = Path.Combine(env.WebRootPath, "uploads", "herramientas");
        Directory.CreateDirectory(_carpetaFisica);
    }

    public async Task<string> Guardar(byte[] contenido, string contentType)
    {
        if (!TiposPermitidos.TryGetValue(contentType, out var extension))
        {
            throw new InvalidOperationException("Formato de imagen no permitido (usa JPG, PNG, WEBP o GIF).");
        }
        if (contenido.Length == 0 || contenido.Length > TamanoMaximo)
        {
            throw new InvalidOperationException("La imagen debe pesar como máximo 5 MB.");
        }

        // Nombre generado por el servidor: nunca se usa el nombre que envía el navegador.
        var nombre = $"{Guid.NewGuid():N}{extension}";
        await File.WriteAllBytesAsync(Path.Combine(_carpetaFisica, nombre), contenido);
        return $"{Carpeta}/{nombre}";
    }

    public void Eliminar(string? rutaRelativa)
    {
        if (string.IsNullOrWhiteSpace(rutaRelativa) || !rutaRelativa.StartsWith(Carpeta + "/"))
            return;

        var nombre = Path.GetFileName(rutaRelativa);
        var ruta = Path.Combine(_carpetaFisica, nombre);
        try
        {
            if (File.Exists(ruta)) File.Delete(ruta);
        }
        catch (IOException)
        {
            // Si el archivo está bloqueado no se interrumpe la operación principal.
        }
    }
}
