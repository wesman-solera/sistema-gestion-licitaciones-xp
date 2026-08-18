using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Licitaciones.Domain.Servicios;

/// <summary>
/// Reglas de normalizacion de texto usadas para garantizar unicidad (seccion 8.3 del enunciado).
/// </summary>
/// <remarks>
/// La forma normalizada se persiste en su propia columna con un indice unico. Esto permite que
/// PostgreSQL sea la ultima linea de defensa contra duplicados aun si dos peticiones concurrentes
/// superan la validacion de aplicacion. La normalizacion debe ser deterministica y estable:
/// cambiarla obliga a regenerar las columnas normalizadas mediante una migracion de datos.
/// </remarks>
public static partial class NormalizadorTexto
{
    /// <summary>
    /// Caracteres admitidos en el nombre de un proveedor: letras, numeros, espacios,
    /// punto, coma y parentesis (seccion 8.4).
    /// </summary>
    public const string PatronNombreProveedor = @"^[\p{L}\p{N} .,()]+$";

    [GeneratedRegex(PatronNombreProveedor, RegexOptions.CultureInvariant)]
    private static partial Regex RegexNombreProveedor();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex RegexEspaciosRepetidos();

    /// <summary>
    /// Normaliza un codigo de licitacion: elimina espacios laterales y lo lleva a mayusculas.
    /// </summary>
    /// <param name="codigo">Codigo tal como lo escribio el usuario.</param>
    /// <returns>Forma normalizada usada por el indice unico.</returns>
    /// <exception cref="ArgumentException">Si el codigo es nulo o solo contiene espacios.</exception>
    public static string NormalizarCodigo(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new ArgumentException("El codigo no puede estar vacio.", nameof(codigo));
        }

        return codigo.Trim().ToUpperInvariant();
    }

    /// <summary>
    /// Normaliza el nombre de un proveedor: recorta los extremos, reduce los espacios
    /// internos repetidos a uno solo, aplica normalizacion Unicode y lleva a mayusculas.
    /// </summary>
    /// <param name="nombre">Nombre tal como lo escribio el usuario.</param>
    /// <returns>Forma normalizada usada por el indice unico.</returns>
    /// <remarks>
    /// Se usa la forma Unicode C (composicion canonica) para que dos representaciones
    /// distintas del mismo caracter acentuado colapsen en la misma cadena. La comparacion
    /// en mayusculas se hace con la cultura invariante para que el resultado no dependa
    /// de la configuracion regional del servidor.
    /// </remarks>
    /// <exception cref="ArgumentException">Si el nombre es nulo o solo contiene espacios.</exception>
    public static string NormalizarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre no puede estar vacio.", nameof(nombre));
        }

        string sinExtremos = nombre.Trim();
        string espacioSimple = RegexEspaciosRepetidos().Replace(sinExtremos, " ");
        string unicodeCompuesto = espacioSimple.Normalize(NormalizationForm.FormC);

        return unicodeCompuesto.ToUpperInvariant();
    }

    /// <summary>
    /// Recorta los extremos y reduce los espacios repetidos sin alterar mayusculas ni acentos.
    /// </summary>
    /// <param name="valor">Texto original.</param>
    /// <returns>Texto listo para almacenarse como valor visible al usuario.</returns>
    /// <remarks>
    /// Se aplica al valor que se muestra en pantalla. La comparacion de unicidad usa
    /// <see cref="NormalizarNombre"/>, no este metodo.
    /// </remarks>
    public static string LimpiarParaMostrar(string valor)
    {
        ArgumentNullException.ThrowIfNull(valor);

        return RegexEspaciosRepetidos().Replace(valor.Trim(), " ").Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Indica si el nombre de proveedor solo contiene los caracteres permitidos.
    /// </summary>
    /// <param name="nombre">Nombre a evaluar, ya limpiado de espacios laterales.</param>
    /// <returns><c>true</c> cuando el nombre cumple el patron de la seccion 8.4.</returns>
    public static bool NombreProveedorTieneCaracteresValidos(string? nombre)
        => !string.IsNullOrWhiteSpace(nombre) && RegexNombreProveedor().IsMatch(nombre);

    /// <summary>
    /// Formatea un monto en colones usando la cultura <c>es-CR</c>.
    /// </summary>
    /// <param name="monto">Monto en colones costarricenses.</param>
    /// <returns>Cadena con formato cultural costarricense.</returns>
    /// <remarks>Requisito 9: el formato monetario de presentacion debe ser es-CR.</remarks>
    public static string FormatearColones(decimal monto)
        => monto.ToString("C2", CultureInfo.GetCultureInfo("es-CR"));
}
