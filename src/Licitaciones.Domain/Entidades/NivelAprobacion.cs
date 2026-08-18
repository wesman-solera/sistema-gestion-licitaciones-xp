using Licitaciones.Domain.Constantes;
using Licitaciones.Domain.Excepciones;
using Licitaciones.Domain.Servicios;

namespace Licitaciones.Domain.Entidades;

/// <summary>
/// Rango parametrizable de montos que determina quien debe aprobar una adjudicacion.
/// </summary>
/// <remarks>
/// El enunciado (seccion 8.7) exige explicitamente que el aprobador se obtenga de una tabla
/// y no de una cadena fija de condiciones. Por eso el aprobador es un dato de la fila y la
/// seleccion vive en <see cref="SelectorNivelAprobacion"/>, que solo recorre los rangos.
/// <para>
/// Un rango con <see cref="MontoMaximoCrc"/> nulo es el rango abierto: cubre desde su minimo
/// hacia arriba sin limite. Solo puede existir uno, y esa invariante se valida al guardar en
/// la capa de aplicacion porque requiere conocer todos los rangos existentes.
/// </para>
/// </remarks>
public sealed class NivelAprobacion
{
    /// <summary>Identificador generado por el sistema. No es editable por el usuario.</summary>
    public Guid Id { get; private set; }

    /// <summary>Monto minimo del rango, inclusivo, en colones.</summary>
    public decimal MontoMinimoCrc { get; private set; }

    /// <summary>Monto maximo del rango, inclusivo, o <c>null</c> si el rango es abierto.</summary>
    public decimal? MontoMaximoCrc { get; private set; }

    /// <summary>Cargo o instancia responsable de aprobar los montos de este rango.</summary>
    public string Aprobador { get; private set; } = string.Empty;

    /// <summary>Instante de creacion del registro, en UTC.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Instante de la ultima modificacion del registro, en UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Token de concurrencia optimista mapeado a la columna de sistema <c>xmin</c>.</summary>
    public uint Version { get; private set; }

    /// <summary>Indica si el rango no tiene limite superior.</summary>
    public bool EsRangoAbierto => MontoMaximoCrc is null;

    /// <summary>Constructor requerido por Entity Framework Core.</summary>
    private NivelAprobacion()
    {
    }

    /// <summary>Crea un rango de aprobacion validando su coherencia interna.</summary>
    /// <param name="montoMinimoCrc">Monto minimo inclusivo, mayor que cero.</param>
    /// <param name="montoMaximoCrc">Monto maximo inclusivo, o <c>null</c> para rango abierto.</param>
    /// <param name="aprobador">Cargo responsable.</param>
    /// <param name="ahoraUtc">Instante actual, obtenido del reloj inyectado.</param>
    /// <returns>Un rango valido.</returns>
    /// <exception cref="ReglaNegocioVioladaException">Si el rango es incoherente.</exception>
    public static NivelAprobacion Crear(
        decimal montoMinimoCrc,
        decimal? montoMaximoCrc,
        string aprobador,
        DateTimeOffset ahoraUtc)
    {
        string aprobadorLimpio = Validar(montoMinimoCrc, montoMaximoCrc, aprobador);

        return new NivelAprobacion
        {
            Id = Guid.CreateVersion7(),
            MontoMinimoCrc = montoMinimoCrc,
            MontoMaximoCrc = montoMaximoCrc,
            Aprobador = aprobadorLimpio,
            CreatedAt = ahoraUtc,
            UpdatedAt = ahoraUtc
        };
    }

    /// <summary>Actualiza los limites y el aprobador del rango.</summary>
    /// <param name="montoMinimoCrc">Nuevo monto minimo inclusivo.</param>
    /// <param name="montoMaximoCrc">Nuevo monto maximo inclusivo, o <c>null</c>.</param>
    /// <param name="aprobador">Nuevo cargo responsable.</param>
    /// <param name="ahoraUtc">Instante actual, obtenido del reloj inyectado.</param>
    /// <exception cref="ReglaNegocioVioladaException">Si el rango resultante es incoherente.</exception>
    public void Actualizar(
        decimal montoMinimoCrc,
        decimal? montoMaximoCrc,
        string aprobador,
        DateTimeOffset ahoraUtc)
    {
        string aprobadorLimpio = Validar(montoMinimoCrc, montoMaximoCrc, aprobador);

        MontoMinimoCrc = montoMinimoCrc;
        MontoMaximoCrc = montoMaximoCrc;
        Aprobador = aprobadorLimpio;
        UpdatedAt = ahoraUtc;
    }

    /// <summary>Indica si el monto indicado cae dentro de este rango.</summary>
    /// <param name="montoCrc">Monto a evaluar, en colones.</param>
    /// <returns><c>true</c> si el monto esta cubierto por el rango.</returns>
    public bool Cubre(decimal montoCrc)
        => montoCrc >= MontoMinimoCrc && (MontoMaximoCrc is null || montoCrc <= MontoMaximoCrc);

    /// <summary>Indica si este rango se traslapa con otro.</summary>
    /// <param name="otro">Rango con el que se compara.</param>
    /// <returns><c>true</c> si ambos rangos comparten al menos un monto.</returns>
    /// <remarks>
    /// Dos intervalos cerrados se traslapan cuando el minimo de cada uno no supera el maximo
    /// del otro. Un rango abierto se trata como si su maximo fuera infinito.
    /// </remarks>
    public bool SeTraslapaCon(NivelAprobacion otro)
    {
        ArgumentNullException.ThrowIfNull(otro);

        decimal maximoPropio = MontoMaximoCrc ?? decimal.MaxValue;
        decimal maximoOtro = otro.MontoMaximoCrc ?? decimal.MaxValue;

        return MontoMinimoCrc <= maximoOtro && otro.MontoMinimoCrc <= maximoPropio;
    }

    private static string Validar(decimal montoMinimoCrc, decimal? montoMaximoCrc, string aprobador)
    {
        if (montoMinimoCrc <= 0m)
        {
            throw new ReglaNegocioVioladaException(
                "El monto minimo del rango debe ser mayor que cero.",
                CodigosError.MontoNoPositivo);
        }

        if (montoMaximoCrc is decimal maximo)
        {
            if (maximo <= 0m)
            {
                throw new ReglaNegocioVioladaException(
                    "El monto maximo del rango debe ser mayor que cero.",
                    CodigosError.MontoNoPositivo);
            }

            if (maximo < montoMinimoCrc)
            {
                throw new ReglaNegocioVioladaException(
                    "El monto maximo del rango no puede ser menor que el monto minimo.",
                    CodigosError.RangoAprobacionInvalido);
            }
        }

        if (string.IsNullOrWhiteSpace(aprobador))
        {
            throw new ReglaNegocioVioladaException(
                "El aprobador del rango es obligatorio.",
                CodigosError.ValidacionFallida);
        }

        return NormalizadorTexto.LimpiarParaMostrar(aprobador);
    }
}
