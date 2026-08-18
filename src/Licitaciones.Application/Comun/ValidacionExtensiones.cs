using FluentValidation;
using Licitaciones.Application.Excepciones;

namespace Licitaciones.Application.Comun;

/// <summary>
/// Utilidades para ejecutar un validador y traducir su resultado a una excepcion del sistema.
/// </summary>
/// <remarks>
/// Centraliza el patron para que ningun servicio tenga que recordar comprobar
/// <c>resultado.IsValid</c> y construir la excepcion a mano.
/// </remarks>
public static class ValidacionExtensiones
{
    /// <summary>Ejecuta el validador y lanza <see cref="ValidacionException"/> si algo falla.</summary>
    /// <typeparam name="T">Tipo del objeto validado.</typeparam>
    /// <param name="validador">Validador a ejecutar.</param>
    /// <param name="instancia">Objeto a validar.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Tarea que se completa cuando la validacion fue exitosa.</returns>
    /// <exception cref="ValidacionException">Si el objeto no supera la validacion.</exception>
    public static async Task AsegurarValidoAsync<T>(
        this IValidator<T> validador,
        T instancia,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(validador);

        var resultado = await validador.ValidateAsync(instancia, cancelacion);

        if (!resultado.IsValid)
        {
            throw new ValidacionException(resultado.Errors);
        }
    }
}
