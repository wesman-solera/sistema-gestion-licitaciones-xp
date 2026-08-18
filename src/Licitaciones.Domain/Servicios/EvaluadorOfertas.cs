using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Enums;
using Licitaciones.Domain.ObjetosValor;

namespace Licitaciones.Domain.Servicios;

/// <summary>
/// Determina la mejor oferta de una licitacion y clasifica el ahorro obtenido (seccion 8.6).
/// </summary>
/// <remarks>
/// Es un servicio de dominio sin estado: recibe los datos y devuelve el resultado, sin tocar
/// la base de datos. Eso permite probar exhaustivamente los umbrales y el desempate con
/// pruebas unitarias puras, que es donde el enunciado exige la mayor cobertura.
/// </remarks>
public static class EvaluadorOfertas
{
    /// <summary>Umbral a partir del cual el ahorro se considera conveniente, en porcentaje.</summary>
    public const decimal UmbralOfertaConveniente = 10m;

    /// <summary>
    /// Evalua las ofertas de una licitacion.
    /// </summary>
    /// <param name="ofertas">Ofertas vigentes de la licitacion.</param>
    /// <param name="presupuestoEstimadoCrc">Presupuesto estimado de la licitacion, en colones.</param>
    /// <returns>El resultado con la mejor oferta, el ahorro y la clasificacion.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Si el presupuesto no es mayor que cero.</exception>
    public static ResultadoEvaluacionOfertas Evaluar(
        IEnumerable<Oferta> ofertas,
        decimal presupuestoEstimadoCrc)
    {
        ArgumentNullException.ThrowIfNull(ofertas);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(presupuestoEstimadoCrc, 0m);

        var lista = ofertas as IReadOnlyList<Oferta> ?? ofertas.ToArray();

        Oferta? mejor = DeterminarMejorOferta(lista);

        if (mejor is null)
        {
            return new ResultadoEvaluacionOfertas(
                MejorOferta: null,
                PorcentajeAhorro: null,
                Clasificacion: ClasificacionAhorro.SinOfertasValidas,
                CantidadOfertas: 0);
        }

        decimal ahorroExacto = CalcularPorcentajeAhorro(presupuestoEstimadoCrc, mejor.MontoOfertadoCrc);

        return new ResultadoEvaluacionOfertas(
            MejorOferta: mejor,
            // El ahorro se redondea solo para mostrarlo; la clasificacion usa el valor exacto
            // para que un 9,996 % no ascienda a "conveniente" por efecto del redondeo.
            PorcentajeAhorro: decimal.Round(ahorroExacto, 2, MidpointRounding.AwayFromZero),
            Clasificacion: Clasificar(ahorroExacto),
            CantidadOfertas: lista.Count);
    }

    /// <summary>
    /// Selecciona la oferta ganadora: la de menor monto y, en empate, la registrada primero.
    /// </summary>
    /// <param name="ofertas">Ofertas a comparar.</param>
    /// <returns>La mejor oferta, o <c>null</c> si la coleccion esta vacia.</returns>
    public static Oferta? DeterminarMejorOferta(IEnumerable<Oferta> ofertas)
    {
        ArgumentNullException.ThrowIfNull(ofertas);

        return ofertas
            .OrderBy(o => o.MontoOfertadoCrc)
            .ThenBy(o => o.FechaRegistro)
            // Tercer criterio deterministico: si dos ofertas coincidieran incluso en el instante
            // de registro, el identificador ordenable garantiza un resultado estable y repetible.
            .ThenBy(o => o.Id)
            .FirstOrDefault();
    }

    /// <summary>
    /// Calcula el porcentaje de ahorro de una oferta respecto del presupuesto.
    /// </summary>
    /// <param name="presupuestoCrc">Presupuesto estimado, en colones.</param>
    /// <param name="mejorOfertaCrc">Monto de la mejor oferta, en colones.</param>
    /// <returns>Porcentaje de ahorro sin redondear.</returns>
    /// <remarks>Formula de la seccion 8.6: ((Presupuesto - Mejor oferta) / Presupuesto) x 100.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Si el presupuesto no es mayor que cero.</exception>
    public static decimal CalcularPorcentajeAhorro(decimal presupuestoCrc, decimal mejorOfertaCrc)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(presupuestoCrc, 0m);

        return (presupuestoCrc - mejorOfertaCrc) / presupuestoCrc * 100m;
    }

    /// <summary>
    /// Traduce un porcentaje de ahorro a su clasificacion cualitativa.
    /// </summary>
    /// <param name="porcentajeAhorro">Porcentaje de ahorro exacto.</param>
    /// <returns>La clasificacion correspondiente.</returns>
    /// <remarks>
    /// Los umbrales son los de la seccion 8.6. Como la regla de negocio impide que una oferta
    /// supere el presupuesto, un ahorro negativo no deberia ocurrir; si ocurriera por datos
    /// corruptos se clasifica igual que la ausencia de ahorro en lugar de inventar una categoria.
    /// </remarks>
    public static ClasificacionAhorro Clasificar(decimal porcentajeAhorro) => porcentajeAhorro switch
    {
        >= UmbralOfertaConveniente => ClasificacionAhorro.OfertaConveniente,
        > 0m => ClasificacionAhorro.OfertaAceptable,
        _ => ClasificacionAhorro.OfertaValidaSinAhorro
    };
}
