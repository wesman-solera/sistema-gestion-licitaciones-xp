using Licitaciones.Api.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Application.Servicios;
using Licitaciones.Web.Modelos;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.Controladores;

/// <summary>Pantallas del modulo de niveles de aprobacion.</summary>
/// <remarks>
/// Esta pantalla es la que hace visible el requisito de la seccion 8.7: el aprobador se cambia
/// editando una fila, no tocando codigo.
/// </remarks>
public sealed class NivelesAprobacionController : ControladorBase
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
    /// <returns>La vista del listado.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] ParametrosConsultaApi parametros,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(parametros);

        var pagina = await _servicio.ListarAsync(parametros.AParametrosConsulta(), cancelacion);

        return View(new ListadoViewModel<NivelAprobacionDto>
        {
            Pagina = pagina,
            Parametros = parametros
        });
    }

    /// <summary>Muestra el formulario de creacion.</summary>
    /// <returns>La vista del formulario.</returns>
    [HttpGet]
    public IActionResult Crear() => View(new NivelAprobacionFormulario());

    /// <summary>Procesa la creacion de un rango.</summary>
    /// <param name="formulario">Datos del formulario.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Redireccion al listado si tuvo exito, o el formulario con los errores.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        NivelAprobacionFormulario formulario,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(formulario);

        if (!ModelState.IsValid)
        {
            return View(formulario);
        }

        var peticion = new CrearNivelAprobacionRequest(
            formulario.MontoMinimoCrc,
            formulario.MontoMaximoCrc,
            formulario.Aprobador);

        bool exito = await EjecutarAsync(
            () => _servicio.CrearAsync(peticion, cancelacion),
            nameof(NivelAprobacionFormulario.MontoMinimoCrc));

        if (!exito)
        {
            return View(formulario);
        }

        AvisarExito("El nivel de aprobacion se creo correctamente.");

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Muestra el formulario de edicion.</summary>
    /// <param name="id">Identificador del rango.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La vista del formulario poblado.</returns>
    [HttpGet]
    public async Task<IActionResult> Editar(Guid id, CancellationToken cancelacion)
    {
        return View(NivelAprobacionFormulario.Desde(await _servicio.ObtenerAsync(id, cancelacion)));
    }

    /// <summary>Procesa la edicion de un rango.</summary>
    /// <param name="id">Identificador del rango.</param>
    /// <param name="formulario">Datos del formulario.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Redireccion al listado si tuvo exito, o el formulario con los errores.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(
        Guid id,
        NivelAprobacionFormulario formulario,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(formulario);

        formulario.Id = id;

        if (!ModelState.IsValid)
        {
            return View(formulario);
        }

        var peticion = new ActualizarNivelAprobacionRequest(
            formulario.MontoMinimoCrc,
            formulario.MontoMaximoCrc,
            formulario.Aprobador);

        bool exito = await EjecutarAsync(
            () => _servicio.ActualizarAsync(id, peticion, cancelacion),
            nameof(NivelAprobacionFormulario.MontoMinimoCrc));

        if (!exito)
        {
            return View(formulario);
        }

        AvisarExito("El nivel de aprobacion se actualizo correctamente.");

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Muestra la confirmacion de eliminacion.</summary>
    /// <param name="id">Identificador del rango.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La vista de confirmacion.</returns>
    [HttpGet]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        return View(await _servicio.ObtenerAsync(id, cancelacion));
    }

    /// <summary>Ejecuta la eliminacion confirmada.</summary>
    /// <param name="id">Identificador del rango.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Redireccion al listado.</returns>
    [HttpPost]
    [ActionName(nameof(Eliminar))]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarEliminacion(Guid id, CancellationToken cancelacion)
    {
        bool exito = await EjecutarAsync(() => _servicio.EliminarAsync(id, cancelacion));

        if (exito)
        {
            AvisarExito("El nivel de aprobacion se elimino correctamente.");
        }
        else
        {
            AvisarError("No fue posible eliminar el nivel de aprobacion.");
        }

        return RedirectToAction(nameof(Index));
    }
}
