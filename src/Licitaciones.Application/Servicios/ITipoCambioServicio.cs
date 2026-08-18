using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;

namespace Licitaciones.Application.Servicios;

/// <summary>Casos de uso del modulo de tipos de cambio.</summary>
public interface ITipoCambioServicio
{
    /// <summary>Devuelve una pagina de tipos de cambio.</summary>
    /// <param name="parametros">Parametros de paginacion y ordenamiento.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La pagina de resultados.</returns>
    Task<PaginaResultado<TipoCambioDto>> ListarAsync(
        ParametrosConsulta parametros,
        CancellationToken cancelacion = default);

    /// <summary>Consulta un tipo de cambio por identificador.</summary>
    /// <param name="id">Identificador del tipo de cambio.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El tipo de cambio solicitado.</returns>
    Task<TipoCambioDto> ObtenerAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Devuelve el tipo de cambio activo.</summary>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El tipo de cambio activo, o <c>null</c> si no hay ninguno configurado.</returns>
    Task<TipoCambioDto?> ObtenerActivoAsync(CancellationToken cancelacion = default);

    /// <summary>Crea un tipo de cambio.</summary>
    /// <param name="peticion">Datos del tipo de cambio.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El tipo de cambio creado.</returns>
    Task<TipoCambioDto> CrearAsync(
        CrearTipoCambioRequest peticion,
        CancellationToken cancelacion = default);

    /// <summary>Modifica un tipo de cambio.</summary>
    /// <param name="id">Identificador del tipo de cambio.</param>
    /// <param name="peticion">Nuevos datos.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El tipo de cambio actualizado.</returns>
    Task<TipoCambioDto> ActualizarAsync(
        Guid id,
        ActualizarTipoCambioRequest peticion,
        CancellationToken cancelacion = default);

    /// <summary>Marca un tipo de cambio como el activo y desactiva el anterior.</summary>
    /// <param name="id">Identificador del tipo de cambio a activar.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El tipo de cambio activado.</returns>
    Task<TipoCambioDto> ActivarAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Elimina un tipo de cambio.</summary>
    /// <param name="id">Identificador del tipo de cambio.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Tarea que se completa cuando el tipo de cambio fue eliminado.</returns>
    Task EliminarAsync(Guid id, CancellationToken cancelacion = default);
}
