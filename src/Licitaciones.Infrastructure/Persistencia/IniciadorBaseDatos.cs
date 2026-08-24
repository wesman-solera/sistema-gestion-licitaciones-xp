using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Licitaciones.Infrastructure.Persistencia;

/// <summary>
/// Aplica las migraciones pendientes durante el arranque de la aplicacion.
/// </summary>
/// <remarks>
/// En Docker Compose y en Kubernetes el contenedor de la aplicacion puede arrancar antes de que
/// PostgreSQL termine de aceptar conexiones. Por eso se reintenta con espera creciente en lugar
/// de fallar en el primer intento: es lo que permite que <c>docker compose up --build</c>
/// funcione sin pasos manuales (seccion 13.1).
/// </remarks>
public static class IniciadorBaseDatos
{
    /// <summary>Cantidad maxima de intentos de conexion durante el arranque.</summary>
    public const int IntentosMaximos = 8;

    /// <summary>Espera base entre intentos, en segundos. Crece de forma lineal con el intento.</summary>
    public const int EsperaBaseSegundos = 2;

    /// <summary>Aplica las migraciones pendientes reintentando mientras la base no responda.</summary>
    /// <param name="proveedorServicios">Proveedor de servicios de la aplicacion.</param>
    /// <param name="cancelacion">Token de cancelacion del arranque.</param>
    /// <returns>Tarea que se completa cuando la base esta migrada.</returns>
    /// <exception cref="InvalidOperationException">Si se agotan los intentos sin lograr conectar.</exception>
    public static async Task MigrarAsync(
        IServiceProvider proveedorServicios,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(proveedorServicios);

        using IServiceScope ambito = proveedorServicios.CreateScope();

        var contexto = ambito.ServiceProvider.GetRequiredService<LicitacionesDbContext>();
        var registrador = ambito.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(IniciadorBaseDatos));

        for (int intento = 1; intento <= IntentosMaximos; intento++)
        {
            try
            {
                await contexto.Database.MigrateAsync(cancelacion);

                registrador.LogInformation(
                    "Migraciones aplicadas correctamente en el intento {Intento}.",
                    intento);

                return;
            }
            catch (Exception excepcion) when (intento < IntentosMaximos)
            {
                TimeSpan espera = TimeSpan.FromSeconds(EsperaBaseSegundos * intento);

                registrador.LogWarning(
                    excepcion,
                    "La base de datos aun no responde (intento {Intento} de {Total}). Reintentando en {Espera}.",
                    intento,
                    IntentosMaximos,
                    espera);

                await Task.Delay(espera, cancelacion);
            }
        }

        throw new InvalidOperationException(
            $"No fue posible aplicar las migraciones despues de {IntentosMaximos} intentos. " +
            "Verifique que PostgreSQL este disponible y que la cadena de conexion sea correcta.");
    }
}
