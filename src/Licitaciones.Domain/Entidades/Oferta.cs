using Licitaciones.Domain.Constantes;
using Licitaciones.Domain.Excepciones;
using Licitaciones.Domain.Servicios;

namespace Licitaciones.Domain.Entidades;

/// <summary>
/// Propuesta economica presentada por un proveedor para una licitacion concreta.
/// </summary>
/// <remarks>
/// Una oferta se crea siempre a traves de <see cref="Registrar"/>, que recibe la licitacion
/// destino y valida en un solo lugar las cuatro condiciones de la seccion 8.5 y 8.2:
/// monto positivo, monto que no supera el presupuesto, licitacion publicada y licitacion vigente.
/// La unicidad proveedor + licitacion se refuerza ademas con un indice unico compuesto.
/// <para>
/// Las ofertas de licitaciones cerradas son inmutables y no admiten borrado logico:
/// el enunciado exige conservarlas como evidencia (seccion 8.9).
/// </para>
/// </remarks>
public sealed class Oferta
{
    /// <summary>Identificador generado por el sistema. No es editable por el usuario.</summary>
    public Guid Id { get; private set; }

    /// <summary>Licitacion a la que pertenece la oferta.</summary>
    public Guid LicitacionId { get; private set; }

    /// <summary>Proveedor que presenta la oferta.</summary>
    public Guid ProveedorId { get; private set; }

    /// <summary>Monto ofertado en colones costarricenses.</summary>
    /// <remarks>Se persiste como <c>numeric(18,2)</c>; nunca como punto flotante (seccion 7).</remarks>
    public decimal MontoOfertadoCrc { get; private set; }

    /// <summary>Instante en que la oferta quedo registrada, en UTC.</summary>
    /// <remarks>Define el orden de desempate cuando dos ofertas empatan en monto (seccion 8.6).</remarks>
    public DateTimeOffset FechaRegistro { get; private set; }

    /// <summary>Instante de la ultima modificacion del registro, en UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Token de concurrencia optimista mapeado a la columna de sistema <c>xmin</c>.</summary>
    public uint Version { get; private set; }

    /// <summary>Licitacion asociada. Propiedad de navegacion gestionada por Entity Framework Core.</summary>
    public Licitacion? Licitacion { get; private set; }

    /// <summary>Proveedor asociado. Propiedad de navegacion gestionada por Entity Framework Core.</summary>
    public Proveedor? Proveedor { get; private set; }

    /// <summary>Constructor requerido por Entity Framework Core.</summary>
    private Oferta()
    {
    }

    /// <summary>
    /// Registra una oferta validando todas las reglas de aceptacion.
    /// </summary>
    /// <param name="licitacion">Licitacion destino, ya cargada desde el repositorio.</param>
    /// <param name="proveedorId">Identificador del proveedor oferente.</param>
    /// <param name="montoOfertadoCrc">Monto propuesto en colones.</param>
    /// <param name="ahoraUtc">Instante actual, obtenido del reloj inyectado.</param>
    /// <returns>Una oferta valida lista para persistirse.</returns>
    /// <exception cref="ReglaNegocioVioladaException">Si alguna condicion de aceptacion falla.</exception>
    public static Oferta Registrar(
        Licitacion licitacion,
        Guid proveedorId,
        decimal montoOfertadoCrc,
        DateTimeOffset ahoraUtc)
    {
        ArgumentNullException.ThrowIfNull(licitacion);

        ValidarMontoPositivo(montoOfertadoCrc);
        AsegurarLicitacionRecibeOfertas(licitacion, ahoraUtc);
        ValidarMontoContraPresupuesto(montoOfertadoCrc, licitacion.PresupuestoEstimadoCrc);

        return new Oferta
        {
            Id = Guid.CreateVersion7(),
            LicitacionId = licitacion.Id,
            ProveedorId = proveedorId,
            MontoOfertadoCrc = montoOfertadoCrc,
            FechaRegistro = ahoraUtc,
            UpdatedAt = ahoraUtc
        };
    }

    /// <summary>
    /// Modifica el monto de una oferta existente.
    /// </summary>
    /// <param name="licitacion">Licitacion asociada, necesaria para revalidar el presupuesto.</param>
    /// <param name="nuevoMontoCrc">Nuevo monto propuesto.</param>
    /// <param name="ahoraUtc">Instante actual, obtenido del reloj inyectado.</param>
    /// <exception cref="ReglaNegocioVioladaException">Si la licitacion vencio o el monto no es valido.</exception>
    public void CambiarMonto(Licitacion licitacion, decimal nuevoMontoCrc, DateTimeOffset ahoraUtc)
    {
        ArgumentNullException.ThrowIfNull(licitacion);

        ValidarMontoPositivo(nuevoMontoCrc);
        AsegurarMutable(licitacion, ahoraUtc);
        ValidarMontoContraPresupuesto(nuevoMontoCrc, licitacion.PresupuestoEstimadoCrc);

        MontoOfertadoCrc = nuevoMontoCrc;
        UpdatedAt = ahoraUtc;
    }

    /// <summary>
    /// Verifica que la oferta pueda editarse o eliminarse en este momento.
    /// </summary>
    /// <param name="licitacion">Licitacion asociada.</param>
    /// <param name="ahoraUtc">Instante actual, obtenido del reloj inyectado.</param>
    /// <exception cref="ReglaNegocioVioladaException">
    /// Si la licitacion esta cerrada o vencida: en ese caso la oferta es evidencia inmutable.
    /// </exception>
    public void AsegurarMutable(Licitacion licitacion, DateTimeOffset ahoraUtc)
    {
        ArgumentNullException.ThrowIfNull(licitacion);

        if (licitacion.EstaCerradaFuncionalmente(ahoraUtc))
        {
            throw new ReglaNegocioVioladaException(
                "Las ofertas de una licitacion cerrada o vencida se conservan como evidencia y no pueden modificarse ni eliminarse.",
                CodigosError.OfertaInmutable);
        }
    }

    private static void AsegurarLicitacionRecibeOfertas(Licitacion licitacion, DateTimeOffset ahoraUtc)
    {
        if (licitacion.EstaEliminada)
        {
            throw new ReglaNegocioVioladaException(
                "La licitacion indicada no esta disponible.",
                CodigosError.RecursoNoEncontrado);
        }

        if (licitacion.Estado != Enums.EstadoLicitacion.Publicada)
        {
            throw new ReglaNegocioVioladaException(
                "Solo se admiten ofertas en licitaciones publicadas.",
                CodigosError.LicitacionNoPublicada);
        }

        // Seccion 8.2: se rechaza cuando la fecha y hora actual son iguales o posteriores al cierre.
        if (ahoraUtc >= licitacion.FechaCierre)
        {
            throw new ReglaNegocioVioladaException(
                "La licitacion ya alcanzo su fecha y hora de cierre; no admite ofertas nuevas.",
                CodigosError.LicitacionVencida);
        }
    }

    private static void ValidarMontoPositivo(decimal monto)
    {
        if (monto <= 0m)
        {
            throw new ReglaNegocioVioladaException(
                "El monto ofertado debe ser mayor que cero.",
                CodigosError.MontoNoPositivo);
        }
    }

    private static void ValidarMontoContraPresupuesto(decimal monto, decimal presupuesto)
    {
        // Seccion 8.5: una oferta igual al presupuesto es valida; solo se rechaza la que lo supera.
        if (monto > presupuesto)
        {
            throw new ReglaNegocioVioladaException(
                $"La oferta no puede superar el presupuesto estimado de la licitacion " +
                $"({NormalizadorTexto.FormatearColones(presupuesto)}).",
                CodigosError.OfertaSuperaPresupuesto);
        }
    }
}
