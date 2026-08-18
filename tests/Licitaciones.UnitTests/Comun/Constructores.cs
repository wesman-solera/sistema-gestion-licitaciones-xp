using Licitaciones.Domain.Entidades;

namespace Licitaciones.UnitTests.Comun;

/// <summary>
/// Constructores de entidades validas para las pruebas.
/// </summary>
/// <remarks>
/// Cada prueba debe leerse por lo que verifica, no por el andamiaje que necesita para llegar
/// ahi. Estos ayudantes producen entidades validas por defecto y permiten alterar solo el dato
/// que la prueba quiere poner a prueba.
/// </remarks>
public static class Constructores
{
    /// <summary>Presupuesto usado por defecto en las licitaciones de prueba.</summary>
    public const decimal PresupuestoPorDefecto = 1_000_000m;

    /// <summary>Crea una licitacion en estado Borrador con datos validos.</summary>
    /// <param name="reloj">Reloj de la prueba.</param>
    /// <param name="codigo">Codigo de la licitacion.</param>
    /// <param name="presupuestoCrc">Presupuesto estimado.</param>
    /// <param name="horasHastaCierre">Horas que faltan para el cierre desde el instante del reloj.</param>
    /// <returns>Una licitacion valida en Borrador.</returns>
    public static Licitacion CrearLicitacion(
        RelojFijo reloj,
        string codigo = "LIC-2026-001",
        decimal presupuestoCrc = PresupuestoPorDefecto,
        int horasHastaCierre = 48)
    {
        ArgumentNullException.ThrowIfNull(reloj);

        return Licitacion.Crear(
            codigo,
            "Compra de equipo de computo",
            presupuestoCrc,
            reloj.AhoraUtc.AddHours(horasHastaCierre),
            reloj.AhoraUtc);
    }

    /// <summary>Crea una licitacion ya publicada con datos validos.</summary>
    /// <param name="reloj">Reloj de la prueba.</param>
    /// <param name="codigo">Codigo de la licitacion.</param>
    /// <param name="presupuestoCrc">Presupuesto estimado.</param>
    /// <param name="horasHastaCierre">Horas que faltan para el cierre desde el instante del reloj.</param>
    /// <returns>Una licitacion valida en estado Publicada.</returns>
    public static Licitacion CrearLicitacionPublicada(
        RelojFijo reloj,
        string codigo = "LIC-2026-001",
        decimal presupuestoCrc = PresupuestoPorDefecto,
        int horasHastaCierre = 48)
    {
        Licitacion licitacion = Licitacion(reloj, codigo, presupuestoCrc, horasHastaCierre);
        licitacion.Publicar(reloj.AhoraUtc);

        return licitacion;
    }

    /// <summary>Crea un proveedor valido.</summary>
    /// <param name="reloj">Reloj de la prueba.</param>
    /// <param name="nombre">Nombre del proveedor.</param>
    /// <returns>Un proveedor valido.</returns>
    public static Proveedor CrearProveedor(RelojFijo reloj, string nombre = "Empresa Central")
    {
        ArgumentNullException.ThrowIfNull(reloj);

        return Proveedor.Crear(nombre, reloj.AhoraUtc);
    }

    /// <summary>Registra una oferta valida para la licitacion indicada.</summary>
    /// <param name="licitacion">Licitacion destino, que debe estar publicada y vigente.</param>
    /// <param name="reloj">Reloj de la prueba.</param>
    /// <param name="montoCrc">Monto ofertado.</param>
    /// <param name="proveedorId">Proveedor oferente. Si se omite se genera uno nuevo.</param>
    /// <returns>Una oferta valida.</returns>
    public static Oferta CrearOferta(
        Licitacion licitacion,
        RelojFijo reloj,
        decimal montoCrc,
        Guid? proveedorId = null)
    {
        ArgumentNullException.ThrowIfNull(reloj);

        return Oferta.Registrar(
            licitacion,
            proveedorId ?? Guid.NewGuid(),
            montoCrc,
            reloj.AhoraUtc);
    }

    /// <summary>Crea la tabla de niveles de aprobacion del enunciado.</summary>
    /// <param name="reloj">Reloj de la prueba.</param>
    /// <returns>Los tres rangos de la seccion 8.7.</returns>
    public static IReadOnlyList<NivelAprobacion> CrearNivelesDelEnunciado(RelojFijo reloj)
    {
        ArgumentNullException.ThrowIfNull(reloj);

        return
        [
            NivelAprobacion.Crear(0.01m, 999_999.99m, "Encargado de area", reloj.AhoraUtc),
            NivelAprobacion.Crear(1_000_000.00m, 9_999_999.99m, "Gerencia", reloj.AhoraUtc),
            NivelAprobacion.Crear(10_000_000.00m, null, "Junta Directiva", reloj.AhoraUtc)
        ];
    }
}
