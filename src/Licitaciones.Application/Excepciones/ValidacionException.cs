using FluentValidation.Results;

namespace Licitaciones.Application.Excepciones;

/// <summary>
/// Se lanza cuando los datos de entrada no superan la validacion de formato.
/// </summary>
/// <remarks>
/// Distingue el error de formato (falta un campo, el texto excede el largo) del error de regla
/// de negocio (la oferta supera el presupuesto). El primero se traduce a HTTP 400 Bad Request
/// y el segundo a 422 Unprocessable Entity, como pide la seccion 10.2.
/// </remarks>
public sealed class ValidacionException : Exception
{
    /// <summary>Errores agrupados por nombre de campo.</summary>
    public IReadOnlyDictionary<string, string[]> Errores { get; }

    /// <summary>Inicializa la excepcion sin errores asociados.</summary>
    public ValidacionException()
        : base("Uno o mas campos no superaron la validacion.")
    {
        Errores = new Dictionary<string, string[]>();
    }

    /// <summary>Inicializa la excepcion a partir de los fallos devueltos por FluentValidation.</summary>
    /// <param name="fallos">Fallos de validacion.</param>
    public ValidacionException(IEnumerable<ValidationFailure> fallos)
        : this()
    {
        ArgumentNullException.ThrowIfNull(fallos);

        Errores = fallos
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.Distinct().ToArray());
    }

    /// <summary>Inicializa la excepcion con un unico error de campo.</summary>
    /// <param name="campo">Nombre del campo.</param>
    /// <param name="mensaje">Mensaje de error.</param>
    public ValidacionException(string campo, string mensaje)
        : this()
    {
        Errores = new Dictionary<string, string[]> { [campo] = [mensaje] };
    }
}
