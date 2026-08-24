namespace Licitaciones.Domain.Excepciones;

/// <summary>
/// Se lanza cuando un valor que debe ser unico ya existe en la base de datos.
/// </summary>
/// <remarks>
/// La unicidad se valida en interfaz, servidor e indice unico de PostgreSQL (seccion 8.3).
/// Esta excepcion cubre las dos primeras capas y tambien la traduccion del error de indice
/// que devuelve PostgreSQL. Se traduce a HTTP 409 Conflict.
/// </remarks>
public sealed class ConflictoUnicidadException : DominioException
{
    /// <summary>Nombre logico del campo en conflicto, util para resaltarlo en el formulario.</summary>
    public string Campo { get; }

    /// <summary>Inicializa la excepcion.</summary>
    /// <param name="campo">Nombre logico del campo duplicado.</param>
    /// <param name="mensaje">Texto seguro para mostrar al usuario final.</param>
    /// <param name="codigoError">Codigo estable de <see cref="Constantes.CodigosError"/>.</param>
    public ConflictoUnicidadException(string campo, string mensaje, string codigoError)
        : base(mensaje, codigoError)
    {
        Campo = campo;
    }
}
