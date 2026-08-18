using System.Globalization;
using Asp.Versioning.ApiExplorer;
using Licitaciones.Api;
using Licitaciones.Application;
using Licitaciones.Infrastructure;
using Licitaciones.Infrastructure.Persistencia;
using Licitaciones.Web.Servicios;
using Microsoft.AspNetCore.Localization;

namespace Licitaciones.Web;

/// <summary>
/// Punto de entrada de la aplicacion web.
/// </summary>
/// <remarks>
/// Este host sirve la interfaz MVC, la API REST versionada y la documentacion interactiva en un
/// unico proceso. Es la forma de monolito modular que contempla la seccion 6.3: los limites entre
/// modulos son de proyecto y de capa, no de proceso, y por eso el despliegue tiene un solo
/// contenedor de aplicacion y un solo conjunto de manifiestos de Kubernetes.
/// </remarks>
public class Program
{
    /// <summary>Constructor protegido: la clase existe solo como punto de entrada.</summary>
    /// <remarks>
    /// No es estatica a proposito. Las pruebas de integracion usan
    /// <c>WebApplicationFactory&lt;Program&gt;</c> para levantar la aplicacion en memoria, y C#
    /// no admite una clase estatica como argumento de tipo generico.
    /// </remarks>
    protected Program()
    {
    }

    /// <summary>Arranca la aplicacion web.</summary>
    /// <param name="args">Argumentos de linea de comandos.</param>
    /// <returns>Tarea que se completa cuando la aplicacion se detiene.</returns>
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder constructor = WebApplication.CreateBuilder(args);

        constructor.Services.AgregarCapaInfraestructura(constructor.Configuration);
        constructor.Services.AgregarCapaAplicacion();
        constructor.Services.AgregarCapaApi();

        constructor.Services
            .AddControllersWithViews()
            // Los controladores de la API viven en el ensamblado Licitaciones.Api. Sin esta
            // linea, MVC solo descubriria los controladores de este proyecto y los endpoints
            // REST no existirian en este host.
            .AddApplicationPart(typeof(RegistroServiciosApi).Assembly);

        constructor.Services.AddHttpContextAccessor();
        constructor.Services.AddScoped<PreferenciasUsuario>();
        constructor.Services.AddScoped<FormateadorMonto>();

        string cadenaConexion = constructor.Configuration.GetConnectionString(
            RegistroServiciosInfraestructura.NombreCadenaConexion) ?? string.Empty;

        constructor.Services.AddHealthChecks()
            .AddNpgSql(cadenaConexion, name: "postgresql", tags: ["listo"]);

        WebApplication aplicacion = constructor.Build();

        ConfigurarCulturaCostaRica(aplicacion);

        // Aplicar las migraciones al arrancar es lo que permite que "docker compose up --build"
        // levante el sistema sin pasos manuales (seccion 13.1 y 17.2).
        await IniciadorBaseDatos.MigrarAsync(aplicacion.Services);

        aplicacion.UseExceptionHandler("/Inicio/Error");

        if (!aplicacion.Environment.IsDevelopment())
        {
            aplicacion.UseHsts();
        }

        aplicacion.UseStatusCodePagesWithReExecute("/Inicio/Error");
        aplicacion.UseStaticFiles();
        aplicacion.UseRouting();

        aplicacion.UseSwagger();
        aplicacion.UseSwaggerUI(opciones =>
        {
            var proveedor = aplicacion.Services.GetRequiredService<IApiVersionDescriptionProvider>();

            foreach (ApiVersionDescription descripcion in proveedor.ApiVersionDescriptions)
            {
                opciones.SwaggerEndpoint(
                    $"/swagger/{descripcion.GroupName}/swagger.json",
                    $"Licitaciones API {descripcion.GroupName.ToUpperInvariant()}");
            }

            opciones.DocumentTitle = "API del Sistema de Gestion de Licitaciones";
        });

        aplicacion.MapControllerRoute(
            name: "default",
            pattern: "{controller=Inicio}/{action=Index}/{id?}");

        aplicacion.MapHealthChecks("/health");
        aplicacion.MapHealthChecks("/health/listo", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = registro => registro.Tags.Contains("listo")
        });

        // Sonda de arranque y de vida: responde sin tocar la base de datos, de modo que un fallo
        // de PostgreSQL no provoque el reinicio en bucle del pod de la aplicacion (seccion 13.2).
        aplicacion.MapGet("/health/vivo", () => Results.Ok(new { estado = "vivo" }));

        await aplicacion.RunAsync();
    }

    /// <summary>
    /// Fija la cultura de la aplicacion en es-CR.
    /// </summary>
    /// <param name="aplicacion">Aplicacion en construccion.</param>
    /// <remarks>
    /// El requisito 9 exige formato monetario y cultural costarricense. Fijar la cultura tambien
    /// hace que el enlace de modelo interprete los separadores decimales igual que como se
    /// muestran, de modo que un monto escrito en el formulario se lee como el usuario lo escribio.
    /// </remarks>
    private static void ConfigurarCulturaCostaRica(WebApplication aplicacion)
    {
        var cultura = new CultureInfo("es-CR");

        var opciones = new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture(cultura),
            SupportedCultures = [cultura],
            SupportedUICultures = [cultura]
        };

        aplicacion.UseRequestLocalization(opciones);
    }
}
