using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Licitaciones.Api.Configuracion;

/// <summary>
/// Genera un documento de OpenAPI por cada version de la API descubierta.
/// </summary>
/// <remarks>
/// Sin esta clase habria que declarar a mano cada version en <c>SwaggerGenOptions</c> y el
/// documento quedaria desactualizado al agregar una version nueva. Aqui se recorre lo que el
/// explorador de API descubrio en tiempo de ejecucion.
/// </remarks>
public sealed class ConfiguracionSwagger : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _proveedorVersiones;

    /// <summary>Inicializa la configuracion.</summary>
    /// <param name="proveedorVersiones">Proveedor de versiones descubiertas.</param>
    public ConfiguracionSwagger(IApiVersionDescriptionProvider proveedorVersiones)
    {
        _proveedorVersiones = proveedorVersiones;
    }

    /// <inheritdoc />
    public void Configure(SwaggerGenOptions opciones)
    {
        ArgumentNullException.ThrowIfNull(opciones);

        foreach (ApiVersionDescription descripcion in _proveedorVersiones.ApiVersionDescriptions)
        {
            opciones.SwaggerDoc(descripcion.GroupName, CrearInformacion(descripcion));
        }
    }

    private static OpenApiInfo CrearInformacion(ApiVersionDescription descripcion)
    {
        var informacion = new OpenApiInfo
        {
            Title = "API del Sistema de Gestion de Licitaciones",
            Version = descripcion.ApiVersion.ToString(),
            Description =
                "API REST del proyecto final del curso ITI-822 (Metodologias Agiles de Desarrollo " +
                "de Software) de la Universidad Tecnica Nacional.\n\n" +
                "Los montos oficiales se expresan siempre en colones costarricenses (CRC). " +
                "El valor en dolares que acompana cada monto es una representacion calculada con " +
                "el tipo de cambio activo y no se almacena en la base de datos.\n\n" +
                "Los errores se devuelven como ProblemDetails (RFC 7807) con las extensiones " +
                "`codigoError` y `correlacion`.",
            Contact = new OpenApiContact
            {
                Name = "Wesman Edel Solera Rodriguez",
                Url = new Uri("https://github.com/wesman-solera")
            }
        };

        if (descripcion.IsDeprecated)
        {
            informacion.Description += "\n\nEsta version de la API esta marcada como obsoleta.";
        }

        return informacion;
    }
}
