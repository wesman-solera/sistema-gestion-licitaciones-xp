using Licitaciones.Domain.Abstracciones;

namespace Licitaciones.UnitTests.Comun;

/// <summary>
/// Reloj de pruebas que devuelve siempre el instante configurado.
/// </summary>
/// <remarks>
/// Es la razon por la que <see cref="IRelojSistema"/> existe. Sin el, una prueba de vencimiento
/// dependeria de la hora real de ejecucion y pasaria o fallaria segun el momento del dia. Con el
/// reloj fijo, "la licitacion vencio" es una condicion que la prueba controla por completo.
/// </remarks>
public sealed class RelojFijo : IRelojSistema
{
    /// <summary>Instante de referencia usado por casi todas las pruebas.</summary>
    /// <remarks>Es una fecha arbitraria pero fija, para que los calculos sean reproducibles.</remarks>
    public static readonly DateTimeOffset Referencia =
        new(2026, 8, 18, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Inicializa el reloj en el instante indicado.</summary>
    /// <param name="ahora">Instante que devolvera el reloj.</param>
    public RelojFijo(DateTimeOffset? ahora = null)
    {
        AhoraUtc = ahora ?? Referencia;
    }

    /// <inheritdoc />
    public DateTimeOffset AhoraUtc { get; set; }

    /// <summary>Avanza el reloj la cantidad indicada.</summary>
    /// <param name="lapso">Tiempo que debe avanzar.</param>
    /// <returns>El mismo reloj, para encadenar llamadas.</returns>
    public RelojFijo Avanzar(TimeSpan lapso)
    {
        AhoraUtc = AhoraUtc.Add(lapso);

        return this;
    }
}
