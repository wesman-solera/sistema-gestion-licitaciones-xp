using Licitaciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Licitaciones.Infrastructure.Persistencia.Configuraciones;

/// <summary>Mapeo relacional de la entidad <see cref="TipoCambio"/>.</summary>
public sealed class TipoCambioConfiguracion : IEntityTypeConfiguration<TipoCambio>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TipoCambio> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("tipos_cambio", t =>
            t.HasCheckConstraint("ck_tipos_cambio_valor_positivo", "crc_por_usd > 0"));

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(t => t.CrcPorUsd)
            .HasColumnName("crc_por_usd")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(t => t.FechaVigencia)
            .HasColumnName("fecha_vigencia")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(t => t.Activo)
            .HasColumnName("activo")
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(t => t.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // Indice unico parcial: PostgreSQL solo lo aplica a las filas con activo = true, de modo
        // que garantiza que exista como maximo un tipo de cambio activo (seccion 8.8) sin
        // impedir que existan muchos historicos inactivos.
        builder.HasIndex(t => t.Activo)
            .IsUnique()
            .HasFilter("activo")
            .HasDatabaseName("ux_tipos_cambio_unico_activo");

        builder.HasIndex(t => t.FechaVigencia)
            .HasDatabaseName("ix_tipos_cambio_fecha_vigencia");
    }
}
