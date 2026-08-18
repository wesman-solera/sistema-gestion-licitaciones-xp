namespace Licitaciones.Domain.Abstracciones;

/// <summary>
/// Abstraccion del reloj del sistema.
/// </summary>
/// <remarks>
/// El enunciado (seccion 8.2) exige que el reloj sea inyectable para que las reglas de
/// vencimiento puedan probarse de forma determinista. Ninguna clase del dominio ni de la
/// capa de aplicacion debe invocar <c>DateTimeOffset.UtcNow</c> directamente.
/// </remarks>
public interface IRelojSistema
{
    /// <summary>Instante actual expresado en UTC.</summary>
    /// <remarks>
    /// Todas las comparaciones internas se hacen en UTC; la conversion a
    /// <c>America/Costa_Rica</c> ocurre unicamente en la capa de presentacion.
    /// </remarks>
    DateTimeOffset AhoraUtc { get; }
}
