using Licitaciones.Application.Servicios;
using Licitaciones.Web.Modelos;
using Licitaciones.Web.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.Controladores;

/// <summary>Landing page y acciones globales de la interfaz.</summary>
public sealed class InicioController : ControladorBase
{
    private readonly PreferenciasUsuario _preferencias;
    private readonly ITipoCambioServicio _tiposCambio;

    /// <summary>Inicializa el controlador.</summary>
    /// <param name="preferencias">Preferencias de presentacion del usuario.</param>
    /// <param name="tiposCambio">Servicio de aplicacion de tipos de cambio.</param>
    public InicioController(PreferenciasUsuario preferencias, ITipoCambioServicio tiposCambio)
    {
        _preferencias = preferencias;
        _tiposCambio = tiposCambio;
    }

    /// <summary>Muestra la landing page con la explicacion del sistema.</summary>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La vista de inicio.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancelacion)
    {
        var modelo = new InicioViewModel
        {
            TipoCambioActivo = await _tiposCambio.ObtenerActivoAsync(cancelacion)
        };

        return View(modelo);
    }

    /// <summary>Alterna entre el modo claro y el modo oscuro.</summary>
    /// <param name="retorno">Ruta a la que regresar despues de cambiar el tema.</param>
    /// <returns>Redireccion a la pagina de origen.</returns>
    /// <remarks>
    /// Es una accion POST y no un enlace porque cambia estado del lado del servidor. Se implementa
    /// como formulario para que funcione tambien sin JavaScript.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AlternarTema(string? retorno)
    {
        _preferencias.AlternarTema();

        return RedirigirSeguro(retorno);
    }

    /// <summary>Alterna la moneda de visualizacion entre colones y dolares.</summary>
    /// <param name="retorno">Ruta a la que regresar despues de cambiar la moneda.</param>
    /// <returns>Redireccion a la pagina de origen.</returns>
    /// <remarks>
    /// Solo cambia la presentacion: los valores almacenados siguen expresados en colones y la
    /// conversion se recalcula en cada respuesta (seccion 8.8).
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AlternarMoneda(string? retorno)
    {
        _preferencias.AlternarMoneda();

        return RedirigirSeguro(retorno);
    }

    /// <summary>Muestra la pagina de error controlada.</summary>
    /// <returns>La vista de error.</returns>
    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();

    /// <summary>
    /// Redirige a la ruta indicada solo si es local.
    /// </summary>
    /// <param name="retorno">Ruta candidata.</param>
    /// <returns>Redireccion segura.</returns>
    /// <remarks>
    /// Aceptar una URL arbitraria del parametro permitiria una redireccion abierta hacia un sitio
    /// externo. Se comprueba que sea local y, si no lo es, se vuelve al inicio.
    /// </remarks>
    private IActionResult RedirigirSeguro(string? retorno)
        => !string.IsNullOrWhiteSpace(retorno) && Url.IsLocalUrl(retorno)
            ? Redirect(retorno)
            : RedirectToAction(nameof(Index));
}
