using Licitaciones.Domain.Constantes;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Excepciones;

namespace Licitaciones.Domain.Servicios;

/// <summary>
/// Convierte montos de colones a dolares para su presentacion (seccion 8.8).
/// </summary>
/// <remarks>
/// La conversion es unidireccional y no persistente: el colon costarricense es la unica
/// fuente de verdad y ningun resultado de este servicio se guarda en la base de datos.
/// La operacion se hace enteramente con <c>decimal</c> para no introducir el error de
/// representacion que tendria un tipo de punto flotante.
/// </remarks>
public static class ConversorMoneda
{
    /// <summary>Cantidad de decimales con la que se presenta un monto en dolares.</summary>
    public const int DecimalesUsd = 2;

    /// <summary>
    /// Convierte un monto en colones a dolares usando el tipo de cambio indicado.
    /// </summary>
    /// <param name="montoCrc">Monto en colones costarricenses.</param>
    /// <param name="crcPorUsd">Colones que equivalen a un dolar.</param>
    /// <returns>El monto equivalente en dolares, redondeado a dos decimales.</returns>
    /// <exception cref="ReglaNegocioVioladaException">Si el tipo de cambio no es mayor que cero.</exception>
    public static decimal ConvertirAUsd(decimal montoCrc, decimal crcPorUsd)
    {
        if (crcPorUsd <= 0m)
        {
            throw new ReglaNegocioVioladaException(
                "El tipo de cambio debe ser mayor que cero para poder convertir montos.",
                CodigosError.TipoCambioInvalido);
        }

        return decimal.Round(montoCrc / crcPorUsd, DecimalesUsd, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Convierte un monto en colones usando un tipo de cambio ya cargado.
    /// </summary>
    /// <param name="montoCrc">Monto en colones costarricenses.</param>
    /// <param name="tipoCambio">Tipo de cambio activo.</param>
    /// <returns>El monto equivalente en dolares, redondeado a dos decimales.</returns>
    /// <exception cref="ReglaNegocioVioladaException">Si no se recibio un tipo de cambio utilizable.</exception>
    public static decimal ConvertirAUsd(decimal montoCrc, TipoCambio? tipoCambio)
    {
        if (tipoCambio is null)
        {
            throw new ReglaNegocioVioladaException(
                "No hay un tipo de cambio activo configurado. Registre uno antes de mostrar montos en dolares.",
                CodigosError.SinTipoCambioActivo);
        }

        return ConvertirAUsd(montoCrc, tipoCambio.CrcPorUsd);
    }
}
