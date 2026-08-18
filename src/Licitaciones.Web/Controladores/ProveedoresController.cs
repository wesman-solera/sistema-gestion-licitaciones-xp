using Licitaciones.Api.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Application.Servicios;
using Licitaciones.Web.Modelos;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.Controladores;

/// <summary>Pantallas del modulo de proveedores.</summary>
public sealed class ProveedoresController : ControladorBase
{
    private readonly IProveedorServicio _servicio;

    /// <summary>Inicializa el controlador.</summary>
    /// <param name="servicio">Servicio de aplicacion de proveedores.</param>
    public ProveedoresController(IProveedorServicio servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Lista los proveedores con filtro, orden y paginacion.</summary>
    /// <param name="parametros">Parametros de consulta.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La vista del listado.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] ParametrosConsultaApi parametros,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(parametros);

        var pagina = await _servicio.ListarAsync(parametros.AParametrosConsulta(), cancelacion);

        return View(new ListadoViewModel<ProveedorDto>
        {
            Pagina = pagina,
            Parametros = parametros
        });
    }

    /// <summary>Muestra el formulario de creacion.</summary>
    /// <returns>La vista del formulario.</returns>
    [HttpGet]
    public IActionResult Crear() => View(new ProveedorFormulario());

    /// <summary>Procesa la creacion de un proveedor.</summary>
    /// <param name="formulario">Datos del formulario.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Redireccion al listado si tuvo exito, o el formulario con los errores.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(ProveedorFormulario formulario, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(formulario);

        if (!ModelState.IsValid)
        {
            return View(formulario);
        }

        bool exito = await EjecutarAsync(
            () => _servicio.CrearAsync(new CrearProveedorRequest(formulario.Nombre), cancelacion),
            nameof(ProveedorFormulario.Nombre));

        if (!exito)
        {
            return View(formulario);
        }

        AvisarExito($"El proveedor {formulario.Nombre} se registro correctamente.");

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Muestra el formulario de edicion.</summary>
    /// <param name="id">Identificador del proveedor.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La vista del formulario poblado.</returns>
    [HttpGet]
    public async Task<IActionResult> Editar(Guid id, CancellationToken cancelacion)
    {
        return View(ProveedorFormulario.Desde(await _servicio.ObtenerAsync(id, cancelacion)));
    }

    /// <summary>Procesa la edicion de un proveedor.</summary>
    /// <param name="id">Identificador del proveedor.</param>
    /// <param name="formulario">Datos del formulario.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Redireccion al listado si tuvo exito, o el formulario con los errores.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(
        Guid id,
        ProveedorFormulario formulario,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(formulario);

        formulario.Id = id;

        if (!ModelState.IsValid)
        {
            return View(formulario);
        }

        bool exito = await EjecutarAsync(
            () => _servicio.ActualizarAsync(
                id,
                new ActualizarProveedorRequest(formulario.Nombre),
                cancelacion),
            nameof(ProveedorFormulario.Nombre));

        if (!exito)
        {
            return View(formulario);
        }

        AvisarExito("El proveedor se actualizo correctamente.");

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Muestra la confirmacion de eliminacion.</summary>
    /// <param name="id">Identificador del proveedor.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La vista de confirmacion.</returns>
    [HttpGet]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        return View(await _servicio.ObtenerAsync(id, cancelacion));
    }

    /// <summary>Ejecuta la eliminacion confirmada.</summary>
    /// <param name="id">Identificador del proveedor.</param>
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
            AvisarError("No fue posible eliminar el proveedor.");

            return RedirectToAction(nameof(Index));
        }

        AvisarExito(borradoLogico
            ? "El proveedor se marco como eliminado. Sus ofertas se conservan como evidencia."
            : "El proveedor se elimino de forma definitiva.");

        return RedirectToAction(nameof(Index));
    }
}
