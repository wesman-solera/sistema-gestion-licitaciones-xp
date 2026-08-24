using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Licitaciones.Infrastructure.Persistencia;

/// <summary>
/// Comprueba que la base de datos responda, para la sonda de disponibilidad.
/// </summary>
/// <remarks>
/// Se implementa aqui en lugar de incorporar un paquete de terceros por dos razones. La primera es
/// que la comprobacion que hace falta es exactamente esta: si el contexto puede abrir conexion,
/// la aplicacion puede atender peticiones utiles. La segunda es que una dependencia menos es una
/// version menos que mantener alineada con el resto de la solucion.
/// <para>
/// Solo alimenta la sonda de disponibilidad. La sonda de vida no la usa a proposito: si lo hiciera,
/// una caida de PostgreSQL provocaria el reinicio en bucle de todos los pods de la aplicacion sin
/// resolver nada, porque el problema no esta en ellos (ver docs/kubernetes.md).
/// </para>
/// </remarks>
public sealed class ComprobacionSaludBaseDatos : IHealthCheck
{
    /// <summary>Nombre con el que se registra la comprobacion.</summary>
    public const string Nombre = "postgresql";

    /// <summary>Etiqueta que la asocia a la sonda de disponibilidad.</summary>
    public const string EtiquetaDisponibilidad = "listo";

    private readonly LicitacionesDbContext _contexto;

    /// <summary>Inicializa la comprobacion con el contexto de datos.</summary>
    /// <param name="contexto">Contexto de Entity Framework Core.</param>
    public ComprobacionSaludBaseDatos(LicitacionesDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            bool responde = await _contexto.Database.CanConnectAsync(cancellationToken);

            return responde
                ? HealthCheckResult.Healthy("La base de datos responde.")
                : HealthCheckResult.Unhealthy("La base de datos no acepta conexiones.");
        }
        catch (Exception excepcion)
        {
            // El detalle tecnico queda en el registro del servidor a traves de la excepcion, no
            // en el cuerpo de la respuesta: la ruta de salud puede estar expuesta al exterior.
            return HealthCheckResult.Unhealthy(
                "No fue posible comprobar el estado de la base de datos.",
                excepcion);
        }
    }
}
