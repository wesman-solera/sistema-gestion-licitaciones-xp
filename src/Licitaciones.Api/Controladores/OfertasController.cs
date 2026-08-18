using Asp.Versioning;
using Licitaciones.Api.Comun;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Application.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Api.Controladores;

/// <summary>Endpoints REST del modulo de ofertas.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ofertas")]
[Produces("application/json")]
public sealed class OfertasController : ControllerBase
{
    private readonly IOfertaServicio _servicio;

    /// <summary>Inicializa el controlador.</summary>
    /// <param name="servicio">Servicio de aplicacion de ofertas.</param>
    public OfertasController(IOfertaServicio servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Lista las ofertas con filtros opcionales por licitacion y proveedor.</summary>
    /// <param name="parametros">Parametros de consulta.</param>
    /// <param name="licitacionId">Filtro opcional por licitacion.</param>
    /// <param name="proveedorId">Filtro opcional por proveedor.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Pagina de ofertas.</returns>
    /// <response code="200">Devuelve la pagina solicitada.</response>
    [HttpGet(Name = "ListarOfertas")]
    [ProducesResponseType(typeof(PaginaResultado<OfertaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginaResultado<OfertaDto>>> Listar(
        [FromQuery] ParametrosConsultaApi parametros,
        [FromQuery] Guid? licitacionId,
        [FromQuery] Guid? proveedorId,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(parametros);

        var resultado = await _servicio.ListarAsync(
            parametros.AParametrosConsulta(),
            licitacionId,
            proveedorId,
            cancelacion);

        return Ok(resultado);
    }

    /// <summary>Consulta una oferta.</summary>
    /// <param name="id">Identificador de la oferta.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La oferta solicitada.</returns>
    /// <response code="200">Devuelve la oferta.</response>
    /// <response code="404">La oferta no existe.</response>
    [HttpGet("{id:guid}", Name = "ObtenerOferta")]
    [ProducesResponseType(typeof(OfertaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OfertaDto>> Obtener(Guid id, CancellationToken cancelacion)
    {
        return Ok(await _servicio.ObtenerAsync(id, cancelacion));
    }

    /// <summary>Registra una oferta indicando la licitacion en el cuerpo.</summary>
    /// <param name="peticion">Licitacion, proveedor y monto.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La oferta registrada.</returns>
    /// <response code="201">La oferta se registro correctamente.</response>
    /// <response code="400">Los datos de entrada no superaron la validacion.</response>
    /// <response code="404">La licitacion o el proveedor no existen.</response>
    /// <response code="409">El proveedor ya oferto en esa licitacion.</response>
    /// <response code="422">La oferta supera el presupuesto, esta vencida o la licitacion no esta publicada.</response>
    [HttpPost(Name = "CrearOferta")]
    [ProducesResponseType(typeof(OfertaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OfertaDto>> Crear(
        [FromBody] CrearOfertaRequest peticion,
        CancellationToken cancelacion)
    {
        OfertaDto creada = await _servicio.RegistrarAsync(peticion, cancelacion);

        return CreatedAtRoute("ObtenerOferta", new { id = creada.Id, version = "1.0" }, creada);
    }

    /// <summary>Modifica el monto de una oferta.</summary>
    /// <param name="id">Identificador de la oferta.</param>
    /// <param name="peticion">Nuevo monto.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La oferta actualizada.</returns>
    /// <response code="200">La oferta se actualizo correctamente.</response>
    /// <response code="400">El monto no supero la validacion.</response>
    /// <response code="404">La oferta no existe.</response>
    /// <response code="422">La licitacion esta cerrada o vencida, o el monto supera el presupuesto.</response>
    [HttpPut("{id:guid}", Name = "ActualizarOferta")]
    [ProducesResponseType(typeof(OfertaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OfertaDto>> Actualizar(
        Guid id,
        [FromBody] ActualizarOfertaRequest peticion,
        CancellationToken cancelacion)
    {
        return Ok(await _servicio.ActualizarAsync(id, peticion, cancelacion));
    }

    /// <summary>Elimina una oferta.</summary>
    /// <param name="id">Identificador de la oferta.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Sin contenido.</returns>
    /// <response code="204">La oferta se elimino correctamente.</response>
    /// <response code="404">La oferta no existe.</response>
    /// <response code="422">La oferta pertenece a una licitacion cerrada o vencida.</response>
    [HttpDelete("{id:guid}", Name = "EliminarOferta")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        await _servicio.EliminarAsync(id, cancelacion);

        return NoContent();
    }
}
