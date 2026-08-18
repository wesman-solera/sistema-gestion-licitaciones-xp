using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;

namespace Licitaciones.Application.Servicios;

/// <summary>Casos de uso del modulo de niveles de aprobacion.</summary>
public interface INivelAprobacionServicio
{
    /// <summary>Devuelve una pagina de rangos de aprobacion.</summary>
    /// <param name="parametros">Parametros de paginacion y ordenamiento.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La pagina de resultados.</returns>
    Task<PaginaResultado<NivelAprobacionDto>> ListarAsync(
        ParametrosConsulta parametros,
        CancellationToken cancelacion = default);

    /// <summary>Consulta un rango por identificador.</summary>
    /// <param name="id">Identificador del rango.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El rango solicitado.</returns>
    Task<NivelAprobacionDto> ObtenerAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Crea un rango de aprobacion.</summary>
    /// <param name="peticion">Datos del rango.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El rango creado.</returns>
    Task<NivelAprobacionDto> CrearAsync(
        CrearNivelAprobacionRequest peticion,
        CancellationToken cancelacion = default);

    /// <summary>Modifica un rango de aprobacion.</summary>
    /// <param name="id">Identificador del rango.</param>
    /// <param name="peticion">Nuevos datos.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El rango actualizado.</returns>
    Task<NivelAprobacionDto> ActualizarAsync(
        Guid id,
        ActualizarNivelAprobacionRequest peticion,
        CancellationToken cancelacion = default);

    /// <summary>Elimina un rango de aprobacion.</summary>
    /// <param name="id">Identificador del rango.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Tarea que se completa cuando el rango fue eliminado.</returns>
    Task EliminarAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Consulta que aprobador corresponde a un monto.</summary>
    /// <param name="montoCrc">Monto a clasificar, en colones.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El aprobador aplicable, o <c>null</c> si ningun rango cubre el monto.</returns>
    Task<ConsultaAprobadorDto> ConsultarAprobadorAsync(
        decimal montoCrc,
        CancellationToken cancelacion = default);
}
