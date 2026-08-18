using Licitaciones.Domain.Constantes;

namespace Licitaciones.Domain.Excepciones;

/// <summary>
/// Se lanza cuando el recurso solicitado no existe o fue eliminado logicamente.
/// </summary>
/// <remarks>Se traduce a HTTP 404 Not Found.</remarks>
public sealed class RecursoNoEncontradoException : ExcepcionDominio
{
    /// <summary>Nombre del tipo de recurso buscado, por ejemplo <c>Licitacion</c>.</summary>
    public string Recurso { get; }

    /// <summary>Inicializa la excepcion.</summary>
    /// <param name="recurso">Nombre legible del recurso.</param>
    /// <param name="identificador">Identificador buscado.</param>
    public RecursoNoEncontradoException(string recurso, object identificador)
        : base($"No se encontro {recurso} con identificador {identificador}.",
               CodigosError.RecursoNoEncontrado)
    {
        Recurso = recurso;
    }
}
