namespace Licitaciones.Domain.Enums;

/// <summary>
/// Estados posibles del ciclo de vida de una licitacion.
/// </summary>
/// <remarks>
/// Los valores numericos se fijan de forma explicita porque se persisten en PostgreSQL
/// como <c>integer</c>. Cambiarlos invalidaria los datos existentes.
/// </remarks>
public enum EstadoLicitacion
{
    /// <summary>Licitacion en preparacion. Admite edicion completa y no acepta ofertas.</summary>
    Borrador = 0,

    /// <summary>Licitacion abierta al mercado. Acepta ofertas hasta la fecha de cierre.</summary>
    Publicada = 1,

    /// <summary>Licitacion finalizada. No acepta ofertas nuevas ni modificacion de las existentes.</summary>
    Cerrada = 2
}
