using Licitaciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Licitaciones.Infrastructure.Persistencia.Configuraciones;

/// <summary>Mapeo relacional de la entidad <see cref="NivelAprobacion"/>.</summary>
public sealed class NivelAprobacionConfiguracion : IEntityTypeConfiguration<NivelAprobacion>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<NivelAprobacion> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("niveles_aprobacion", t =>
        {
            t.HasCheckConstraint(
                "ck_niveles_aprobacion_minimo_positivo",
                "monto_minimo_crc > 0");

            t.HasCheckConstraint(
                "ck_niveles_aprobacion_maximo_positivo",
                "monto_maximo_crc IS NULL OR monto_maximo_crc > 0");

            // La coherencia interna del rango se garantiza en la base de datos. El no traslape
            // entre rangos distintos no puede expresarse con un CHECK de fila y se valida en
            // la capa de aplicacion sobre el conjunto completo.
            t.HasCheckConstraint(
                "ck_niveles_aprobacion_rango_coherente",
                "monto_maximo_crc IS NULL OR monto_maximo_crc >= monto_minimo_crc");
        });

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(n => n.MontoMinimoCrc)
            .HasColumnName("monto_minimo_crc")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(n => n.MontoMaximoCrc)
            .HasColumnName("monto_maximo_crc")
            .HasColumnType("numeric(18,2)");

        builder.Property(n => n.Aprobador)
            .HasColumnName("aprobador")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(n => n.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(n => n.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(n => n.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(n => n.MontoMinimoCrc)
            .IsUnique()
            .HasDatabaseName("ux_niveles_aprobacion_monto_minimo");
    }
}
