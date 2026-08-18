using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Domain.Enums;

namespace Licitaciones.Application.Servicios;

/// <summary>Casos de uso del modulo de licitaciones.</summary>
public interface ILicitacionServicio
{
    /// <summary>Devuelve una pagina de licitaciones.</summary>
    /// <param name="parametros">Parametros de paginacion, filtrado y ordenamiento.</param>
    /// <param name="estado">Estado por el que filtrar, o <c>null</c>.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La pagina de resultados.</returns>
    Task<PaginaResultado<LicitacionResumenDto>> ListarAsync(
        ParametrosConsulta parametros,
        EstadoLicitacion? estado = null,
        CancellationToken cancelacion = default);

    /// <summary>Consulta el detalle completo de una licitacion, con evaluacion de ofertas.</summary>
    /// <param name="id">Identificador de la licitacion.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El detalle de la licitacion.</returns>
    Task<LicitacionDetalleDto> ObtenerDetalleAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Crea una licitacion en estado Borrador.</summary>
    /// <param name="peticion">Datos de la licitacion.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El detalle de la licitacion creada.</returns>
    Task<LicitacionDetalleDto> CrearAsync(
        CrearLicitacionRequest peticion,
        CancellationToken cancelacion = default);

    /// <summary>Modifica una licitacion.</summary>
    /// <param name="id">Identificador de la licitacion.</param>
    /// <param name="peticion">Nuevos datos.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El detalle de la licitacion actualizada.</returns>
    Task<LicitacionDetalleDto> ActualizarAsync(
        Guid id,
        ActualizarLicitacionRequest peticion,
        CancellationToken cancelacion = default);

    /// <summary>Aplica una transicion de estado.</summary>
    /// <param name="id">Identificador de la licitacion.</param>
    /// <param name="peticion">Estado destino.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El detalle de la licitacion tras la transicion.</returns>
    Task<LicitacionDetalleDto> CambiarEstadoAsync(
        Guid id,
        CambiarEstadoRequest peticion,
        CancellationToken cancelacion = default);

    /// <summary>Elimina una licitacion de forma fisica o logica segun tenga ofertas asociadas.</summary>
    /// <param name="id">Identificador de la licitacion.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns><c>true</c> si el borrado fue logico, <c>false</c> si fue fisico.</returns>
    Task<bool> EliminarAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Devuelve la evaluacion de ofertas de una licitacion.</summary>
    /// <param name="id">Identificador de la licitacion.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Mejor oferta, ahorro, clasificacion y aprobador.</returns>
    Task<EvaluacionLicitacionDto> ObtenerMejorOfertaAsync(
        Guid id,
        CancellationToken cancelacion = default);
}
