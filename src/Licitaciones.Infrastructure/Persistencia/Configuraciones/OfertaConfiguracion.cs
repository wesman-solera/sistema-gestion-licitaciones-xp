using Licitaciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Licitaciones.Infrastructure.Persistencia.Configuraciones;

/// <summary>Mapeo relacional de la entidad <see cref="Oferta"/>.</summary>
public sealed class OfertaConfiguracion : IEntityTypeConfiguration<Oferta>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Oferta> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ofertas", t =>
            t.HasCheckConstraint("ck_ofertas_monto_positivo", "monto_ofertado_crc > 0"));

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(o => o.LicitacionId)
            .HasColumnName("licitacion_id")
            .IsRequired();

        builder.Property(o => o.ProveedorId)
            .HasColumnName("proveedor_id")
            .IsRequired();

        builder.Property(o => o.MontoOfertadoCrc)
            .HasColumnName("monto_ofertado_crc")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(o => o.FechaRegistro)
            .HasColumnName("fecha_registro")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(o => o.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(o => o.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // Indice unico compuesto exigido explicitamente por la seccion 8.3: un proveedor no
        // puede registrar mas de una oferta para la misma licitacion. Cubre la carrera entre
        // dos peticiones simultaneas que superen ambas la comprobacion de aplicacion.
        builder.HasIndex(o => new { o.LicitacionId, o.ProveedorId })
            .IsUnique()
            .HasDatabaseName("ux_ofertas_licitacion_proveedor");

        // Soporta la busqueda de la mejor oferta sin recorrer toda la tabla.
        builder.HasIndex(o => new { o.LicitacionId, o.MontoOfertadoCrc })
            .HasDatabaseName("ix_ofertas_licitacion_monto");

        builder.HasIndex(o => o.ProveedorId)
            .HasDatabaseName("ix_ofertas_proveedor");
    }
}
