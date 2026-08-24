namespace Licitaciones.Domain.Excepciones;

/// <summary>
/// Se lanza cuando una operacion incumple una regla de negocio del sistema.
/// </summary>
/// <remarks>Se traduce a HTTP 422 Unprocessable Entity en la capa de API.</remarks>
public sealed class ReglaNegocioVioladaException : DominioException
{
    /// <summary>Inicializa la excepcion.</summary>
    /// <param name="mensaje">Texto seguro para mostrar al usuario final.</param>
    /// <param name="codigoError">Codigo estable de <see cref="Constantes.CodigosError"/>.</param>
    public ReglaNegocioVioladaException(string mensaje, string codigoError)
        : base(mensaje, codigoError)
    {
    }
}
