using Licitaciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.Infrastructure.Persistencia;

/// <summary>
/// Contexto de Entity Framework Core del sistema de licitaciones.
/// </summary>
/// <remarks>
/// La persistencia es exclusivamente PostgreSQL, tal como exige la seccion 11 del enunciado.
/// Los nombres de tabla y columna se declaran de forma explicita en snake_case dentro de cada
/// configuracion: depender de la convencion por defecto haria que un cambio de version del
/// proveedor pudiera renombrar columnas de forma silenciosa.
/// </remarks>
public sealed class LicitacionesDbContext : DbContext
{
    /// <summary>Inicializa el contexto con sus opciones.</summary>
    /// <param name="opciones">Opciones de configuracion del contexto.</param>
    public LicitacionesDbContext(DbContextOptions<LicitacionesDbContext> opciones)
        : base(opciones)
    {
    }

    /// <summary>Licitaciones registradas.</summary>
    public DbSet<Licitacion> Licitaciones => Set<Licitacion>();

    /// <summary>Proveedores registrados.</summary>
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();

    /// <summary>Ofertas presentadas.</summary>
    public DbSet<Oferta> Ofertas => Set<Oferta>();

    /// <summary>Rangos parametrizables de aprobacion.</summary>
    public DbSet<NivelAprobacion> NivelesAprobacion => Set<NivelAprobacion>();

    /// <summary>Tipos de cambio administrados localmente.</summary>
    public DbSet<TipoCambio> TiposCambio => Set<TipoCambio>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        // Todas las configuraciones viven en clases separadas dentro de /Configuraciones para
        // que este archivo no crezca hasta volverse ilegible.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LicitacionesDbContext).Assembly);
    }
}
