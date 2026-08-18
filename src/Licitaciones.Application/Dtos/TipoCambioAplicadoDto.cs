namespace Licitaciones.Application.Dtos;

/// <summary>
/// Tipo de cambio usado para calcular los equivalentes en dolares de una respuesta.
/// </summary>
/// <param name="CrcPorUsd">Colones por dolar aplicados.</param>
/// <param name="FechaVigencia">Fecha desde la que rige el tipo de cambio.</param>
/// <remarks>
/// La seccion 8.8 exige mostrar la fecha del tipo de cambio utilizado, por lo que viaja junto
/// a los montos convertidos en lugar de tener que consultarse aparte.
/// </remarks>
public sealed record TipoCambioAplicadoDto(decimal CrcPorUsd, DateTimeOffset FechaVigencia);
