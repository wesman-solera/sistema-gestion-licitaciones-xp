using Licitaciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Licitaciones.Infrastructure.Persistencia.Configuraciones;

/// <summary>Mapeo relacional de la entidad <see cref="Licitacion"/>.</summary>
public sealed class LicitacionConfiguracion : IEntityTypeConfiguration<Licitacion>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Licitacion> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("licitaciones", t =>
        {
            // La regla "el presupuesto debe ser mayor que cero" se valida en interfaz, servidor
            // y base de datos. Esta restriccion es la tercera capa: ningun camino, ni siquiera
            // una consulta manual, puede insertar un presupuesto invalido.
            t.HasCheckConstraint(
                "ck_licitaciones_presupuesto_positivo",
                "presupuesto_estimado_crc > 0");

            t.HasCheckConstraint(
                "ck_licitaciones_estado_valido",
                "estado IN (0, 1, 2)");
        });

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(l => l.Codigo)
            .HasColumnName("codigo")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.CodigoNormalizado)
            .HasColumnName("codigo_normalizado")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.Titulo)
            .HasColumnName("titulo")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(l => l.Estado)
            .HasColumnName("estado")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(l => l.FechaCierre)
            .HasColumnName("fecha_cierre")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(l => l.PresupuestoEstimadoCrc)
            .HasColumnName("presupuesto_estimado_crc")
            // Seccion 7: los montos usan decimal con precision explicita. Queda prohibido
            // float o double, que no representan exactamente los valores monetarios.
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(l => l.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(l => l.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(l => l.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone");

        // Concurrencia optimista sobre la columna de sistema xmin de PostgreSQL: no ocupa
        // espacio adicional y cambia sola en cada UPDATE (seccion 11).
        builder.Property(l => l.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(l => l.CodigoNormalizado)
            .IsUnique()
            .HasDatabaseName("ux_licitaciones_codigo_normalizado");

        builder.HasIndex(l => l.Estado)
            .HasDatabaseName("ix_licitaciones_estado");

        builder.HasIndex(l => l.FechaCierre)
            .HasDatabaseName("ix_licitaciones_fecha_cierre");

        builder.HasMany(l => l.Ofertas)
            .WithOne(o => o.Licitacion!)
            .HasForeignKey(o => o.LicitacionId)
            // Restrict y no Cascade: borrar una licitacion no debe arrastrar sus ofertas,
            // que son evidencia del proceso (seccion 8.9).
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(l => l.Ofertas)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Metadata
            .FindNavigation(nameof(Licitacion.Ofertas))!
            .SetField("_ofertas");
    }
}
