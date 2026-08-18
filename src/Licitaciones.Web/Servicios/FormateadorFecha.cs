using System.Globalization;

namespace Licitaciones.Web.Servicios;

/// <summary>
/// Convierte los instantes almacenados en UTC a la zona horaria de Costa Rica para mostrarlos.
/// </summary>
/// <remarks>
/// La seccion 8.2 exige que las comparaciones internas se hagan en UTC y que la presentacion
/// use <c>America/Costa_Rica</c>. Este es el unico punto donde ocurre esa conversion: ninguna
/// regla de negocio trabaja con la hora local.
/// </remarks>
public static class FormateadorFecha
{
    /// <summary>Identificador de la zona horaria de presentacion.</summary>
    public const string ZonaHorariaCostaRica = "America/Costa_Rica";

    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CR");

    private static readonly TimeZoneInfo Zona = ObtenerZona();

    /// <summary>Convierte un instante UTC a hora de Costa Rica.</summary>
    /// <param name="instanteUtc">Instante almacenado, en UTC.</param>
    /// <returns>El mismo instante expresado en la zona horaria local.</returns>
    public static DateTimeOffset AHoraLocal(DateTimeOffset instanteUtc)
        => TimeZoneInfo.ConvertTime(instanteUtc, Zona);

    /// <summary>Formatea un instante como fecha y hora local.</summary>
    /// <param name="instanteUtc">Instante almacenado, en UTC.</param>
    /// <returns>Texto con fecha y hora en formato costarricense.</returns>
    public static string FechaHora(DateTimeOffset instanteUtc)
        => AHoraLocal(instanteUtc).ToString("dd/MM/yyyy HH:mm", Cultura);

    /// <summary>Formatea un instante como fecha sin hora.</summary>
    /// <param name="instanteUtc">Instante almacenado, en UTC.</param>
    /// <returns>Texto con la fecha en formato costarricense.</returns>
    public static string Fecha(DateTimeOffset instanteUtc)
        => AHoraLocal(instanteUtc).ToString("dd/MM/yyyy", Cultura);

    /// <summary>
    /// Formatea un instante para el atributo <c>value</c> de un control
    /// <c>input type="datetime-local"</c>.
    /// </summary>
    /// <param name="instanteUtc">Instante almacenado, en UTC.</param>
    /// <returns>Texto en el formato ISO que exige el control del navegador.</returns>
    public static string ParaControlCalendario(DateTimeOffset instanteUtc)
        => AHoraLocal(instanteUtc).ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);

    /// <summary>
    /// Interpreta el valor de un control de calendario como hora local y lo lleva a UTC.
    /// </summary>
    /// <param name="fechaLocal">Valor recibido del formulario, sin zona horaria.</param>
    /// <returns>El instante equivalente en UTC.</returns>
    /// <remarks>
    /// El control <c>datetime-local</c> del navegador envia la fecha sin desplazamiento. Si se
    /// interpretara como UTC, una licitacion que cierra a las 17:00 en Costa Rica quedaria
    /// registrada seis horas antes de lo que el usuario quiso.
    /// </remarks>
    public static DateTimeOffset DesdeControlCalendario(DateTime fechaLocal)
    {
        DateTime sinZona = DateTime.SpecifyKind(fechaLocal, DateTimeKind.Unspecified);
        TimeSpan desplazamiento = Zona.GetUtcOffset(sinZona);

        return new DateTimeOffset(sinZona, desplazamiento).ToUniversalTime();
    }

    /// <summary>Obtiene la zona horaria de presentacion tolerando las diferencias entre sistemas.</summary>
    /// <returns>La zona de Costa Rica, o UTC si el sistema no la reconoce.</returns>
    /// <remarks>
    /// Linux usa identificadores IANA y Windows usa los suyos propios. .NET convierte entre
    /// ambos desde la version 6, pero una imagen minima puede no traer la base de datos de
    /// zonas horarias; en ese caso se cae a UTC en lugar de impedir el arranque.
    /// </remarks>
    private static TimeZoneInfo ObtenerZona()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(ZonaHorariaCostaRica);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
