using Asp.Versioning;
using Licitaciones.Api.Comun;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Application.Servicios;
using Licitaciones.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Api.Controladores;

/// <summary>Endpoints REST del modulo de licitaciones.</summary>
/// <remarks>
/// El controlador es deliberadamente delgado (requisito 6.4): traduce la peticion HTTP a una
/// llamada del servicio de aplicacion y el resultado a un codigo de estado. No contiene ninguna
/// regla de negocio ni acceso a datos. Los errores no se capturan aqui: los traduce
/// <see cref="Comun.ManejadorExcepcionesGlobal"/> de forma uniforme para toda la API.
/// </remarks>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/licitaciones")]
[Produces("application/json")]
public sealed class LicitacionesApiController : ControllerBase
{
    private readonly ILicitacionServicio _servicio;
    private readonly IOfertaServicio _ofertas;

    /// <summary>Inicializa el controlador.</summary>
    /// <param name="servicio">Servicio de aplicacion de licitaciones.</param>
    /// <param name="ofertas">Servicio de aplicacion de ofertas.</param>
    public LicitacionesApiController(ILicitacionServicio servicio, IOfertaServicio ofertas)
    {
        _servicio = servicio;
        _ofertas = ofertas;
    }

    /// <summary>Lista las licitaciones con paginacion, filtrado y ordenamiento.</summary>
    /// <param name="parametros">Parametros de consulta.</param>
    /// <param name="estado">Filtro opcional por estado.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Pagina de licitaciones.</returns>
    /// <response code="200">Devuelve la pagina solicitada.</response>
    [HttpGet(Name = "ListarLicitaciones")]
    [ProducesResponseType(typeof(PaginaResultado<LicitacionResumenDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginaResultado<LicitacionResumenDto>>> Listar(
        [FromQuery] ParametrosConsultaApi parametros,
        [FromQuery] EstadoLicitacion? estado,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(parametros);

        var resultado = await _servicio.ListarAsync(
            parametros.AParametrosConsulta(),
            estado,
            cancelacion);

        return Ok(resultado);
    }

    /// <summary>Consulta el detalle de una licitacion.</summary>
    /// <param name="id">Identificador de la licitacion.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Detalle de la licitacion, con evaluacion de ofertas y nivel de aprobacion.</returns>
    /// <response code="200">Devuelve la licitacion solicitada.</response>
    /// <response code="404">La licitacion no existe.</response>
    [HttpGet("{id:guid}", Name = "ObtenerLicitacion")]
    [ProducesResponseType(typeof(LicitacionDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LicitacionDetalleDto>> Obtener(
        Guid id,
        CancellationToken cancelacion)
    {
        return Ok(await _servicio.ObtenerDetalleAsync(id, cancelacion));
    }

    /// <summary>Crea una licitacion en estado Borrador.</summary>
    /// <param name="peticion">Datos de la licitacion.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La licitacion creada.</returns>
    /// <response code="201">La licitacion se creo correctamente.</response>
    /// <response code="400">Los datos de entrada no superaron la validacion.</response>
    /// <response code="409">Ya existe una licitacion con ese codigo.</response>
    /// <response code="422">Los datos incumplen una regla de negocio.</response>
    [HttpPost(Name = "CrearLicitacion")]
    [ProducesResponseType(typeof(LicitacionDetalleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LicitacionDetalleDto>> Crear(
        [FromBody] CrearLicitacionRequest peticion,
        CancellationToken cancelacion)
    {
        LicitacionDetalleDto creada = await _servicio.CrearAsync(peticion, cancelacion);

        return CreatedAtRoute("ObtenerLicitacion", new { id = creada.Id, version = "1.0" }, creada);
    }

    /// <summary>Modifica una licitacion existente.</summary>
    /// <param name="id">Identificador de la licitacion.</param>
    /// <param name="peticion">Nuevos datos.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La licitacion actualizada.</returns>
    /// <response code="200">La licitacion se actualizo correctamente.</response>
    /// <response code="400">Los datos de entrada no superaron la validacion.</response>
    /// <response code="404">La licitacion no existe.</response>
    /// <response code="409">El codigo ya esta en uso o hubo conflicto de concurrencia.</response>
    /// <response code="422">Los datos incumplen una regla de negocio.</response>
    [HttpPut("{id:guid}", Name = "ActualizarLicitacion")]
    [ProducesResponseType(typeof(LicitacionDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LicitacionDetalleDto>> Actualizar(
        Guid id,
        [FromBody] ActualizarLicitacionRequest peticion,
        CancellationToken cancelacion)
    {
        return Ok(await _servicio.ActualizarAsync(id, peticion, cancelacion));
    }

    /// <summary>Aplica una transicion de estado a la licitacion.</summary>
    /// <param name="id">Identificador de la licitacion.</param>
    /// <param name="peticion">Estado destino solicitado.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La licitacion tras la transicion.</returns>
    /// <response code="200">La transicion se aplico correctamente.</response>
    /// <response code="404">La licitacion no existe.</response>
    /// <response code="409">La transicion no esta permitida por el ciclo de vida.</response>
    /// <response code="422">Los datos de la licitacion no permiten publicarla.</response>
    [HttpPatch("{id:guid}/estado", Name = "CambiarEstadoLicitacion")]
    [ProducesResponseType(typeof(LicitacionDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LicitacionDetalleDto>> CambiarEstado(
        Guid id,
        [FromBody] CambiarEstadoRequest peticion,
        CancellationToken cancelacion)
    {
        return Ok(await _servicio.CambiarEstadoAsync(id, peticion, cancelacion));
    }

    /// <summary>Elimina una licitacion.</summary>
    /// <param name="id">Identificador de la licitacion.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Sin contenido.</returns>
    /// <response code="204">La licitacion se elimino. Si tenia ofertas, el borrado fue logico.</response>
    /// <response code="404">La licitacion no existe.</response>
    [HttpDelete("{id:guid}", Name = "EliminarLicitacion")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        bool borradoLogico = await _servicio.EliminarAsync(id, cancelacion);

        // La cabecera informa al cliente que la fila sigue existiendo con marca de borrada,
        // sin cambiar el codigo de estado ni obligarlo a interpretarla.
        Response.Headers.Append("X-Tipo-Borrado", borradoLogico ? "logico" : "fisico");

        return NoContent();
    }

    /// <summary>Lista las ofertas de una licitacion.</summary>
    /// <param name="id">Identificador de la licitacion.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Ofertas ordenadas por monto ascendente.</returns>
    /// <response code="200">Devuelve las ofertas de la licitacion.</response>
    [HttpGet("{id:guid}/ofertas", Name = "ListarOfertasDeLicitacion")]
    [ProducesResponseType(typeof(IReadOnlyList<OfertaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OfertaDto>>> ListarOfertas(
        Guid id,
        CancellationToken cancelacion)
    {
        return Ok(await _ofertas.ListarPorLicitacionAsync(id, cancelacion));
    }

    /// <summary>Registra una oferta para la licitacion indicada en la ruta.</summary>
    /// <param name="id">Identificador de la licitacion.</param>
    /// <param name="peticion">Proveedor y monto ofertado.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La oferta registrada.</returns>
    /// <response code="201">La oferta se registro correctamente.</response>
    /// <response code="400">Los datos de entrada no superaron la validacion.</response>
    /// <response code="404">La licitacion o el proveedor no existen.</response>
    /// <response code="409">El proveedor ya oferto en esta licitacion.</response>
    /// <response code="422">La oferta supera el presupuesto, o la licitacion no admite ofertas.</response>
    [HttpPost("{id:guid}/ofertas", Name = "RegistrarOfertaEnLicitacion")]
    [ProducesResponseType(typeof(OfertaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OfertaDto>> RegistrarOferta(
        Guid id,
        [FromBody] RegistrarOfertaEnLicitacionRequest peticion,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(peticion);

        var solicitud = new CrearOfertaRequest(id, peticion.ProveedorId, peticion.MontoOfertadoCrc);
        OfertaDto creada = await _ofertas.RegistrarAsync(solicitud, cancelacion);

        return CreatedAtRoute("ObtenerOferta", new { id = creada.Id, version = "1.0" }, creada);
    }

    /// <summary>Consulta la mejor oferta de una licitacion y su clasificacion.</summary>
    /// <param name="id">Identificador de la licitacion.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Mejor oferta, porcentaje de ahorro, clasificacion y aprobador.</returns>
    /// <response code="200">Devuelve la evaluacion. Si no hay ofertas, la clasificacion lo indica.</response>
    /// <response code="404">La licitacion no existe.</response>
    [HttpGet("{id:guid}/mejor-oferta", Name = "ObtenerMejorOferta")]
    [ProducesResponseType(typeof(EvaluacionLicitacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EvaluacionLicitacionDto>> ObtenerMejorOferta(
        Guid id,
        CancellationToken cancelacion)
    {
        return Ok(await _servicio.ObtenerMejorOfertaAsync(id, cancelacion));
    }
}
