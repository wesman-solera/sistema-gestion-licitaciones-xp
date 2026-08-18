using Licitaciones.Api.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Application.Servicios;
using Licitaciones.Domain.Enums;
using Licitaciones.Web.Modelos;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.Controladores;

/// <summary>Pantallas del modulo de licitaciones.</summary>
/// <remarks>
/// El controlador MVC consume exactamente los mismos servicios de aplicacion que la API REST.
/// Esa reutilizacion es intencional: garantiza que una regla de negocio no pueda comportarse de
/// una forma en la pantalla y de otra por la API.
/// </remarks>
public sealed class LicitacionesController : ControladorBase
{
    private readonly ILicitacionServicio _servicio;

    /// <summary>Inicializa el controlador.</summary>
    /// <param name="servicio">Servicio de aplicacion de licitaciones.</param>
    public LicitacionesController(ILicitacionServicio servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Lista las licitaciones con filtro, orden y paginacion.</summary>
    /// <param name="parametros">Parametros de consulta.</param>
    /// <param name="estado">Filtro opcional por estado.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La vista del listado.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] ParametrosConsultaApi parametros,
        [FromQuery] EstadoLicitacion? estado,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(parametros);

        var pagina = await _servicio.ListarAsync(
            parametros.AParametrosConsulta(),
            estado,
            cancelacion);

        var modelo = new ListadoViewModel<LicitacionResumenDto>
        {
            Pagina = pagina,
            Parametros = parametros,
            FiltrosExtra = new Dictionary<string, string> { ["estado"] = estado?.ToString() ?? string.Empty }
        };

        ViewData["EstadoFiltrado"] = estado;

        return View(modelo);
    }

    /// <summary>Muestra el detalle de una licitacion con su evaluacion de ofertas.</summary>
    /// <param name="id">Identificador de la licitacion.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La vista de detalle.</returns>
    [HttpGet]
    public async Task<IActionResult> Detalle(Guid id, CancellationToken cancelacion)
    {
        return View(await _servicio.ObtenerDetalleAsync(id, cancelacion));
    }

    /// <summary>Muestra el formulario de creacion.</summary>
    /// <returns>La vista del formulario.</returns>
    [HttpGet]
    public IActionResult Crear()
    {
        // Se propone una fecha de cierre a una semana vista: es un valor razonable y evita que
        // el usuario tenga que escribir la fecha completa desde cero.
        return View(new LicitacionFormulario
        {
            FechaCierre = DateTime.Now.Date.AddDays(7).AddHours(17)
        });
    }

    /// <summary>Procesa la creacion de una licitacion.</summary>
    /// <param name="formulario">Datos del formulario.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Redireccion al detalle si tuvo exito, o el formulario con los errores.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(LicitacionFormulario formulario, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(formulario);

        if (!ModelState.IsValid)
        {
            return View(formulario);
        }

        LicitacionDetalleDto? creada = null;

        bool exito = await EjecutarAsync(
            async () => creada = await _servicio.CrearAsync(formulario.ACrearRequest(), cancelacion),
            nameof(LicitacionFormulario.Codigo));

        if (!exito || creada is null)
        {
            return View(formulario);
        }

        AvisarExito($"La licitacion {creada.Codigo} se creo en estado Borrador.");

        return RedirectToAction(nameof(Detalle), new { id = creada.Id });
    }

    /// <summary>Muestra el formulario de edicion.</summary>
    /// <param name="id">Identificador de la licitacion.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La vista del formulario poblado.</returns>
    [HttpGet]
    public async Task<IActionResult> Editar(Guid id, CancellationToken cancelacion)
    {
        LicitacionDetalleDto detalle = await _servicio.ObtenerDetalleAsync(id, cancelacion);

        if (detalle.Estado == EstadoLicitacion.Cerrada)
        {
            AvisarAdvertencia("Una licitacion cerrada no puede editarse.");

            return RedirectToAction(nameof(Detalle), new { id });
        }

        return View(LicitacionFormulario.Desde(detalle));
    }

    /// <summary>Procesa la edicion de una licitacion.</summary>
    /// <param name="id">Identificador de la licitacion.</param>
    /// <param name="formulario">Datos del formulario.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Redireccion al detalle si tuvo exito, o el formulario con los errores.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(
        Guid id,
        LicitacionFormulario formulario,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(formulario);

        formulario.Id = id;

        if (!ModelState.IsValid)
        {
            return View(formulario);
        }

        bool exito = await EjecutarAsync(
            () => _servicio.ActualizarAsync(id, formulario.AActualizarRequest(), cancelacion),
            nameof(LicitacionFormulario.PresupuestoEstimadoCrc));

        if (!exito)
        {
            return View(formulario);
        }

        AvisarExito("La licitacion se actualizo correctamente.");

        return RedirectToAction(nameof(Detalle), new { id });
    }

    /// <summary>Aplica una transicion de estado.</summary>
    /// <param name="id">Identificador de la licitacion.</param>
    /// <param name="estado">Estado destino.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Redireccion al detalle.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(
        Guid id,
        EstadoLicitacion estado,
        CancellationToken cancelacion)
    {
        bool exito = await EjecutarAsync(
            () => _servicio.CambiarEstadoAsync(id, new CambiarEstadoRequest(estado), cancelacion));

        if (exito)
        {
            AvisarExito($"La licitacion paso a estado {estado}.");
        }
        else
        {
            // El detalle es una vista de solo lectura: el error de transicion se muestra como
            // mensaje de pagina en lugar de como error de campo.
            AvisarError(ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "No fue posible cambiar el estado de la licitacion.");
        }

        return RedirectToAction(nameof(Detalle), new { id });
    }

    /// <summary>Muestra la confirmacion de eliminacion.</summary>
    /// <param name="id">Identificador de la licitacion.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La vista de confirmacion.</returns>
    /// <remarks>La seccion 8.9 exige solicitar confirmacion antes de cualquier eliminacion.</remarks>
    [HttpGet]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        return View(await _servicio.ObtenerDetalleAsync(id, cancelacion));
    }

    /// <summary>Ejecuta la eliminacion confirmada.</summary>
    /// <param name="id">Identificador de la licitacion.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Redireccion al listado.</returns>
    [HttpPost]
    [ActionName(nameof(Eliminar))]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarEliminacion(Guid id, CancellationToken cancelacion)
    {
        bool borradoLogico = false;

        bool exito = await EjecutarAsync(
            async () => borradoLogico = await _servicio.EliminarAsync(id, cancelacion));

        if (!exito)
        {
            AvisarError("No fue posible eliminar la licitacion.");

            return RedirectToAction(nameof(Detalle), new { id });
        }

        AvisarExito(borradoLogico
            ? "La licitacion se marco como eliminada. Sus ofertas se conservan como evidencia."
            : "La licitacion se elimino de forma definitiva.");

        return RedirectToAction(nameof(Index));
    }
}
