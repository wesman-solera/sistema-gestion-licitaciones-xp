using Asp.Versioning;
using Licitaciones.Api.Comun;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Application.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Api.Controladores;

/// <summary>Endpoints REST del modulo de proveedores.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/proveedores")]
[Produces("application/json")]
public sealed class ProveedoresController : ControllerBase
{
    private readonly IProveedorServicio _servicio;
    private readonly IOfertaServicio _ofertas;

    /// <summary>Inicializa el controlador.</summary>
    /// <param name="servicio">Servicio de aplicacion de proveedores.</param>
    /// <param name="ofertas">Servicio de aplicacion de ofertas.</param>
    public ProveedoresController(IProveedorServicio servicio, IOfertaServicio ofertas)
    {
        _servicio = servicio;
        _ofertas = ofertas;
    }

    /// <summary>Lista los proveedores con paginacion, filtrado y ordenamiento.</summary>
    /// <param name="parametros">Parametros de consulta.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Pagina de proveedores.</returns>
    /// <response code="200">Devuelve la pagina solicitada.</response>
    [HttpGet(Name = "ListarProveedores")]
    [ProducesResponseType(typeof(PaginaResultado<ProveedorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginaResultado<ProveedorDto>>> Listar(
        [FromQuery] ParametrosConsultaApi parametros,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(parametros);

        return Ok(await _servicio.ListarAsync(parametros.AParametrosConsulta(), cancelacion));
    }

    /// <summary>Consulta un proveedor.</summary>
    /// <param name="id">Identificador del proveedor.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El proveedor solicitado.</returns>
    /// <response code="200">Devuelve el proveedor.</response>
    /// <response code="404">El proveedor no existe.</response>
    [HttpGet("{id:guid}", Name = "ObtenerProveedor")]
    [ProducesResponseType(typeof(ProveedorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProveedorDto>> Obtener(Guid id, CancellationToken cancelacion)
    {
        return Ok(await _servicio.ObtenerAsync(id, cancelacion));
    }

    /// <summary>Lista las ofertas presentadas por un proveedor.</summary>
    /// <param name="id">Identificador del proveedor.</param>
    /// <param name="parametros">Parametros de consulta.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Pagina de ofertas del proveedor.</returns>
    /// <response code="200">Devuelve las ofertas del proveedor.</response>
    [HttpGet("{id:guid}/ofertas", Name = "ListarOfertasDeProveedor")]
    [ProducesResponseType(typeof(PaginaResultado<OfertaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginaResultado<OfertaDto>>> ListarOfertas(
        Guid id,
        [FromQuery] ParametrosConsultaApi parametros,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(parametros);

        var resultado = await _ofertas.ListarAsync(
            parametros.AParametrosConsulta(),
            licitacionId: null,
            proveedorId: id,
            cancelacion);

        return Ok(resultado);
    }

    /// <summary>Crea un proveedor.</summary>
    /// <param name="peticion">Datos del proveedor.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El proveedor creado.</returns>
    /// <response code="201">El proveedor se creo correctamente.</response>
    /// <response code="400">El nombre esta vacio o usa caracteres no permitidos.</response>
    /// <response code="409">Ya existe un proveedor con ese nombre normalizado.</response>
    [HttpPost(Name = "CrearProveedor")]
    [ProducesResponseType(typeof(ProveedorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProveedorDto>> Crear(
        [FromBody] CrearProveedorRequest peticion,
        CancellationToken cancelacion)
    {
        ProveedorDto creado = await _servicio.CrearAsync(peticion, cancelacion);

        return CreatedAtRoute("ObtenerProveedor", new { id = creado.Id, version = "1.0" }, creado);
    }

    /// <summary>Modifica un proveedor.</summary>
    /// <param name="id">Identificador del proveedor.</param>
    /// <param name="peticion">Nuevos datos.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El proveedor actualizado.</returns>
    /// <response code="200">El proveedor se actualizo correctamente.</response>
    /// <response code="400">El nombre esta vacio o usa caracteres no permitidos.</response>
    /// <response code="404">El proveedor no existe.</response>
    /// <response code="409">Ya existe otro proveedor con ese nombre normalizado.</response>
    [HttpPut("{id:guid}", Name = "ActualizarProveedor")]
    [ProducesResponseType(typeof(ProveedorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProveedorDto>> Actualizar(
        Guid id,
        [FromBody] ActualizarProveedorRequest peticion,
        CancellationToken cancelacion)
    {
        return Ok(await _servicio.ActualizarAsync(id, peticion, cancelacion));
    }

    /// <summary>Elimina un proveedor.</summary>
    /// <param name="id">Identificador del proveedor.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Sin contenido.</returns>
    /// <response code="204">El proveedor se elimino. Si tenia ofertas, el borrado fue logico.</response>
    /// <response code="404">El proveedor no existe.</response>
    [HttpDelete("{id:guid}", Name = "EliminarProveedor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        bool borradoLogico = await _servicio.EliminarAsync(id, cancelacion);

        Response.Headers.Append("X-Tipo-Borrado", borradoLogico ? "logico" : "fisico");

        return NoContent();
    }
}
