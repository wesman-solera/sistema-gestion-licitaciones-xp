namespace Licitaciones.Domain.Excepciones;

/// <summary>
/// Excepcion base de todas las violaciones de regla de negocio del dominio.
/// </summary>
/// <remarks>
/// Transporta un <see cref="CodigoError"/> estable para que las capas superiores puedan
/// traducirla a un codigo HTTP y a un ProblemDetails sin inspeccionar el texto del mensaje.
/// El mensaje esta escrito para ser mostrado directamente al usuario final: nunca debe
/// contener rutas internas, consultas SQL ni datos de conexion (seccion 10.2).
/// </remarks>
public abstract class DominioException : Exception
{
    /// <summary>Codigo estable definido en <see cref="Constantes.CodigosError"/>.</summary>
    public string CodigoError { get; }

    /// <summary>Inicializa la excepcion con un mensaje apto para el usuario y su codigo.</summary>
    /// <param name="mensaje">Texto seguro para mostrar al usuario final.</param>
    /// <param name="codigoError">Codigo estable de <see cref="Constantes.CodigosError"/>.</param>
    protected DominioException(string mensaje, string codigoError)
        : base(mensaje)
    {
        CodigoError = codigoError;
    }
}
