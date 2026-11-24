using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using A_2.Components;
using A_2.Data;
using A_2.Repositorio;
using A_2.Servicios;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Tema 10 - Diapositiva 4: Agregar el servicio EntityFramework.
// Se usa AddDbContextFactory en lugar de AddDbContext: en Blazor Server un servicio "Scoped" dura
// todo el circuito (la pestaña abierta), así que un único DbContext acumulaba entidades rastreadas
// y cambios no guardados entre páginas. Con la fábrica, cada operación del repositorio crea y
// desecha su propio contexto.
builder.Services.AddDbContextFactory<TallerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           // Si el modelo y la última migración difieren, se registra un aviso en la consola
           // en lugar de impedir que la aplicación arranque.
           .ConfigureWarnings(w => w.Log(RelationalEventId.PendingModelChangesWarning)));

// Tema 10 - Diapositiva 7: Agregar servicios de repositorios por inyección de dependencias
builder.Services.AddScoped<IRepositorioClasificaciones, RepositorioClasificaciones>();
builder.Services.AddScoped<IRepositorioHerramientas, RepositorioHerramientas>();
builder.Services.AddScoped<IRepositorioEncargos, RepositorioEncargos>();

// Almacenamiento de fotos de herramientas (wwwroot/uploads/herramientas)
builder.Services.AddSingleton<IAlmacenFotos, AlmacenFotos>();

var app = builder.Build();

// Crea la base TallerDB (si no existe) y aplica las migraciones pendientes al iniciar.
// Así el proyecto se puede ejecutar con F5 sin correr antes Update-Database.
using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TallerDbContext>>();
    using var db = dbFactory.CreateDbContext();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// MapStaticAssets solo sirve los archivos que existen al compilar; UseStaticFiles sirve además
// las fotos que se suben mientras la aplicación está corriendo.
app.UseStaticFiles();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
