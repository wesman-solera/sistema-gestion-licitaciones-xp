using Licitaciones.Application.Abstracciones;
using Licitaciones.Domain.Abstracciones;
using Licitaciones.Infrastructure.Persistencia;
using Licitaciones.Infrastructure.Repositorios;
using Licitaciones.Infrastructure.Servicios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Licitaciones.Infrastructure;

/// <summary>
/// Registro de los servicios de infraestructura en el contenedor de dependencias.
/// </summary>
public static class RegistroServiciosInfraestructura
{
    /// <summary>Nombre logico de la cadena de conexion en la configuracion.</summary>
    public const string NombreCadenaConexion = "Licitaciones";

    /// <summary>Registra el contexto de datos, los repositorios y los servicios de infraestructura.</summary>
    /// <param name="servicios">Coleccion de servicios del contenedor.</param>
    /// <param name="configuracion">Configuracion de la aplicacion.</param>
    /// <returns>La misma coleccion, para encadenar llamadas.</returns>
    /// <exception cref="InvalidOperationException">Si no se configuro la cadena de conexion.</exception>
    /// <remarks>
    /// La cadena de conexion se resuelve desde la configuracion, que en ejecucion proviene de
    /// variables de entorno o de un Secret de Kubernetes. El repositorio no contiene credenciales
    /// reales: <c>appsettings.json</c> deja el valor vacio a proposito (seccion 11).
    /// </remarks>
    public static IServiceCollection AgregarCapaInfraestructura(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        ArgumentNullException.ThrowIfNull(servicios);
        ArgumentNullException.ThrowIfNull(configuracion);

        string cadena = configuracion.GetConnectionString(NombreCadenaConexion)
            ?? throw new InvalidOperationException(
                $"No se configuro la cadena de conexion '{NombreCadenaConexion}'. " +
                "Defina ConnectionStrings__Licitaciones como variable de entorno o secreto.");

        servicios.AddDbContext<LicitacionesDbContext>(opciones =>
        {
            opciones.UseNpgsql(cadena, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(LicitacionesDbContext).Assembly.FullName);

                // Reintento ante fallos transitorios de red, habitual en Kubernetes cuando el
                // pod de PostgreSQL se reprograma.
                npgsql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(5), null);
            });

            opciones.ConfigureWarnings(avisos =>
                avisos.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        servicios.AddScoped<IUnidadTrabajo, UnidadTrabajo>();
        servicios.AddScoped<ILicitacionRepositorio, LicitacionRepositorio>();
        servicios.AddScoped<IProveedorRepositorio, ProveedorRepositorio>();
        servicios.AddScoped<IOfertaRepositorio, OfertaRepositorio>();
        servicios.AddScoped<INivelAprobacionRepositorio, NivelAprobacionRepositorio>();
        servicios.AddScoped<ITipoCambioRepositorio, TipoCambioRepositorio>();

        // El reloj no guarda estado, por lo que una sola instancia sirve a toda la aplicacion.
        servicios.AddSingleton<IRelojSistema, RelojSistema>();

        return servicios;
    }
}
