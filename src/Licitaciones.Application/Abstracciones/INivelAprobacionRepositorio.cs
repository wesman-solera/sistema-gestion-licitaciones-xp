using Licitaciones.Application.Comun;
using Licitaciones.Domain.Entidades;

namespace Licitaciones.Application.Abstracciones;

/// <summary>Puerto de acceso a datos de los niveles de aprobacion.</summary>
public interface INivelAprobacionRepositorio
{
    /// <summary>Obtiene un nivel por identificador.</summary>
    /// <param name="id">Identificador del nivel.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El nivel, o <c>null</c> si no existe.</returns>
    Task<NivelAprobacion?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Devuelve todos los niveles ordenados por monto minimo.</summary>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Los rangos parametrizados vigentes.</returns>
    /// <remarks>
    /// La tabla es intencionalmente pequena, por lo que se carga completa: la validacion de
    /// traslapes y la seleccion del aprobador necesitan verla entera.
    /// </remarks>
    Task<IReadOnlyList<NivelAprobacion>> ListarTodosAsync(CancellationToken cancelacion = default);

    /// <summary>Devuelve una pagina de niveles de aprobacion.</summary>
    /// <param name="parametros">Parametros de paginacion y ordenamiento.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La pagina de resultados.</returns>
    Task<PaginaResultado<NivelAprobacion>> ListarAsync(
        ParametrosConsulta parametros,
        CancellationToken cancelacion = default);

    /// <summary>Agrega un nivel nuevo al contexto.</summary>
    /// <param name="nivel">Nivel a agregar.</param>
    void Agregar(NivelAprobacion nivel);

    /// <summary>Elimina un nivel del contexto.</summary>
    /// <param name="nivel">Nivel a eliminar.</param>
    void Eliminar(NivelAprobacion nivel);
}
