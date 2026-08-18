using Licitaciones.Domain.Enums;
using Licitaciones.Domain.Excepciones;

namespace Licitaciones.Domain.Servicios;

/// <summary>
/// Tabla de transiciones permitidas del ciclo de vida de una licitacion (seccion 8.1).
/// </summary>
/// <remarks>
/// La politica se expresa como un conjunto de datos y no como una cadena de condiciones
/// <c>if/else</c>: agregar o retirar una transicion es modificar la tabla, no la logica.
/// Esto mantiene el diseno simple y hace que las pruebas puedan recorrer exhaustivamente
/// todas las combinaciones de origen y destino.
/// </remarks>
public static class PoliticaTransicionEstado
{
    private static readonly HashSet<(EstadoLicitacion Origen, EstadoLicitacion Destino)> Permitidas =
    [
        (EstadoLicitacion.Borrador, EstadoLicitacion.Publicada),
        (EstadoLicitacion.Borrador, EstadoLicitacion.Cerrada),
        (EstadoLicitacion.Publicada, EstadoLicitacion.Cerrada)
    ];

    private static readonly Dictionary<(EstadoLicitacion, EstadoLicitacion), string> Motivos = new()
    {
        [(EstadoLicitacion.Publicada, EstadoLicitacion.Borrador)] =
            "Una licitacion publicada no puede regresar a Borrador.",
        [(EstadoLicitacion.Cerrada, EstadoLicitacion.Publicada)] =
            "La reapertura de una licitacion cerrada requiere autorizacion expresa de la persona docente.",
        [(EstadoLicitacion.Cerrada, EstadoLicitacion.Borrador)] =
            "La reapertura de una licitacion cerrada requiere autorizacion expresa de la persona docente."
    };

    /// <summary>Indica si la transicion solicitada esta permitida.</summary>
    /// <param name="origen">Estado actual.</param>
    /// <param name="destino">Estado solicitado.</param>
    /// <returns><c>true</c> si la tabla de transiciones la contempla.</returns>
    public static bool EsPermitida(EstadoLicitacion origen, EstadoLicitacion destino)
        => Permitidas.Contains((origen, destino));

    /// <summary>Lanza una excepcion si la transicion solicitada no esta permitida.</summary>
    /// <param name="origen">Estado actual.</param>
    /// <param name="destino">Estado solicitado.</param>
    /// <exception cref="TransicionEstadoInvalidaException">Siempre que la transicion no este en la tabla.</exception>
    public static void AsegurarTransicionPermitida(EstadoLicitacion origen, EstadoLicitacion destino)
    {
        if (EsPermitida(origen, destino))
        {
            return;
        }

        string motivo = Motivos.TryGetValue((origen, destino), out string? texto)
            ? texto
            : origen == destino
                ? $"La licitacion ya se encuentra en estado {destino}."
                : "La transicion solicitada no forma parte del ciclo de vida definido.";

        throw new TransicionEstadoInvalidaException(origen, destino, motivo);
    }

    /// <summary>Devuelve los estados a los que se puede transicionar desde el estado indicado.</summary>
    /// <param name="origen">Estado actual.</param>
    /// <returns>Coleccion de estados destino validos, util para habilitar botones en la interfaz.</returns>
    public static IReadOnlyCollection<EstadoLicitacion> DestinosDisponibles(EstadoLicitacion origen)
        => Permitidas.Where(t => t.Origen == origen).Select(t => t.Destino).ToArray();
}
