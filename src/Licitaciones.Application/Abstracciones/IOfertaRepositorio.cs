using Licitaciones.Application.Comun;
using Licitaciones.Domain.Entidades;

namespace Licitaciones.Application.Abstracciones;

/// <summary>Puerto de acceso a datos de ofertas.</summary>
public interface IOfertaRepositorio
{
    /// <summary>Obtiene una oferta por identificador.</summary>
    /// <param name="id">Identificador de la oferta.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La oferta, o <c>null</c> si no existe.</returns>
    Task<Oferta?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Indica si el proveedor ya oferto en esa licitacion.</summary>
    /// <param name="licitacionId">Identificador de la licitacion.</param>
    /// <param name="proveedorId">Identificador del proveedor.</param>
    /// <param name="idExcluido">Identificador de oferta a excluir, util al editar.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns><c>true</c> si ya existe una oferta de ese proveedor para esa licitacion.</returns>
    /// <remarks>Respalda el indice unico compuesto exigido en la seccion 8.3.</remarks>
    Task<bool> ExisteOfertaDeProveedorAsync(
        Guid licitacionId,
        Guid proveedorId,
        Guid? idExcluido = null,
        CancellationToken cancelacion = default);

    /// <summary>Devuelve las ofertas de una licitacion.</summary>
    /// <param name="licitacionId">Identificador de la licitacion.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Ofertas registradas, ordenadas por monto ascendente.</returns>
    Task<IReadOnlyList<Oferta>> ListarPorLicitacionAsync(
        Guid licitacionId,
        CancellationToken cancelacion = default);

    /// <summary>Devuelve una pagina de ofertas con filtros opcionales.</summary>
    /// <param name="parametros">Parametros de paginacion, filtrado y ordenamiento.</param>
    /// <param name="licitacionId">Filtro por licitacion, o <c>null</c>.</param>
    /// <param name="proveedorId">Filtro por proveedor, o <c>null</c>.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La pagina de resultados.</returns>
    Task<PaginaResultado<Oferta>> ListarAsync(
        ParametrosConsulta parametros,
        Guid? licitacionId = null,
        Guid? proveedorId = null,
        CancellationToken cancelacion = default);

    /// <summary>Devuelve el monto de la oferta mas alta registrada para una licitacion.</summary>
    /// <param name="licitacionId">Identificador de la licitacion.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El monto mayor, o <c>null</c> si la licitacion no tiene ofertas.</returns>
    /// <remarks>
    /// Lo consume la regla de la seccion 8.5 que impide reducir el presupuesto por debajo de
    /// una oferta existente. Se resuelve con una agregacion en la base de datos en lugar de
    /// traer todas las ofertas a memoria.
    /// </remarks>
    Task<decimal?> ObtenerMayorMontoAsync(Guid licitacionId, CancellationToken cancelacion = default);

    /// <summary>Indica si una licitacion tiene al menos una oferta.</summary>
    /// <param name="licitacionId">Identificador de la licitacion.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns><c>true</c> si existe alguna oferta asociada.</returns>
    Task<bool> LicitacionTieneOfertasAsync(Guid licitacionId, CancellationToken cancelacion = default);

    /// <summary>Indica si un proveedor tiene al menos una oferta.</summary>
    /// <param name="proveedorId">Identificador del proveedor.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns><c>true</c> si existe alguna oferta asociada.</returns>
    Task<bool> ProveedorTieneOfertasAsync(Guid proveedorId, CancellationToken cancelacion = default);

    /// <summary>Agrega una oferta nueva al contexto.</summary>
    /// <param name="oferta">Oferta a agregar.</param>
    void Agregar(Oferta oferta);

    /// <summary>Elimina fisicamente una oferta del contexto.</summary>
    /// <param name="oferta">Oferta a eliminar.</param>
    void Eliminar(Oferta oferta);
}
