namespace Licitaciones.Application.Dtos;

/// <summary>Datos necesarios para crear un tipo de cambio.</summary>
/// <param name="CrcPorUsd">Colones que equivalen a un dolar.</param>
/// <param name="FechaVigencia">Fecha desde la que rige.</param>
/// <param name="Activo">Indica si debe quedar activo de inmediato.</param>
public sealed record CrearTipoCambioRequest(
    decimal CrcPorUsd,
    DateTimeOffset FechaVigencia,
    bool Activo);

/// <summary>Datos necesarios para modificar un tipo de cambio.</summary>
/// <param name="CrcPorUsd">Nuevo valor en colones por dolar.</param>
/// <param name="FechaVigencia">Nueva fecha de vigencia.</param>
public sealed record ActualizarTipoCambioRequest(
    decimal CrcPorUsd,
    DateTimeOffset FechaVigencia);

/// <summary>Proyeccion de lectura de un tipo de cambio.</summary>
/// <param name="Id">Identificador generado por el sistema.</param>
/// <param name="CrcPorUsd">Colones por dolar.</param>
/// <param name="FechaVigencia">Fecha desde la que rige, en UTC.</param>
/// <param name="Activo">Indica si es el tipo de cambio en uso.</param>
/// <param name="CreatedAt">Instante de creacion, en UTC.</param>
/// <param name="UpdatedAt">Instante de la ultima modificacion, en UTC.</param>
public sealed record TipoCambioDto(
    Guid Id,
    decimal CrcPorUsd,
    DateTimeOffset FechaVigencia,
    bool Activo,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
