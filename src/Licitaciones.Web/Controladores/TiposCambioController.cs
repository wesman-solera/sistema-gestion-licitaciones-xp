using Licitaciones.Api.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Application.Servicios;
using Licitaciones.Web.Modelos;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.Controladores;

/// <summary>Pantallas del modulo de tipos de cambio.</summary>
public sealed class TiposCambioController : ControladorBase
{
    private readonly ITipoCambioServicio _servicio;

    /// <summary>Inicializa el controlador.</summary>
    /// <param name="servicio">Servicio de aplicacion de tipos de cambio.</param>
    public TiposCambioController(ITipoCambioServicio servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Lista los tipos de cambio registrados.</summary>
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

        return View(new ListadoViewModel<TipoCambioDto>
        {
            Pagina = pagina,
            Parametros = parametros
        });
    }

    /// <summary>Muestra el formulario de creacion.</summary>
    /// <returns>La vista del formulario.</returns>
    [HttpGet]
    public IActionResult Crear() => View(new TipoCambioFormulario { Activo = true });

    /// <summary>Procesa la creacion de un tipo de cambio.</summary>
    /// <param name="formulario">Datos del formulario.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Redireccion al listado si tuvo exito, o el formulario con los errores.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        TipoCambioFormulario formulario,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(formulario);

        if (!ModelState.IsValid)
        {
            return View(formulario);
        }

        var peticion = new CrearTipoCambioRequest(
            formulario.CrcPorUsd,
            formulario.FechaVigenciaUtc(),
            formulario.Activo);

        bool exito = await EjecutarAsync(
            () => _servicio.CrearAsync(peticion, cancelacion),
            nameof(TipoCambioFormulario.CrcPorUsd));

        if (!exito)
        {
            return View(formulario);
        }

        AvisarExito(formulario.Activo
            ? "El tipo de cambio se registro y quedo activo."
            : "El tipo de cambio se registro.");

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Muestra el formulario de edicion.</summary>
    /// <param name="id">Identificador del tipo de cambio.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La vista del formulario poblado.</returns>
    [HttpGet]
    public async Task<IActionResult> Editar(Guid id, CancellationToken cancelacion)
    {
        return View(TipoCambioFormulario.Desde(await _servicio.ObtenerAsync(id, cancelacion)));
    }

    /// <summary>Procesa la edicion de un tipo de cambio.</summary>
    /// <param name="id">Identificador del tipo de cambio.</param>
    /// <param name="formulario">Datos del formulario.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Redireccion al listado si tuvo exito, o el formulario con los errores.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(
        Guid id,
        TipoCambioFormulario formulario,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(formulario);

        formulario.Id = id;

        if (!ModelState.IsValid)
        {
            return View(formulario);
        }

        var peticion = new ActualizarTipoCambioRequest(
            formulario.CrcPorUsd,
            formulario.FechaVigenciaUtc());

        bool exito = await EjecutarAsync(
            () => _servicio.ActualizarAsync(id, peticion, cancelacion),
            nameof(TipoCambioFormulario.CrcPorUsd));

        if (!exito)
        {
            return View(formulario);
        }

        AvisarExito("El tipo de cambio se actualizo correctamente.");

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Marca un tipo de cambio como el activo.</summary>
    /// <param name="id">Identificador del tipo de cambio.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Redireccion al listado.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activar(Guid id, CancellationToken cancelacion)
    {
        bool exito = await EjecutarAsync(() => _servicio.ActivarAsync(id, cancelacion));

        if (exito)
        {
            AvisarExito("El tipo de cambio quedo activo. El anterior se desactivo automaticamente.");
        }
        else
        {
            AvisarError("No fue posible activar el tipo de cambio.");
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Muestra la confirmacion de eliminacion.</summary>
    /// <param name="id">Identificador del tipo de cambio.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La vista de confirmacion.</returns>
    [HttpGet]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        return View(await _servicio.ObtenerAsync(id, cancelacion));
    }

    /// <summary>Ejecuta la eliminacion confirmada.</summary>
    /// <param name="id">Identificador del tipo de cambio.</param>
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
            AvisarExito("El tipo de cambio se elimino correctamente.");
        }
        else
        {
            AvisarError(ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "No fue posible eliminar el tipo de cambio.");
        }

        return RedirectToAction(nameof(Index));
    }
}
