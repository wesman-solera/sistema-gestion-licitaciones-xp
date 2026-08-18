using Licitaciones.Domain.Abstracciones;

namespace Licitaciones.Infrastructure.Servicios;

/// <summary>Implementacion real del reloj del sistema.</summary>
/// <remarks>
/// Es el unico lugar de toda la solucion donde se lee la hora del sistema operativo. Las
/// pruebas sustituyen esta implementacion por un reloj fijo para poder ejercitar el
/// vencimiento de licitaciones sin depender del momento en que se ejecuten.
/// </remarks>
public sealed class RelojSistema : IRelojSistema
{
    /// <inheritdoc />
    public DateTimeOffset AhoraUtc => DateTimeOffset.UtcNow;
}
