using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Enums;

namespace Licitaciones.Domain.ObjetosValor;

/// <summary>
/// Resultado del analisis de las ofertas de una licitacion.
/// </summary>
/// <param name="MejorOferta">Oferta ganadora, o <c>null</c> cuando la licitacion no tiene ofertas.</param>
/// <param name="PorcentajeAhorro">
/// Ahorro de la mejor oferta respecto del presupuesto, redondeado a dos decimales para su
/// presentacion. Es <c>null</c> cuando no hay ofertas.
/// </param>
/// <param name="Clasificacion">Clasificacion cualitativa segun los umbrales de la seccion 8.6.</param>
/// <param name="CantidadOfertas">Cantidad de ofertas consideradas en la evaluacion.</param>
/// <remarks>
/// Es un objeto de valor inmutable: se calcula a demanda a partir de las ofertas vigentes y
/// nunca se persiste, de modo que no puede quedar desincronizado con los datos reales.
/// </remarks>
public sealed record ResultadoEvaluacionOfertas(
    Oferta? MejorOferta,
    decimal? PorcentajeAhorro,
    ClasificacionAhorro Clasificacion,
    int CantidadOfertas)
{
    /// <summary>Texto exacto que el enunciado exige mostrar para cada clasificacion.</summary>
    /// <remarks>
    /// Se centraliza aqui para que la interfaz web, la API y las pruebas funcionales usen
    /// literalmente la misma cadena y no aparezcan variantes redactadas a mano.
    /// </remarks>
    public string EtiquetaClasificacion => Clasificacion switch
    {
        ClasificacionAhorro.SinOfertasValidas => "Sin ofertas validas",
        ClasificacionAhorro.OfertaConveniente => "Oferta conveniente",
        ClasificacionAhorro.OfertaAceptable => "Oferta aceptable",
        ClasificacionAhorro.OfertaValidaSinAhorro => "Oferta valida sin ahorro",
        _ => "Sin ofertas validas"
    };

    /// <summary>Indica si la licitacion tiene al menos una oferta valida.</summary>
    public bool TieneOfertas => MejorOferta is not null;
}
