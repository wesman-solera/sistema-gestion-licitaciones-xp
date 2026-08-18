namespace Licitaciones.Application.Dtos;

/// <summary>
/// Representacion de un monto en las dos monedas que maneja la interfaz.
/// </summary>
/// <param name="Crc">Valor oficial almacenado, en colones costarricenses.</param>
/// <param name="Usd">
/// Equivalente en dolares calculado al momento de responder, o <c>null</c> si no hay
/// tipo de cambio activo configurado.
/// </param>
/// <remarks>
/// El colon es la fuente de verdad (seccion 8.8): el valor en dolares nunca se persiste ni se
/// usa para tomar decisiones de negocio, solo para mostrarse. Se envian ambos en la misma
/// respuesta para que el boton de alternar moneda funcione sin una segunda peticion.
/// </remarks>
public sealed record MontoDto(decimal Crc, decimal? Usd);
