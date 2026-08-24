using Licitaciones.Domain.Constantes;
using Licitaciones.Domain.Enums;

namespace Licitaciones.Domain.Excepciones;

/// <summary>
/// Se lanza cuando se intenta una transicion de estado que el ciclo de vida no permite.
/// </summary>
/// <remarks>
/// El ciclo permitido esta descrito en la seccion 8.1 del enunciado e implementado en
/// <see cref="Servicios.PoliticaTransicionEstado"/>. Se traduce a HTTP 409 Conflict.
/// </remarks>
public sealed class TransicionEstadoInvalidaException : DominioException
{
    /// <summary>Estado en el que se encuentra actualmente la licitacion.</summary>
    public EstadoLicitacion EstadoActual { get; }

    /// <summary>Estado al que se intento transicionar.</summary>
    public EstadoLicitacion EstadoDestino { get; }

    /// <summary>Inicializa la excepcion a partir de la transicion rechazada.</summary>
    /// <param name="estadoActual">Estado de origen.</param>
    /// <param name="estadoDestino">Estado de destino solicitado.</param>
    /// <param name="motivo">Explicacion legible de por que se rechaza.</param>
    public TransicionEstadoInvalidaException(
        EstadoLicitacion estadoActual,
        EstadoLicitacion estadoDestino,
        string motivo)
        : base(
            $"No se permite pasar de {estadoActual} a {estadoDestino}. {motivo}",
            CodigosError.TransicionEstadoInvalida)
    {
        EstadoActual = estadoActual;
        EstadoDestino = estadoDestino;
    }
}
