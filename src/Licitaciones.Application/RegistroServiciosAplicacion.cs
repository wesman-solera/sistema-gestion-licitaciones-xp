using FluentValidation;
using Licitaciones.Application.Servicios;
using Microsoft.Extensions.DependencyInjection;

namespace Licitaciones.Application;

/// <summary>
/// Registro de los servicios de la capa de aplicacion en el contenedor de dependencias.
/// </summary>
/// <remarks>
/// Cada capa expone su propio metodo de registro para que el arranque de la aplicacion no
/// tenga que conocer los tipos concretos de las capas internas.
/// </remarks>
public static class RegistroServiciosAplicacion
{
    /// <summary>Registra servicios de aplicacion y validadores.</summary>
    /// <param name="servicios">Coleccion de servicios del contenedor.</param>
    /// <returns>La misma coleccion, para encadenar llamadas.</returns>
    /// <remarks>
    /// El ciclo de vida es "scoped" porque los servicios dependen del contexto de datos y del
    /// contexto de moneda, que viven durante una peticion.
    /// </remarks>
    public static IServiceCollection AgregarCapaAplicacion(this IServiceCollection servicios)
    {
        ArgumentNullException.ThrowIfNull(servicios);

        servicios.AddValidatorsFromAssemblyContaining<Servicios.LicitacionServicio>(
            ServiceLifetime.Scoped);

        servicios.AddScoped<IContextoMoneda, ContextoMoneda>();
        servicios.AddScoped<ILicitacionServicio, LicitacionServicio>();
        servicios.AddScoped<IProveedorServicio, ProveedorServicio>();
        servicios.AddScoped<IOfertaServicio, OfertaServicio>();
        servicios.AddScoped<INivelAprobacionServicio, NivelAprobacionServicio>();
        servicios.AddScoped<ITipoCambioServicio, TipoCambioServicio>();

        return servicios;
    }
}
