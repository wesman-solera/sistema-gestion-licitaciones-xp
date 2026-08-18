using Licitaciones.Application.Dtos;

namespace Licitaciones.Web.Modelos;

/// <summary>Modelo de vista de la landing page.</summary>
public sealed class InicioViewModel
{
    /// <summary>
    /// Tipo de cambio en uso, o <c>null</c> si todavia no se configuro ninguno.
    /// </summary>
    /// <remarks>
    /// La portada lo muestra junto a su fecha porque la seccion 8.8 exige que el usuario sepa
    /// con que valor se estan calculando las conversiones que ve.
    /// </remarks>
    public TipoCambioDto? TipoCambioActivo { get; init; }
}
