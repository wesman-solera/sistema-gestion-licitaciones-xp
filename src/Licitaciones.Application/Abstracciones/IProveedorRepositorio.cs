using Licitaciones.Application.Comun;
using Licitaciones.Domain.Entidades;

namespace Licitaciones.Application.Abstracciones;

/// <summary>Puerto de acceso a datos de proveedores.</summary>
public interface IProveedorRepositorio
{
    /// <summary>Obtiene un proveedor por identificador.</summary>
    /// <param name="id">Identificador del proveedor.</param>
    /// <param name="incluirEliminados">Si es <c>true</c>, tambien devuelve los eliminados logicamente.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El proveedor, o <c>null</c> si no existe.</returns>
    Task<Proveedor?> ObtenerPorIdAsync(
        Guid id,
        bool incluirEliminados = false,
        CancellationToken cancelacion = default);

    /// <summary>Indica si ya existe otro proveedor con el mismo nombre normalizado.</summary>
    /// <param name="nombreNormalizado">Nombre ya normalizado.</param>
    /// <param name="idExcluido">Identificador a excluir de la busqueda, util al editar.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns><c>true</c> si el nombre ya esta en uso.</returns>
    Task<bool> ExisteNombreAsync(
        string nombreNormalizado,
        Guid? idExcluido = null,
        CancellationToken cancelacion = default);

    /// <summary>Devuelve una pagina de proveedores aplicando filtro y ordenamiento.</summary>
    /// <param name="parametros">Parametros de paginacion, filtrado y ordenamiento.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La pagina de resultados.</returns>
    Task<PaginaResultado<Proveedor>> ListarAsync(
        ParametrosConsulta parametros,
        CancellationToken cancelacion = default);

    /// <summary>Devuelve todos los proveedores activos, ordenados por nombre.</summary>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Lista de proveedores activos, util para poblar listas desplegables.</returns>
    Task<IReadOnlyList<Proveedor>> ListarActivosAsync(CancellationToken cancelacion = default);

    /// <summary>Cuenta las ofertas de cada proveedor indicado.</summary>
    /// <param name="ids">Identificadores de los proveedores a contar.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Diccionario con la cantidad de ofertas por proveedor.</returns>
    /// <remarks>
    /// Se resuelve con una unica agregacion en la base de datos. La alternativa de cargar la
    /// coleccion de ofertas de cada proveedor produciria un problema N+1 en los listados.
    /// </remarks>
    Task<IReadOnlyDictionary<Guid, int>> ContarOfertasAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancelacion = default);

    /// <summary>Agrega un proveedor nuevo al contexto.</summary>
    /// <param name="proveedor">Proveedor a agregar.</param>
    void Agregar(Proveedor proveedor);

    /// <summary>Elimina fisicamente un proveedor del contexto.</summary>
    /// <param name="proveedor">Proveedor a eliminar.</param>
    /// <remarks>Solo debe usarse cuando se comprobo que no tiene ofertas asociadas (seccion 8.9).</remarks>
    void Eliminar(Proveedor proveedor);
}
