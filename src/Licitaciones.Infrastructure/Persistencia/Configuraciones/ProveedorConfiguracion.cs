using Licitaciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Licitaciones.Infrastructure.Persistencia.Configuraciones;

/// <summary>Mapeo relacional de la entidad <see cref="Proveedor"/>.</summary>
public sealed class ProveedorConfiguracion : IEntityTypeConfiguration<Proveedor>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Proveedor> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("proveedores");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(p => p.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.NombreNormalizado)
            .HasColumnName("nombre_normalizado")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(p => p.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(p => p.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // Indice unico sobre la forma normalizada: es la defensa final contra el duplicado
        // "Empresa Central" / " empresa central" / "EMPRESA  CENTRAL" (seccion 8.3).
        builder.HasIndex(p => p.NombreNormalizado)
            .IsUnique()
            .HasDatabaseName("ux_proveedores_nombre_normalizado");

        builder.HasMany(p => p.Ofertas)
            .WithOne(o => o.Proveedor!)
            .HasForeignKey(o => o.ProveedorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(p => p.Ofertas)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Metadata
            .FindNavigation(nameof(Proveedor.Ofertas))!
            .SetField("_ofertas");
    }
}
