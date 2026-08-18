using Asp.Versioning;
using Licitaciones.Api.Comun;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Application.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Api.Controladores;

/// <summary>Endpoints REST del modulo de niveles de aprobacion.</summary>
/// <remarks>
/// Esta es la tabla parametrizable que exige la seccion 8.7: cambiar quien aprueba un rango de
/// montos es una operacion de datos a traves de esta API, no un cambio de codigo.
/// </remarks>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/niveles-aprobacion")]
[Produces("application/json")]
public sealed class NivelesAprobacionController : ControllerBase
{
    private readonly INivelAprobacionServicio _servicio;

    /// <summary>Inicializa el controlador.</summary>
    /// <param name="servicio">Servicio de aplicacion de niveles de aprobacion.</param>
    public NivelesAprobacionController(INivelAprobacionServicio servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Lista los rangos de aprobacion.</summary>
    /// <param name="parametros">Parametros de consulta.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Pagina de rangos ordenada por monto minimo.</returns>
    /// <response code="200">Devuelve la pagina solicitada.</response>
    [HttpGet(Name = "ListarNivelesAprobacion")]
    [ProducesResponseType(typeof(PaginaResultado<NivelAprobacionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginaResultado<NivelAprobacionDto>>> Listar(
        [FromQuery] ParametrosConsultaApi parametros,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(parametros);

        return Ok(await _servicio.ListarAsync(parametros.AParametrosConsulta(), cancelacion));
    }

    /// <summary>Consulta un rango de aprobacion.</summary>
    /// <param name="id">Identificador del rango.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El rango solicitado.</returns>
    /// <response code="200">Devuelve el rango.</response>
    /// <response code="404">El rango no existe.</response>
    [HttpGet("{id:guid}", Name = "ObtenerNivelAprobacion")]
    [ProducesResponseType(typeof(NivelAprobacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NivelAprobacionDto>> Obtener(
        Guid id,
        CancellationToken cancelacion)
    {
        return Ok(await _servicio.ObtenerAsync(id, cancelacion));
    }

    /// <summary>Consulta que aprobador corresponde a un monto determinado.</summary>
    /// <param name="montoCrc">Monto a clasificar, en colones.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El aprobador aplicable y el rango que lo determina.</returns>
    /// <response code="200">Devuelve el aprobador, o <c>null</c> si ningun rango cubre el monto.</response>
    [HttpGet("aplicable", Name = "ConsultarAprobador")]
    [ProducesResponseType(typeof(ConsultaAprobadorDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ConsultaAprobadorDto>> ConsultarAprobador(
        [FromQuery] decimal montoCrc,
        CancellationToken cancelacion)
    {
        return Ok(await _servicio.ConsultarAprobadorAsync(montoCrc, cancelacion));
    }

    /// <summary>Crea un rango de aprobacion.</summary>
    /// <param name="peticion">Datos del rango.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El rango creado.</returns>
    /// <response code="201">El rango se creo correctamente.</response>
    /// <response code="400">Los datos de entrada no superaron la validacion.</response>
    /// <response code="422">El rango se traslapa con otro, o ya existe un rango abierto.</response>
    [HttpPost(Name = "CrearNivelAprobacion")]
    [ProducesResponseType(typeof(NivelAprobacionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<NivelAprobacionDto>> Crear(
        [FromBody] CrearNivelAprobacionRequest peticion,
        CancellationToken cancelacion)
    {
        NivelAprobacionDto creado = await _servicio.CrearAsync(peticion, cancelacion);

        return CreatedAtRoute(
            "ObtenerNivelAprobacion",
            new { id = creado.Id, version = "1.0" },
            creado);
    }

    /// <summary>Modifica un rango de aprobacion.</summary>
    /// <param name="id">Identificador del rango.</param>
    /// <param name="peticion">Nuevos datos.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El rango actualizado.</returns>
    /// <response code="200">El rango se actualizo correctamente.</response>
    /// <response code="400">Los datos de entrada no superaron la validacion.</response>
    /// <response code="404">El rango no existe.</response>
    /// <response code="422">El rango resultante se traslapa con otro.</response>
    [HttpPut("{id:guid}", Name = "ActualizarNivelAprobacion")]
    [ProducesResponseType(typeof(NivelAprobacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<NivelAprobacionDto>> Actualizar(
        Guid id,
        [FromBody] ActualizarNivelAprobacionRequest peticion,
        CancellationToken cancelacion)
    {
        return Ok(await _servicio.ActualizarAsync(id, peticion, cancelacion));
    }

    /// <summary>Elimina un rango de aprobacion.</summary>
    /// <param name="id">Identificador del rango.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Sin contenido.</returns>
    /// <response code="204">El rango se elimino correctamente.</response>
    /// <response code="404">El rango no existe.</response>
    [HttpDelete("{id:guid}", Name = "EliminarNivelAprobacion")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        await _servicio.EliminarAsync(id, cancelacion);

        return NoContent();
    }
}
