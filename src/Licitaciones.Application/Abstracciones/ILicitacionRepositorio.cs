using Licitaciones.Application.Comun;
using Licitaciones.Domain.Entidades;

namespace Licitaciones.Application.Abstracciones;

/// <summary>
/// Puerto de acceso a datos de licitaciones.
/// </summary>
/// <remarks>
/// La capa de aplicacion depende de esta interfaz y no de Entity Framework Core: eso permite
/// sustituir la implementacion en las pruebas unitarias y mantiene la regla de dependencias
/// apuntando siempre hacia el dominio.
/// </remarks>
public interface ILicitacionRepositorio
{
    /// <summary>Obtiene una licitacion por identificador.</summary>
    /// <param name="id">Identificador de la licitacion.</param>
    /// <param name="incluirEliminadas">Si es <c>true</c>, tambien devuelve las eliminadas logicamente.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La licitacion, o <c>null</c> si no existe.</returns>
    Task<Licitacion?> ObtenerPorIdAsync(
        Guid id,
        bool incluirEliminadas = false,
        CancellationToken cancelacion = default);

    /// <summary>Obtiene una licitacion junto con sus ofertas cargadas.</summary>
    /// <param name="id">Identificador de la licitacion.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La licitacion con sus ofertas, o <c>null</c> si no existe.</returns>
    Task<Licitacion?> ObtenerConOfertasAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Indica si ya existe otra licitacion con el mismo codigo normalizado.</summary>
    /// <param name="codigoNormalizado">Codigo ya normalizado.</param>
    /// <param name="idExcluido">Identificador a excluir de la busqueda, util al editar.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns><c>true</c> si el codigo ya esta en uso.</returns>
    Task<bool> ExisteCodigoAsync(
        string codigoNormalizado,
        Guid? idExcluido = null,
        CancellationToken cancelacion = default);

    /// <summary>Devuelve una pagina de licitaciones aplicando filtro y ordenamiento.</summary>
    /// <param name="parametros">Parametros de paginacion, filtrado y ordenamiento.</param>
    /// <param name="estado">Estado por el que filtrar, o <c>null</c> para no filtrar.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La pagina de resultados.</returns>
    Task<PaginaResultado<Licitacion>> ListarAsync(
        ParametrosConsulta parametros,
        Domain.Enums.EstadoLicitacion? estado = null,
        CancellationToken cancelacion = default);

    /// <summary>Agrega una licitacion nueva al contexto.</summary>
    /// <param name="licitacion">Licitacion a agregar.</param>
    void Agregar(Licitacion licitacion);

    /// <summary>Elimina fisicamente una licitacion del contexto.</summary>
    /// <param name="licitacion">Licitacion a eliminar.</param>
    /// <remarks>Solo debe usarse cuando se comprobo que no tiene ofertas asociadas (seccion 8.9).</remarks>
    void Eliminar(Licitacion licitacion);
}
