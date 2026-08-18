namespace Licitaciones.Application.Dtos;

/// <summary>Datos necesarios para registrar una oferta indicando la licitacion en el cuerpo.</summary>
/// <param name="LicitacionId">Licitacion a la que se oferta.</param>
/// <param name="ProveedorId">Proveedor que presenta la oferta.</param>
/// <param name="MontoOfertadoCrc">Monto propuesto en colones.</param>
public sealed record CrearOfertaRequest(
    Guid LicitacionId,
    Guid ProveedorId,
    decimal MontoOfertadoCrc);

/// <summary>
/// Datos necesarios para registrar una oferta cuando la licitacion viaja en la ruta.
/// </summary>
/// <param name="ProveedorId">Proveedor que presenta la oferta.</param>
/// <param name="MontoOfertadoCrc">Monto propuesto en colones.</param>
/// <remarks>
/// Corresponde al endpoint <c>POST /api/v1/licitaciones/{id}/ofertas</c>. Se separa del DTO
/// anterior para que el identificador de licitacion no pueda enviarse dos veces con valores
/// distintos entre la ruta y el cuerpo.
/// </remarks>
public sealed record RegistrarOfertaEnLicitacionRequest(
    Guid ProveedorId,
    decimal MontoOfertadoCrc);

/// <summary>Datos necesarios para modificar una oferta.</summary>
/// <param name="MontoOfertadoCrc">Nuevo monto propuesto en colones.</param>
public sealed record ActualizarOfertaRequest(decimal MontoOfertadoCrc);

/// <summary>Proyeccion de lectura de una oferta.</summary>
/// <param name="Id">Identificador generado por el sistema.</param>
/// <param name="LicitacionId">Licitacion asociada.</param>
/// <param name="CodigoLicitacion">Codigo de la licitacion, para mostrarlo sin otra consulta.</param>
/// <param name="ProveedorId">Proveedor oferente.</param>
/// <param name="NombreProveedor">Nombre del proveedor, para mostrarlo sin otra consulta.</param>
/// <param name="Monto">Monto ofertado en colones y su equivalente en dolares.</param>
/// <param name="FechaRegistro">Instante de registro, en UTC. Define el desempate.</param>
/// <param name="UpdatedAt">Instante de la ultima modificacion, en UTC.</param>
/// <param name="EsMejorOferta">Indica si esta oferta es la ganadora de su licitacion.</param>
public sealed record OfertaDto(
    Guid Id,
    Guid LicitacionId,
    string CodigoLicitacion,
    Guid ProveedorId,
    string NombreProveedor,
    MontoDto Monto,
    DateTimeOffset FechaRegistro,
    DateTimeOffset UpdatedAt,
    bool EsMejorOferta);
