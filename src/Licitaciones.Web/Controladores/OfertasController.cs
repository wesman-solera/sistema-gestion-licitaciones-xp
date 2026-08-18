using Licitaciones.Api.Comun;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Application.Servicios;
using Licitaciones.Domain.Enums;
using Licitaciones.Web.Modelos;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.Controladores;

/// <summary>Pantallas del modulo de ofertas.</summary>
public sealed class OfertasController : ControladorBase
{
    private readonly IOfertaServicio _servicio;
    private readonly ILicitacionServicio _licitaciones;
    private readonly IProveedorServicio _proveedores;

    /// <summary>Inicializa el controlador.</summary>
    /// <param name="servicio">Servicio de aplicacion de ofertas.</param>
    /// <param name="licitaciones">Servicio de aplicacion de licitaciones.</param>
    /// <param name="proveedores">Servicio de aplicacion de proveedores.</param>
    public OfertasController(
        IOfertaServicio servicio,
        ILicitacionServicio licitaciones,
        IProveedorServicio proveedores)
    {
        _servicio = servicio;
        _licitaciones = licitaciones;
        _proveedores = proveedores;
    }

    /// <summary>Lista las ofertas con filtros por licitacion y proveedor.</summary>
    /// <param name="parametros">Parametros de consulta.</param>
    /// <param name="licitacionId">Filtro opcional por licitacion.</param>
    /// <param name="proveedorId">Filtro opcional por proveedor.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La vista del listado.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] ParametrosConsultaApi parametros,
        [FromQuery] Guid? licitacionId,
        [FromQuery] Guid? proveedorId,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(parametros);

        var pagina = await _servicio.ListarAsync(
            parametros.AParametrosConsulta(),
            licitacionId,
            proveedorId,
            cancelacion);

        ViewData["Licitaciones"] = await ObtenerLicitacionesAsync(soloAbiertas: false, cancelacion);
        ViewData["Proveedores"] = await _proveedores.ListarActivosAsync(cancelacion);
        ViewData["LicitacionFiltrada"] = licitacionId;
        ViewData["ProveedorFiltrado"] = proveedorId;

        return View(new ListadoViewModel<OfertaDto>
        {
            Pagina = pagina,
            Parametros = parametros,
            FiltrosExtra = new Dictionary<string, string>
            {
                ["licitacionId"] = licitacionId?.ToString() ?? string.Empty,
                ["proveedorId"] = proveedorId?.ToString() ?? string.Empty
            }
        });
    }

    /// <summary>Muestra el formulario de registro de una oferta.</summary>
    /// <param name="licitacionId">Licitacion preseleccionada, si viene desde su detalle.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La vista del formulario.</returns>
    [HttpGet]
    public async Task<IActionResult> Crear(Guid? licitacionId, CancellationToken cancelacion)
    {
        var formulario = new OfertaFormulario
        {
            LicitacionId = licitacionId ?? Guid.Empty
        };

        await PoblarListasAsync(formulario, cancelacion);

        return View(formulario);
    }

    /// <summary>Procesa el registro de una oferta.</summary>
    /// <param name="formulario">Datos del formulario.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Redireccion al detalle de la licitacion, o el formulario con los errores.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(OfertaFormulario formulario, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(formulario);

        if (!ModelState.IsValid)
        {
            await PoblarListasAsync(formulario, cancelacion);

            return View(formulario);
        }

        var peticion = new CrearOfertaRequest(
            formulario.LicitacionId,
            formulario.ProveedorId,
            formulario.MontoOfertadoCrc);

        bool exito = await EjecutarAsync(
            () => _servicio.RegistrarAsync(peticion, cancelacion),
            nameof(OfertaFormulario.MontoOfertadoCrc));

        if (!exito)
        {
            await PoblarListasAsync(formulario, cancelacion);

            return View(formulario);
        }

        AvisarExito("La oferta se registro correctamente.");

        return RedirectToAction(
            nameof(LicitacionesController.Detalle),
            "Licitaciones",
            new { id = formulario.LicitacionId });
    }

    /// <summary>Muestra el formulario de edicion del monto de una oferta.</summary>
    /// <param name="id">Identificador de la oferta.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La vista del formulario poblado.</returns>
    [HttpGet]
    public async Task<IActionResult> Editar(Guid id, CancellationToken cancelacion)
    {
        OfertaDto oferta = await _servicio.ObtenerAsync(id, cancelacion);

        var formulario = new OfertaFormulario
        {
            Id = oferta.Id,
            LicitacionId = oferta.LicitacionId,
            ProveedorId = oferta.ProveedorId,
            MontoOfertadoCrc = oferta.Monto.Crc
        };

        await PoblarListasAsync(formulario, cancelacion);

        return View(formulario);
    }

    /// <summary>Procesa la edicion del monto de una oferta.</summary>
    /// <param name="id">Identificador de la oferta.</param>
    /// <param name="formulario">Datos del formulario.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Redireccion al detalle de la licitacion, o el formulario con los errores.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(
        Guid id,
        OfertaFormulario formulario,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(formulario);

        formulario.Id = id;

        if (!ModelState.IsValid)
        {
            await PoblarListasAsync(formulario, cancelacion);

            return View(formulario);
        }

        bool exito = await EjecutarAsync(
            () => _servicio.ActualizarAsync(
                id,
                new ActualizarOfertaRequest(formulario.MontoOfertadoCrc),
                cancelacion),
            nameof(OfertaFormulario.MontoOfertadoCrc));

        if (!exito)
        {
            await PoblarListasAsync(formulario, cancelacion);

            return View(formulario);
        }

        AvisarExito("La oferta se actualizo correctamente.");

        return RedirectToAction(
            nameof(LicitacionesController.Detalle),
            "Licitaciones",
            new { id = formulario.LicitacionId });
    }

    /// <summary>Muestra la confirmacion de eliminacion.</summary>
    /// <param name="id">Identificador de la oferta.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La vista de confirmacion.</returns>
    [HttpGet]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        return View(await _servicio.ObtenerAsync(id, cancelacion));
    }

    /// <summary>Ejecuta la eliminacion confirmada.</summary>
    /// <param name="id">Identificador de la oferta.</param>
    /// <param name="licitacionId">Licitacion a la que regresar.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Redireccion al detalle de la licitacion.</returns>
    [HttpPost]
    [ActionName(nameof(Eliminar))]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarEliminacion(
        Guid id,
        Guid licitacionId,
        CancellationToken cancelacion)
    {
        bool exito = await EjecutarAsync(() => _servicio.EliminarAsync(id, cancelacion));

        if (exito)
        {
            AvisarExito("La oferta se elimino correctamente.");
        }
        else
        {
            AvisarError(ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "No fue posible eliminar la oferta.");
        }

        return RedirectToAction(
            nameof(LicitacionesController.Detalle),
            "Licitaciones",
            new { id = licitacionId });
    }

    /// <summary>
    /// Carga las listas de seleccion del formulario.
    /// </summary>
    /// <param name="formulario">Formulario a poblar.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Tarea que se completa cuando las listas estan cargadas.</returns>
    /// <remarks>
    /// Al registrar una oferta solo se ofrecen licitaciones que efectivamente pueden recibirla.
    /// Mostrar las cerradas solo produciria un rechazo despues de que el usuario completo el
    /// formulario. Al editar se conserva la licitacion original aunque ya no admita ofertas
    /// nuevas, para que el desplegable no aparezca vacio.
    /// </remarks>
    private async Task PoblarListasAsync(OfertaFormulario formulario, CancellationToken cancelacion)
    {
        var licitaciones = await ObtenerLicitacionesAsync(soloAbiertas: !formulario.EsEdicion, cancelacion);

        if (formulario.EsEdicion && licitaciones.All(l => l.Id != formulario.LicitacionId))
        {
            var todas = await ObtenerLicitacionesAsync(soloAbiertas: false, cancelacion);
            licitaciones = [.. todas.Where(l => l.Id == formulario.LicitacionId), .. licitaciones];
        }

        formulario.LicitacionesDisponibles = licitaciones;
        formulario.ProveedoresDisponibles = await _proveedores.ListarActivosAsync(cancelacion);
        formulario.PresupuestoReferencia = licitaciones
            .FirstOrDefault(l => l.Id == formulario.LicitacionId)?
            .PresupuestoEstimado.Crc;
    }

    private async Task<IReadOnlyList<LicitacionResumenDto>> ObtenerLicitacionesAsync(
        bool soloAbiertas,
        CancellationToken cancelacion)
    {
        var parametros = new ParametrosConsulta
        {
            Pagina = 1,
            TamanoPagina = ParametrosConsulta.TamanoPaginaMaximo,
            OrdenarPor = "codigo"
        };

        var pagina = await _licitaciones.ListarAsync(
            parametros,
            soloAbiertas ? EstadoLicitacion.Publicada : null,
            cancelacion);

        return soloAbiertas
            ? [.. pagina.Elementos.Where(l => l.EstadoEfectivo == EstadoLicitacion.Publicada)]
            : pagina.Elementos;
    }
}
