using System.Reflection;
using Asp.Versioning;
using Licitaciones.Api.Comun;
using Licitaciones.Api.Configuracion;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Licitaciones.Api;

/// <summary>
/// Registro de los servicios de la capa de API en el contenedor de dependencias.
/// </summary>
/// <remarks>
/// El proyecto Web reutiliza este registro para exponer la misma API dentro del mismo proceso,
/// de modo que la interfaz y los endpoints REST se despliegan como un unico contenedor
/// (monolito modular, seccion 6.3). El proyecto Api tambien puede ejecutarse por separado.
/// </remarks>
public static class RegistroServiciosApi
{
    /// <summary>Registra versionado, documentacion OpenAPI y manejo global de errores.</summary>
    /// <param name="servicios">Coleccion de servicios del contenedor.</param>
    /// <returns>La misma coleccion, para encadenar llamadas.</returns>
    public static IServiceCollection AgregarCapaApi(this IServiceCollection servicios)
    {
        ArgumentNullException.ThrowIfNull(servicios);

        servicios
            .AddApiVersioning(opciones =>
            {
                opciones.DefaultApiVersion = new ApiVersion(1, 0);
                opciones.AssumeDefaultVersionWhenUnspecified = true;
                // Publicar las versiones disponibles en las cabeceras evita que el cliente tenga
                // que adivinarlas o leerlas de la documentacion.
                opciones.ReportApiVersions = true;
            })
            .AddApiExplorer(opciones =>
            {
                opciones.GroupNameFormat = "'v'VVV";
                opciones.SubstituteApiVersionInUrl = true;
            });

        servicios.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfiguracionSwagger>();

        servicios.AddSwaggerGen(opciones =>
        {
            // Los comentarios XML de controladores y DTO alimentan la documentacion interactiva:
            // lo que se escribe una vez en el codigo aparece en Swagger sin duplicarse.
            IncluirComentariosXml(opciones, Assembly.GetExecutingAssembly());
            IncluirComentariosXml(opciones, typeof(Application.Dtos.MontoDto).Assembly);

            opciones.SupportNonNullableReferenceTypes();
        });

        servicios.AddExceptionHandler<ManejadorExcepcionesGlobal>();

        servicios.AddProblemDetails(opciones =>
            opciones.CustomizeProblemDetails = contexto =>
            {
                contexto.ProblemDetails.Extensions.TryAdd(
                    "traceId",
                    contexto.HttpContext.TraceIdentifier);
            });

        servicios.Configure<ApiBehaviorOptions>(opciones =>
        {
            // El enlace de modelo de ASP.NET Core produce su propio formato de error. Se sustituye
            // para que toda la API responda con la misma forma de ProblemDetails y el mismo
            // conjunto de extensiones que el manejador global.
            opciones.InvalidModelStateResponseFactory = contexto =>
            {
                var errores = contexto.ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .ToDictionary(
                        e => e.Key,
                        e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray());

                var problema = new ValidationProblemDetails(errores)
                {
                    Title = "Datos de entrada invalidos",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = contexto.HttpContext.Request.Path
                };

                problema.Extensions["codigoError"] = Domain.Constantes.CodigosError.ValidacionFallida;
                problema.Extensions["correlacion"] = contexto.HttpContext.TraceIdentifier;

                return new BadRequestObjectResult(problema)
                {
                    ContentTypes = { "application/problem+json" }
                };
            };
        });

        return servicios;
    }

    /// <summary>Incorpora el archivo de comentarios XML de un ensamblado si esta presente.</summary>
    /// <param name="opciones">Opciones de generacion de Swagger.</param>
    /// <param name="ensamblado">Ensamblado cuyos comentarios se quieren incluir.</param>
    /// <remarks>
    /// La ausencia del archivo no se trata como error: en un paquete publicado sin documentacion
    /// XML la API debe seguir levantando, solo que con menos descripciones.
    /// </remarks>
    private static void IncluirComentariosXml(SwaggerGenOptions opciones, Assembly ensamblado)
    {
        string ruta = Path.Combine(AppContext.BaseDirectory, $"{ensamblado.GetName().Name}.xml");

        if (File.Exists(ruta))
        {
            opciones.IncludeXmlComments(ruta, includeControllerXmlComments: true);
        }
    }
}
