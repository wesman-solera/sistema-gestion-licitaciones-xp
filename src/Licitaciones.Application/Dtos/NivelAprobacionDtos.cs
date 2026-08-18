namespace Licitaciones.Application.Dtos;

/// <summary>Datos necesarios para crear un rango de aprobacion.</summary>
/// <param name="MontoMinimoCrc">Monto minimo inclusivo, en colones.</param>
/// <param name="MontoMaximoCrc">Monto maximo inclusivo, o <c>null</c> para el rango abierto.</param>
/// <param name="Aprobador">Cargo o instancia responsable.</param>
public sealed record CrearNivelAprobacionRequest(
    decimal MontoMinimoCrc,
    decimal? MontoMaximoCrc,
    string Aprobador);

/// <summary>Datos necesarios para modificar un rango de aprobacion.</summary>
/// <param name="MontoMinimoCrc">Nuevo monto minimo inclusivo.</param>
/// <param name="MontoMaximoCrc">Nuevo monto maximo inclusivo, o <c>null</c>.</param>
/// <param name="Aprobador">Nuevo cargo responsable.</param>
public sealed record ActualizarNivelAprobacionRequest(
    decimal MontoMinimoCrc,
    decimal? MontoMaximoCrc,
    string Aprobador);

/// <summary>Proyeccion de lectura de un rango de aprobacion.</summary>
/// <param name="Id">Identificador generado por el sistema.</param>
/// <param name="MontoMinimo">Monto minimo en colones y su equivalente en dolares.</param>
/// <param name="MontoMaximo">Monto maximo, o <c>null</c> si el rango es abierto.</param>
/// <param name="Aprobador">Cargo responsable.</param>
/// <param name="EsRangoAbierto">Indica si el rango no tiene limite superior.</param>
/// <param name="CreatedAt">Instante de creacion, en UTC.</param>
/// <param name="UpdatedAt">Instante de la ultima modificacion, en UTC.</param>
public sealed record NivelAprobacionDto(
    Guid Id,
    MontoDto MontoMinimo,
    MontoDto? MontoMaximo,
    string Aprobador,
    bool EsRangoAbierto,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Consulta puntual del aprobador que corresponde a un monto.</summary>
/// <param name="MontoCrc">Monto consultado, en colones.</param>
/// <param name="Aprobador">Cargo responsable, o <c>null</c> si ningun rango lo cubre.</param>
/// <param name="NivelAprobacionId">Identificador del rango aplicado.</param>
public sealed record ConsultaAprobadorDto(
    decimal MontoCrc,
    string? Aprobador,
    Guid? NivelAprobacionId);
