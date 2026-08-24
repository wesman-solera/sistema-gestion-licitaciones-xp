using Asp.Versioning.ApiExplorer;
using Licitaciones.Application;
using Licitaciones.Infrastructure;
using Licitaciones.Infrastructure.Persistencia;

namespace Licitaciones.Api;

/// <summary>
/// Punto de entrada del servicio de API cuando se ejecuta de forma independiente.
/// </summary>
/// <remarks>
/// El despliegue habitual de la solucion usa el proyecto Web como unico host, que incluye
/// tambien estos controladores. Este arranque separado existe para poder levantar la API sola
/// durante el desarrollo y para dejar abierta la division en servicios independientes que
/// contempla la seccion 6.3 sin reescribir la capa de aplicacion.
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

    /// <summary>Arranca el servicio de API.</summary>
    /// <param name="args">Argumentos de linea de comandos.</param>
    /// <returns>Tarea que se completa cuando el servicio se detiene.</returns>
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder constructor = WebApplication.CreateBuilder(args);

        constructor.Services.AgregarCapaInfraestructura(constructor.Configuration);
        constructor.Services.AgregarCapaAplicacion();
        constructor.Services.AgregarCapaApi();

        constructor.Services.AddControllers();

        constructor.Services.AddHealthChecks()
            .AddCheck<ComprobacionSaludBaseDatos>(ComprobacionSaludBaseDatos.Nombre);

        WebApplication aplicacion = constructor.Build();

        // Las migraciones se aplican al arrancar y con reintentos, para que el contenedor
        // funcione aunque PostgreSQL todavia no acepte conexiones (seccion 13.1).
        await IniciadorBaseDatos.MigrarAsync(aplicacion.Services);

        aplicacion.UseExceptionHandler();
        aplicacion.UseStatusCodePages();

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
        });

        aplicacion.MapControllers();
        aplicacion.MapHealthChecks("/health");

        await aplicacion.RunAsync();
    }
}
