using Licitaciones.Application.Comun;
using Licitaciones.Domain.Entidades;

namespace Licitaciones.Application.Abstracciones;

/// <summary>Puerto de acceso a datos de los tipos de cambio.</summary>
public interface ITipoCambioRepositorio
{
    /// <summary>Obtiene un tipo de cambio por identificador.</summary>
    /// <param name="id">Identificador del tipo de cambio.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El tipo de cambio, o <c>null</c> si no existe.</returns>
    Task<TipoCambio?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Obtiene el unico tipo de cambio marcado como activo.</summary>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El tipo de cambio activo, o <c>null</c> si todavia no se configuro ninguno.</returns>
    Task<TipoCambio?> ObtenerActivoAsync(CancellationToken cancelacion = default);

    /// <summary>Devuelve todos los tipos de cambio marcados como activos.</summary>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Los registros activos.</returns>
    /// <remarks>
    /// Solo deberia devolver uno. Se usa al activar otro registro para desactivar el anterior
    /// dentro de la misma transaccion y mantener la invariante de un unico activo.
    /// </remarks>
    Task<IReadOnlyList<TipoCambio>> ListarActivosAsync(CancellationToken cancelacion = default);

    /// <summary>Devuelve una pagina de tipos de cambio.</summary>
    /// <param name="parametros">Parametros de paginacion y ordenamiento.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La pagina de resultados.</returns>
    Task<PaginaResultado<TipoCambio>> ListarAsync(
        ParametrosConsulta parametros,
        CancellationToken cancelacion = default);

    /// <summary>Agrega un tipo de cambio nuevo al contexto.</summary>
    /// <param name="tipoCambio">Tipo de cambio a agregar.</param>
    void Agregar(TipoCambio tipoCambio);

    /// <summary>Elimina un tipo de cambio del contexto.</summary>
    /// <param name="tipoCambio">Tipo de cambio a eliminar.</param>
    void Eliminar(TipoCambio tipoCambio);
}
