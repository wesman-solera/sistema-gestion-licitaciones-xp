using Licitaciones.Domain.Constantes;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Excepciones;

namespace Licitaciones.Domain.Servicios;

/// <summary>
/// Determina el aprobador que corresponde a un monto recorriendo la tabla parametrizable.
/// </summary>
/// <remarks>
/// El enunciado (seccion 8.7) exige que el aprobador provenga de una tabla y prohibe
/// resolverlo con una cadena fija de condiciones. Este servicio no contiene ningun umbral
/// literal: los limites son datos de la tabla <c>niveles_aprobacion</c> y cambiarlos no
/// requiere recompilar ni modificar codigo.
/// </remarks>
public static class SelectorNivelAprobacion
{
    /// <summary>
    /// Busca el rango que cubre el monto indicado.
    /// </summary>
    /// <param name="montoCrc">Monto a clasificar, en colones.</param>
    /// <param name="niveles">Rangos parametrizados vigentes.</param>
    /// <returns>El rango aplicable, o <c>null</c> si ninguno cubre el monto.</returns>
    /// <remarks>
    /// Se ordena por monto minimo para que el resultado sea estable con independencia del
    /// orden en que la base de datos devuelva las filas.
    /// </remarks>
    public static NivelAprobacion? Seleccionar(decimal montoCrc, IEnumerable<NivelAprobacion> niveles)
    {
        ArgumentNullException.ThrowIfNull(niveles);

        return niveles
            .OrderBy(n => n.MontoMinimoCrc)
            .FirstOrDefault(n => n.Cubre(montoCrc));
    }

    /// <summary>
    /// Busca el rango aplicable y falla si la tabla no cubre el monto.
    /// </summary>
    /// <param name="montoCrc">Monto a clasificar, en colones.</param>
    /// <param name="niveles">Rangos parametrizados vigentes.</param>
    /// <returns>El rango aplicable.</returns>
    /// <exception cref="ReglaNegocioVioladaException">
    /// Si ningun rango cubre el monto. Es un error de parametrizacion, no del usuario, y se
    /// reporta como tal para que el administrador sepa que debe completar la tabla.
    /// </exception>
    public static NivelAprobacion SeleccionarObligatorio(
        decimal montoCrc,
        IEnumerable<NivelAprobacion> niveles)
    {
        return Seleccionar(montoCrc, niveles)
            ?? throw new ReglaNegocioVioladaException(
                $"No hay un nivel de aprobacion parametrizado que cubra el monto " +
                $"{NormalizadorTexto.FormatearColones(montoCrc)}. Revise la tabla de niveles de aprobacion.",
                CodigosError.SinNivelAprobacionAplicable);
    }

    /// <summary>
    /// Verifica que un conjunto de rangos cumpla las invariantes de la seccion 8.7.
    /// </summary>
    /// <param name="niveles">Conjunto completo de rangos que quedaria vigente.</param>
    /// <exception cref="ReglaNegocioVioladaException">
    /// Si dos rangos se traslapan o si existe mas de un rango abierto.
    /// </exception>
    /// <remarks>
    /// Debe invocarse con el conjunto resultante de la operacion, no con el actual: la capa de
    /// aplicacion arma la lista incluyendo el rango nuevo o modificado y excluyendo el eliminado.
    /// </remarks>
    public static void AsegurarConjuntoValido(IReadOnlyList<NivelAprobacion> niveles)
    {
        ArgumentNullException.ThrowIfNull(niveles);

        int rangosAbiertos = niveles.Count(n => n.EsRangoAbierto);
        if (rangosAbiertos > 1)
        {
            throw new ReglaNegocioVioladaException(
                "Solo puede existir un rango abierto sin monto maximo.",
                CodigosError.RangoAbiertoDuplicado);
        }

        for (int i = 0; i < niveles.Count; i++)
        {
            for (int j = i + 1; j < niveles.Count; j++)
            {
                if (niveles[i].SeTraslapaCon(niveles[j]))
                {
                    throw new ReglaNegocioVioladaException(
                        "Los rangos de aprobacion no pueden traslaparse entre si.",
                        CodigosError.RangosAprobacionTraslapados);
                }
            }
        }
    }
}
