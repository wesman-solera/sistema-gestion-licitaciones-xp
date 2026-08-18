using Licitaciones.Application.Dtos;

namespace Licitaciones.Application.Servicios;

/// <summary>
/// Resuelve el tipo de cambio activo una sola vez por peticion y convierte montos con el.
/// </summary>
/// <remarks>
/// Sin esta abstraccion, cada mapeo de un monto a dolares dispararia su propia consulta a la
/// base de datos: un listado de 20 licitaciones haria 20 consultas identicas. Se registra con
/// ciclo de vida "scoped" para que el valor se lea una vez y se reutilice durante la peticion.
/// </remarks>
public interface IContextoMoneda
{
    /// <summary>Carga el tipo de cambio activo si todavia no se cargo en esta peticion.</summary>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Tarea que se completa cuando el tipo de cambio esta disponible.</returns>
    Task CargarAsync(CancellationToken cancelacion = default);

    /// <summary>Tipo de cambio aplicado, o <c>null</c> si no hay ninguno activo configurado.</summary>
    TipoCambioAplicadoDto? TipoCambioAplicado { get; }

    /// <summary>
    /// Construye la representacion de un monto en colones y su equivalente en dolares.
    /// </summary>
    /// <param name="montoCrc">Monto oficial en colones.</param>
    /// <returns>
    /// El monto con su equivalente, o con el componente en dolares en <c>null</c> cuando no
    /// hay tipo de cambio activo. La ausencia de tipo de cambio nunca impide leer los datos:
    /// solo desactiva la conversion.
    /// </returns>
    MontoDto Monto(decimal montoCrc);
}
