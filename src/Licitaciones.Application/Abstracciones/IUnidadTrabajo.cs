namespace Licitaciones.Application.Abstracciones;

/// <summary>
/// Coordina la confirmacion de los cambios pendientes y el uso de transacciones explicitas.
/// </summary>
/// <remarks>
/// Los repositorios no guardan por su cuenta: acumulan cambios y esta unidad de trabajo los
/// confirma en un solo punto. Eso permite que una operacion que toca varias entidades
/// (por ejemplo activar un tipo de cambio, que desactiva el anterior) quede en una sola
/// transaccion, tal como exige la seccion 11 del enunciado.
/// </remarks>
public interface IUnidadTrabajo
{
    /// <summary>Confirma todos los cambios pendientes.</summary>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Cantidad de filas afectadas.</returns>
    Task<int> GuardarCambiosAsync(CancellationToken cancelacion = default);

    /// <summary>
    /// Ejecuta una operacion dentro de una transaccion explicita.
    /// </summary>
    /// <typeparam name="T">Tipo del resultado devuelto por la operacion.</typeparam>
    /// <param name="operacion">Operacion a ejecutar.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El resultado de la operacion.</returns>
    /// <remarks>Si la operacion lanza una excepcion, la transaccion se revierte por completo.</remarks>
    Task<T> EnTransaccionAsync<T>(
        Func<CancellationToken, Task<T>> operacion,
        CancellationToken cancelacion = default);
}
