namespace Licitaciones.Domain.Enums;

/// <summary>
/// Clasificacion cualitativa del ahorro obtenido por la mejor oferta de una licitacion.
/// </summary>
/// <remarks>
/// Los umbrales se definen en la seccion 8.6 del enunciado y se implementan en
/// <see cref="Servicios.EvaluadorOfertas"/>.
/// </remarks>
public enum ClasificacionAhorro
{
    /// <summary>La licitacion no tiene ninguna oferta valida registrada.</summary>
    SinOfertasValidas = 0,

    /// <summary>Ahorro mayor que 0 % y menor que 10 %.</summary>
    OfertaAceptable = 1,

    /// <summary>Ahorro igual o superior al 10 %.</summary>
    OfertaConveniente = 2,

    /// <summary>La mejor oferta iguala exactamente el presupuesto estimado: el ahorro es 0 %.</summary>
    OfertaValidaSinAhorro = 3
}
