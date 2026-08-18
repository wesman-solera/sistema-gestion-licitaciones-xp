using Licitaciones.Domain.Constantes;
using Licitaciones.Domain.Excepciones;

namespace Licitaciones.Domain.Entidades;

/// <summary>
/// Tipo de cambio administrable que expresa cuantos colones equivalen a un dolar.
/// </summary>
/// <remarks>
/// El enunciado (seccion 8.8) exige que la solucion funcione sin Internet: el valor se
/// administra localmente y nunca se consulta a un servicio externo. Los montos oficiales se
/// almacenan solo en colones; la conversion a dolares es una representacion calculada en el
/// momento de mostrarla y jamas se persiste.
/// <para>
/// Solo puede existir un tipo de cambio activo para la operacion ordinaria. La activacion es
/// una operacion transaccional que desactiva el anterior, coordinada por la capa de aplicacion.
/// </para>
/// </remarks>
public sealed class TipoCambio
{
    /// <summary>Identificador generado por el sistema. No es editable por el usuario.</summary>
    public Guid Id { get; private set; }

    /// <summary>Cantidad de colones que equivalen a un dolar estadounidense.</summary>
    /// <remarks>Se persiste como <c>numeric(18,2)</c> y debe ser estrictamente mayor que cero.</remarks>
    public decimal CrcPorUsd { get; private set; }

    /// <summary>Fecha desde la que rige el tipo de cambio, en UTC.</summary>
    /// <remarks>Se muestra junto a los montos convertidos, tal como pide la seccion 8.8.</remarks>
    public DateTimeOffset FechaVigencia { get; private set; }

    /// <summary>Indica si este es el tipo de cambio en uso para la operacion ordinaria.</summary>
    public bool Activo { get; private set; }

    /// <summary>Instante de creacion del registro, en UTC.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Instante de la ultima modificacion del registro, en UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Token de concurrencia optimista mapeado a la columna de sistema <c>xmin</c>.</summary>
    public uint Version { get; private set; }

    /// <summary>Constructor requerido por Entity Framework Core.</summary>
    private TipoCambio()
    {
    }

    /// <summary>Crea un tipo de cambio.</summary>
    /// <param name="crcPorUsd">Colones por dolar, estrictamente mayor que cero.</param>
    /// <param name="fechaVigencia">Fecha desde la que rige.</param>
    /// <param name="activo">Indica si queda activo de inmediato.</param>
    /// <param name="ahoraUtc">Instante actual, obtenido del reloj inyectado.</param>
    /// <returns>Un tipo de cambio valido.</returns>
    /// <exception cref="ReglaNegocioVioladaException">Si el valor no es positivo.</exception>
    public static TipoCambio Crear(
        decimal crcPorUsd,
        DateTimeOffset fechaVigencia,
        bool activo,
        DateTimeOffset ahoraUtc)
    {
        ValidarValor(crcPorUsd);

        return new TipoCambio
        {
            Id = Guid.CreateVersion7(),
            CrcPorUsd = crcPorUsd,
            FechaVigencia = fechaVigencia.ToUniversalTime(),
            Activo = activo,
            CreatedAt = ahoraUtc,
            UpdatedAt = ahoraUtc
        };
    }

    /// <summary>Actualiza el valor y la fecha de vigencia.</summary>
    /// <param name="crcPorUsd">Nuevo valor en colones por dolar.</param>
    /// <param name="fechaVigencia">Nueva fecha de vigencia.</param>
    /// <param name="ahoraUtc">Instante actual, obtenido del reloj inyectado.</param>
    /// <exception cref="ReglaNegocioVioladaException">Si el valor no es positivo.</exception>
    public void Actualizar(decimal crcPorUsd, DateTimeOffset fechaVigencia, DateTimeOffset ahoraUtc)
    {
        ValidarValor(crcPorUsd);

        CrcPorUsd = crcPorUsd;
        FechaVigencia = fechaVigencia.ToUniversalTime();
        UpdatedAt = ahoraUtc;
    }

    /// <summary>Marca este tipo de cambio como el activo.</summary>
    /// <param name="ahoraUtc">Instante actual, obtenido del reloj inyectado.</param>
    public void Activar(DateTimeOffset ahoraUtc)
    {
        if (Activo)
        {
            return;
        }

        Activo = true;
        UpdatedAt = ahoraUtc;
    }

    /// <summary>Retira la marca de activo de este tipo de cambio.</summary>
    /// <param name="ahoraUtc">Instante actual, obtenido del reloj inyectado.</param>
    public void Desactivar(DateTimeOffset ahoraUtc)
    {
        if (!Activo)
        {
            return;
        }

        Activo = false;
        UpdatedAt = ahoraUtc;
    }

    private static void ValidarValor(decimal crcPorUsd)
    {
        if (crcPorUsd <= 0m)
        {
            throw new ReglaNegocioVioladaException(
                "El tipo de cambio debe ser mayor que cero.",
                CodigosError.TipoCambioInvalido);
        }
    }
}
