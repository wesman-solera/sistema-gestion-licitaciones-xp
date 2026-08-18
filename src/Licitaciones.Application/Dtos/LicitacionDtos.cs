using Licitaciones.Domain.Enums;

namespace Licitaciones.Application.Dtos;

/// <summary>Datos necesarios para crear una licitacion.</summary>
/// <param name="Codigo">Codigo unico de la licitacion.</param>
/// <param name="Titulo">Titulo descriptivo.</param>
/// <param name="PresupuestoEstimadoCrc">Presupuesto estimado en colones.</param>
/// <param name="FechaCierre">Fecha y hora de cierre, seleccionada con un control de calendario.</param>
public sealed record CrearLicitacionRequest(
    string Codigo,
    string Titulo,
    decimal PresupuestoEstimadoCrc,
    DateTimeOffset FechaCierre);

/// <summary>Datos necesarios para modificar una licitacion.</summary>
/// <param name="Codigo">Codigo de la licitacion. Solo puede cambiar mientras esta en Borrador.</param>
/// <param name="Titulo">Nuevo titulo.</param>
/// <param name="PresupuestoEstimadoCrc">Nuevo presupuesto en colones.</param>
/// <param name="FechaCierre">Nueva fecha y hora de cierre.</param>
public sealed record ActualizarLicitacionRequest(
    string Codigo,
    string Titulo,
    decimal PresupuestoEstimadoCrc,
    DateTimeOffset FechaCierre);

/// <summary>Solicitud de cambio de estado de una licitacion.</summary>
/// <param name="Estado">Estado destino solicitado.</param>
public sealed record CambiarEstadoRequest(EstadoLicitacion Estado);

/// <summary>Proyeccion resumida de una licitacion, usada en los listados.</summary>
/// <param name="Id">Identificador generado por el sistema.</param>
/// <param name="Codigo">Codigo visible.</param>
/// <param name="Titulo">Titulo descriptivo.</param>
/// <param name="Estado">Estado persistido.</param>
/// <param name="EstadoEfectivo">
/// Estado real considerando el vencimiento: una licitacion Publicada cuya fecha ya paso se
/// reporta como Cerrada (aclaracion de la seccion 8.1).
/// </param>
/// <param name="FechaCierre">Fecha y hora de cierre, en UTC.</param>
/// <param name="PresupuestoEstimado">Presupuesto en colones y su equivalente en dolares.</param>
/// <param name="CantidadOfertas">Cantidad de ofertas registradas.</param>
/// <param name="Eliminada">Indica si fue eliminada logicamente.</param>
/// <param name="CreatedAt">Instante de creacion, en UTC.</param>
/// <param name="UpdatedAt">Instante de la ultima modificacion, en UTC.</param>
public sealed record LicitacionResumenDto(
    Guid Id,
    string Codigo,
    string Titulo,
    EstadoLicitacion Estado,
    EstadoLicitacion EstadoEfectivo,
    DateTimeOffset FechaCierre,
    MontoDto PresupuestoEstimado,
    int CantidadOfertas,
    bool Eliminada,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Proyeccion completa de una licitacion con su evaluacion de ofertas.</summary>
/// <param name="Id">Identificador generado por el sistema.</param>
/// <param name="Codigo">Codigo visible.</param>
/// <param name="Titulo">Titulo descriptivo.</param>
/// <param name="Estado">Estado persistido.</param>
/// <param name="EstadoEfectivo">Estado real considerando el vencimiento.</param>
/// <param name="FechaCierre">Fecha y hora de cierre, en UTC.</param>
/// <param name="PresupuestoEstimado">Presupuesto en colones y su equivalente en dolares.</param>
/// <param name="Eliminada">Indica si fue eliminada logicamente.</param>
/// <param name="TransicionesDisponibles">Estados a los que se puede transicionar desde el actual.</param>
/// <param name="Evaluacion">Mejor oferta, ahorro, clasificacion y nivel de aprobacion.</param>
/// <param name="Ofertas">Ofertas registradas, ordenadas por monto ascendente.</param>
/// <param name="TipoCambioAplicado">Tipo de cambio usado para los equivalentes en dolares.</param>
/// <param name="CreatedAt">Instante de creacion, en UTC.</param>
/// <param name="UpdatedAt">Instante de la ultima modificacion, en UTC.</param>
public sealed record LicitacionDetalleDto(
    Guid Id,
    string Codigo,
    string Titulo,
    EstadoLicitacion Estado,
    EstadoLicitacion EstadoEfectivo,
    DateTimeOffset FechaCierre,
    MontoDto PresupuestoEstimado,
    bool Eliminada,
    IReadOnlyList<EstadoLicitacion> TransicionesDisponibles,
    EvaluacionLicitacionDto Evaluacion,
    IReadOnlyList<OfertaDto> Ofertas,
    TipoCambioAplicadoDto? TipoCambioAplicado,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Resultado del analisis de las ofertas de una licitacion.</summary>
/// <param name="MejorOferta">Oferta ganadora, o <c>null</c> si no hay ofertas.</param>
/// <param name="PorcentajeAhorro">Ahorro respecto del presupuesto, con dos decimales.</param>
/// <param name="Clasificacion">Clasificacion cualitativa del ahorro.</param>
/// <param name="EtiquetaClasificacion">Texto exacto que debe mostrarse al usuario.</param>
/// <param name="CantidadOfertas">Cantidad de ofertas consideradas.</param>
/// <param name="Aprobador">
/// Cargo responsable segun la tabla de niveles de aprobacion, o <c>null</c> si no hay mejor
/// oferta o si ningun rango parametrizado cubre el monto.
/// </param>
/// <param name="NivelAprobacionId">Identificador del rango aplicado, util para trazabilidad.</param>
public sealed record EvaluacionLicitacionDto(
    OfertaDto? MejorOferta,
    decimal? PorcentajeAhorro,
    ClasificacionAhorro Clasificacion,
    string EtiquetaClasificacion,
    int CantidadOfertas,
    string? Aprobador,
    Guid? NivelAprobacionId);
