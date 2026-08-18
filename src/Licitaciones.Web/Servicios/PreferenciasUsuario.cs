namespace Licitaciones.Web.Servicios;

/// <summary>
/// Lee y escribe las preferencias de presentacion del usuario en cookies.
/// </summary>
/// <remarks>
/// Se usan cookies y no almacenamiento del navegador por dos razones. La primera es que el
/// servidor necesita conocer el tema antes de renderizar: si el tema se aplicara desde
/// JavaScript despues de cargar, la pagina parpadearia en claro antes de pasar a oscuro. La
/// segunda es que las pruebas funcionales pueden fijar la preferencia sin ejecutar guiones.
/// </remarks>
public sealed class PreferenciasUsuario
{
    /// <summary>Nombre de la cookie que guarda el tema visual.</summary>
    public const string CookieTema = "licitaciones.tema";

    /// <summary>Nombre de la cookie que guarda la moneda de visualizacion.</summary>
    public const string CookieMoneda = "licitaciones.moneda";

    /// <summary>Valor de tema para el modo claro.</summary>
    public const string TemaClaro = "claro";

    /// <summary>Valor de tema para el modo oscuro.</summary>
    public const string TemaOscuro = "oscuro";

    /// <summary>Valor de moneda para colones costarricenses.</summary>
    public const string MonedaCrc = "CRC";

    /// <summary>Valor de moneda para dolares estadounidenses.</summary>
    public const string MonedaUsd = "USD";

    private const int DiasVigencia = 365;

    private readonly IHttpContextAccessor _acceso;

    /// <summary>Inicializa el servicio.</summary>
    /// <param name="acceso">Acceso al contexto HTTP actual.</param>
    public PreferenciasUsuario(IHttpContextAccessor acceso)
    {
        _acceso = acceso;
    }

    /// <summary>Tema visual vigente. El modo claro es el valor por defecto.</summary>
    public string Tema
    {
        get
        {
            string? valor = _acceso.HttpContext?.Request.Cookies[CookieTema];

            return valor == TemaOscuro ? TemaOscuro : TemaClaro;
        }
    }

    /// <summary>Moneda de visualizacion vigente. El colon es el valor por defecto.</summary>
    /// <remarks>
    /// Cambiar esta preferencia solo altera lo que se muestra. Los montos almacenados siguen
    /// siendo colones en todos los casos (seccion 8.8).
    /// </remarks>
    public string Moneda
    {
        get
        {
            string? valor = _acceso.HttpContext?.Request.Cookies[CookieMoneda];

            return valor == MonedaUsd ? MonedaUsd : MonedaCrc;
        }
    }

    /// <summary>Indica si el tema vigente es el oscuro.</summary>
    public bool EsTemaOscuro => Tema == TemaOscuro;

    /// <summary>Indica si los montos deben mostrarse en dolares.</summary>
    public bool MostrarEnDolares => Moneda == MonedaUsd;

    /// <summary>Alterna el tema visual y lo persiste en la cookie.</summary>
    /// <returns>El tema que quedo vigente.</returns>
    public string AlternarTema()
    {
        string nuevo = EsTemaOscuro ? TemaClaro : TemaOscuro;
        Guardar(CookieTema, nuevo);

        return nuevo;
    }

    /// <summary>Alterna la moneda de visualizacion y la persiste en la cookie.</summary>
    /// <returns>La moneda que quedo vigente.</returns>
    public string AlternarMoneda()
    {
        string nueva = MostrarEnDolares ? MonedaCrc : MonedaUsd;
        Guardar(CookieMoneda, nueva);

        return nueva;
    }

    private void Guardar(string nombre, string valor)
    {
        _acceso.HttpContext?.Response.Cookies.Append(nombre, valor, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(DiasVigencia),
            HttpOnly = false,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
    }
}
