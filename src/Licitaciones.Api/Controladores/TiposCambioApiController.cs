using Asp.Versioning;
using Licitaciones.Api.Comun;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Application.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Api.Controladores;

/// <summary>Endpoints REST del modulo de tipos de cambio.</summary>
/// <remarks>
/// El tipo de cambio se administra localmente: la seccion 8.8 exige que la solucion funcione sin
/// Internet, por lo que no existe ninguna integracion con un servicio externo de cotizaciones.
/// </remarks>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tipos-cambio")]
[Produces("application/json")]
public sealed class TiposCambioApiController : ControllerBase
{
    private readonly ITipoCambioServicio _servicio;

    /// <summary>Inicializa el controlador.</summary>
    /// <param name="servicio">Servicio de aplicacion de tipos de cambio.</param>
    public TiposCambioApiController(ITipoCambioServicio servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Lista los tipos de cambio registrados.</summary>
    /// <param name="parametros">Parametros de consulta.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Pagina de tipos de cambio.</returns>
    /// <response code="200">Devuelve la pagina solicitada.</response>
    [HttpGet(Name = "ListarTiposCambio")]
    [ProducesResponseType(typeof(PaginaResultado<TipoCambioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginaResultado<TipoCambioDto>>> Listar(
        [FromQuery] ParametrosConsultaApi parametros,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(parametros);

        return Ok(await _servicio.ListarAsync(parametros.AParametrosConsulta(), cancelacion));
    }

    /// <summary>Consulta el tipo de cambio activo.</summary>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El tipo de cambio en uso.</returns>
    /// <response code="200">Devuelve el tipo de cambio activo.</response>
    /// <response code="404">Todavia no se configuro ningun tipo de cambio activo.</response>
    [HttpGet("activo", Name = "ObtenerTipoCambioActivo")]
    [ProducesResponseType(typeof(TipoCambioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TipoCambioDto>> ObtenerActivo(CancellationToken cancelacion)
    {
        TipoCambioDto? activo = await _servicio.ObtenerActivoAsync(cancelacion);

        return activo is null
            ? Problem(
                title: "Sin tipo de cambio activo",
                detail: "No hay un tipo de cambio activo configurado. Registre uno para habilitar la conversion a dolares.",
                statusCode: StatusCodes.Status404NotFound)
            : Ok(activo);
    }

    /// <summary>Consulta un tipo de cambio.</summary>
    /// <param name="id">Identificador del tipo de cambio.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El tipo de cambio solicitado.</returns>
    /// <response code="200">Devuelve el tipo de cambio.</response>
    /// <response code="404">El tipo de cambio no existe.</response>
    [HttpGet("{id:guid}", Name = "ObtenerTipoCambio")]
    [ProducesResponseType(typeof(TipoCambioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TipoCambioDto>> Obtener(Guid id, CancellationToken cancelacion)
    {
        return Ok(await _servicio.ObtenerAsync(id, cancelacion));
    }

    /// <summary>Crea un tipo de cambio.</summary>
    /// <param name="peticion">Datos del tipo de cambio.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El tipo de cambio creado.</returns>
    /// <response code="201">El tipo de cambio se creo correctamente.</response>
    /// <response code="400">El valor no es mayor que cero o tiene mas de dos decimales.</response>
    [HttpPost(Name = "CrearTipoCambio")]
    [ProducesResponseType(typeof(TipoCambioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TipoCambioDto>> Crear(
        [FromBody] CrearTipoCambioRequest peticion,
        CancellationToken cancelacion)
    {
        TipoCambioDto creado = await _servicio.CrearAsync(peticion, cancelacion);

        return CreatedAtRoute("ObtenerTipoCambio", new { id = creado.Id, version = "1.0" }, creado);
    }

    /// <summary>Modifica un tipo de cambio.</summary>
    /// <param name="id">Identificador del tipo de cambio.</param>
    /// <param name="peticion">Nuevos datos.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El tipo de cambio actualizado.</returns>
    /// <response code="200">El tipo de cambio se actualizo correctamente.</response>
    /// <response code="400">El valor no supero la validacion.</response>
    /// <response code="404">El tipo de cambio no existe.</response>
    [HttpPut("{id:guid}", Name = "ActualizarTipoCambio")]
    [ProducesResponseType(typeof(TipoCambioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TipoCambioDto>> Actualizar(
        Guid id,
        [FromBody] ActualizarTipoCambioRequest peticion,
        CancellationToken cancelacion)
    {
        return Ok(await _servicio.ActualizarAsync(id, peticion, cancelacion));
    }

    /// <summary>Marca un tipo de cambio como el activo.</summary>
    /// <param name="id">Identificador del tipo de cambio a activar.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El tipo de cambio activado.</returns>
    /// <response code="200">El tipo de cambio quedo activo y el anterior se desactivo.</response>
    /// <response code="404">El tipo de cambio no existe.</response>
    /// <remarks>
    /// La activacion y la desactivacion del anterior ocurren dentro de una misma transaccion,
    /// de modo que nunca puede haber cero ni dos tipos de cambio activos.
    /// </remarks>
    [HttpPatch("{id:guid}/activar", Name = "ActivarTipoCambio")]
    [ProducesResponseType(typeof(TipoCambioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TipoCambioDto>> Activar(Guid id, CancellationToken cancelacion)
    {
        return Ok(await _servicio.ActivarAsync(id, cancelacion));
    }

    /// <summary>Elimina un tipo de cambio.</summary>
    /// <param name="id">Identificador del tipo de cambio.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Sin contenido.</returns>
    /// <response code="204">El tipo de cambio se elimino correctamente.</response>
    /// <response code="404">El tipo de cambio no existe.</response>
    /// <response code="422">No se puede eliminar el tipo de cambio activo.</response>
    [HttpDelete("{id:guid}", Name = "EliminarTipoCambio")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        await _servicio.EliminarAsync(id, cancelacion);

        return NoContent();
    }
}
