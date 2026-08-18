using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;

namespace Licitaciones.Application.Servicios;

/// <summary>Casos de uso del modulo de ofertas.</summary>
public interface IOfertaServicio
{
    /// <summary>Devuelve una pagina de ofertas con filtros opcionales.</summary>
    /// <param name="parametros">Parametros de paginacion, filtrado y ordenamiento.</param>
    /// <param name="licitacionId">Filtro por licitacion, o <c>null</c>.</param>
    /// <param name="proveedorId">Filtro por proveedor, o <c>null</c>.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La pagina de resultados.</returns>
    Task<PaginaResultado<OfertaDto>> ListarAsync(
        ParametrosConsulta parametros,
        Guid? licitacionId = null,
        Guid? proveedorId = null,
        CancellationToken cancelacion = default);

    /// <summary>Devuelve las ofertas de una licitacion.</summary>
    /// <param name="licitacionId">Identificador de la licitacion.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Ofertas ordenadas por monto ascendente, marcando la mejor.</returns>
    Task<IReadOnlyList<OfertaDto>> ListarPorLicitacionAsync(
        Guid licitacionId,
        CancellationToken cancelacion = default);

    /// <summary>Consulta una oferta por identificador.</summary>
    /// <param name="id">Identificador de la oferta.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La oferta solicitada.</returns>
    Task<OfertaDto> ObtenerAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Registra una oferta.</summary>
    /// <param name="peticion">Datos de la oferta.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La oferta registrada.</returns>
    Task<OfertaDto> RegistrarAsync(CrearOfertaRequest peticion, CancellationToken cancelacion = default);

    /// <summary>Modifica el monto de una oferta.</summary>
    /// <param name="id">Identificador de la oferta.</param>
    /// <param name="peticion">Nuevo monto.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La oferta actualizada.</returns>
    Task<OfertaDto> ActualizarAsync(
        Guid id,
        ActualizarOfertaRequest peticion,
        CancellationToken cancelacion = default);

    /// <summary>Elimina una oferta.</summary>
    /// <param name="id">Identificador de la oferta.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Tarea que se completa cuando la oferta fue eliminada.</returns>
    Task EliminarAsync(Guid id, CancellationToken cancelacion = default);
}
