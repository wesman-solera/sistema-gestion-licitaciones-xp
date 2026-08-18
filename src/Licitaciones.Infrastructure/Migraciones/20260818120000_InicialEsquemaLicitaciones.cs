using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Licitaciones.Infrastructure.Migraciones;

/// <summary>
/// Migracion inicial: crea el esquema completo del sistema y sus datos semilla.
/// </summary>
/// <remarks>
/// Notas de diseno relevantes para revisar esta migracion:
/// <list type="bullet">
/// <item>
/// La columna <c>xmin</c> que usa la concurrencia optimista no se crea aqui: es una columna
/// de sistema que PostgreSQL mantiene en toda tabla. El modelo la mapea, pero el DDL no la declara.
/// </item>
/// <item>
/// Todas las claves foraneas usan <c>RESTRICT</c>. Un borrado en cascada destruiria ofertas,
/// que el enunciado exige conservar como evidencia (seccion 8.9).
/// </item>
/// <item>
/// Los montos son <c>numeric(18,2)</c>. No se usa <c>double precision</c> en ninguna columna monetaria.
/// </item>
/// </list>
/// </remarks>
public partial class InicialEsquemaLicitaciones : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        migrationBuilder.CreateTable(
            name: "proveedores",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                nombre_normalizado = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_proveedores", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "licitaciones",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                codigo_normalizado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                titulo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                estado = table.Column<int>(type: "integer", nullable: false),
                fecha_cierre = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                presupuesto_estimado_crc = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_licitaciones", x => x.id);
                table.CheckConstraint("ck_licitaciones_presupuesto_positivo", "presupuesto_estimado_crc > 0");
                table.CheckConstraint("ck_licitaciones_estado_valido", "estado IN (0, 1, 2)");
            });

        migrationBuilder.CreateTable(
            name: "niveles_aprobacion",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                monto_minimo_crc = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                monto_maximo_crc = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                aprobador = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_niveles_aprobacion", x => x.id);
                table.CheckConstraint("ck_niveles_aprobacion_minimo_positivo", "monto_minimo_crc > 0");
                table.CheckConstraint("ck_niveles_aprobacion_maximo_positivo", "monto_maximo_crc IS NULL OR monto_maximo_crc > 0");
                table.CheckConstraint("ck_niveles_aprobacion_rango_coherente", "monto_maximo_crc IS NULL OR monto_maximo_crc >= monto_minimo_crc");
            });

        migrationBuilder.CreateTable(
            name: "tipos_cambio",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                crc_por_usd = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                fecha_vigencia = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                activo = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_tipos_cambio", x => x.id);
                table.CheckConstraint("ck_tipos_cambio_valor_positivo", "crc_por_usd > 0");
            });

        migrationBuilder.CreateTable(
            name: "ofertas",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                licitacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                proveedor_id = table.Column<Guid>(type: "uuid", nullable: false),
                monto_ofertado_crc = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                fecha_registro = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_ofertas", x => x.id);
                table.CheckConstraint("ck_ofertas_monto_positivo", "monto_ofertado_crc > 0");
                table.ForeignKey(
                    name: "fk_ofertas_licitaciones_licitacion_id",
                    column: x => x.licitacion_id,
                    principalTable: "licitaciones",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_ofertas_proveedores_proveedor_id",
                    column: x => x.proveedor_id,
                    principalTable: "proveedores",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ux_proveedores_nombre_normalizado",
            table: "proveedores",
            column: "nombre_normalizado",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_licitaciones_codigo_normalizado",
            table: "licitaciones",
            column: "codigo_normalizado",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_licitaciones_estado",
            table: "licitaciones",
            column: "estado");

        migrationBuilder.CreateIndex(
            name: "ix_licitaciones_fecha_cierre",
            table: "licitaciones",
            column: "fecha_cierre");

        migrationBuilder.CreateIndex(
            name: "ux_niveles_aprobacion_monto_minimo",
            table: "niveles_aprobacion",
            column: "monto_minimo_crc",
            unique: true);

        // Indice unico parcial: solo aplica a las filas activas, de modo que impide que existan
        // dos tipos de cambio activos a la vez sin restringir el historico de inactivos.
        migrationBuilder.CreateIndex(
            name: "ux_tipos_cambio_unico_activo",
            table: "tipos_cambio",
            column: "activo",
            unique: true,
            filter: "activo");

        migrationBuilder.CreateIndex(
            name: "ix_tipos_cambio_fecha_vigencia",
            table: "tipos_cambio",
            column: "fecha_vigencia");

        migrationBuilder.CreateIndex(
            name: "ux_ofertas_licitacion_proveedor",
            table: "ofertas",
            columns: ["licitacion_id", "proveedor_id"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_ofertas_licitacion_monto",
            table: "ofertas",
            columns: ["licitacion_id", "monto_ofertado_crc"]);

        migrationBuilder.CreateIndex(
            name: "ix_ofertas_proveedor",
            table: "ofertas",
            column: "proveedor_id");

        SembrarDatosIniciales(migrationBuilder);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        migrationBuilder.DropTable(name: "ofertas");
        migrationBuilder.DropTable(name: "niveles_aprobacion");
        migrationBuilder.DropTable(name: "tipos_cambio");
        migrationBuilder.DropTable(name: "licitaciones");
        migrationBuilder.DropTable(name: "proveedores");
    }

    /// <summary>
    /// Inserta los datos semilla exigidos por la seccion 11 del enunciado.
    /// </summary>
    /// <param name="migrationBuilder">Constructor de la migracion.</param>
    /// <remarks>
    /// Los tres rangos de aprobacion reproducen exactamente la tabla de la seccion 8.7. El tipo
    /// de cambio inicial permite que la conversion a dolares funcione desde el primer arranque,
    /// sin Internet y sin configuracion manual previa. Los identificadores son fijos para que
    /// la semilla sea idempotente entre entornos y las pruebas puedan referenciarlos.
    /// </remarks>
    private static void SembrarDatosIniciales(MigrationBuilder migrationBuilder)
    {
        var momento = new DateTimeOffset(2026, 8, 18, 0, 0, 0, TimeSpan.Zero);

        migrationBuilder.InsertData(
            table: "niveles_aprobacion",
            columns: ["id", "monto_minimo_crc", "monto_maximo_crc", "aprobador", "created_at", "updated_at"],
            values: new object[,]
            {
                {
                    new Guid("a1000000-0000-4000-8000-000000000001"),
                    0.01m,
                    999_999.99m,
                    "Encargado de area",
                    momento,
                    momento
                },
                {
                    new Guid("a1000000-0000-4000-8000-000000000002"),
                    1_000_000.00m,
                    9_999_999.99m,
                    "Gerencia",
                    momento,
                    momento
                }
            });

        // El rango abierto se inserta aparte porque su monto maximo es NULL y el arreglo
        // bidimensional anterior no admite mezclar un nulo sin ambiguedad de tipo.
        migrationBuilder.InsertData(
            table: "niveles_aprobacion",
            columns: ["id", "monto_minimo_crc", "monto_maximo_crc", "aprobador", "created_at", "updated_at"],
            values:
            [
                new Guid("a1000000-0000-4000-8000-000000000003"),
                10_000_000.00m,
                null,
                "Junta Directiva",
                momento,
                momento
            ]);

        migrationBuilder.InsertData(
            table: "tipos_cambio",
            columns: ["id", "crc_por_usd", "fecha_vigencia", "activo", "created_at", "updated_at"],
            values:
            [
                new Guid("b2000000-0000-4000-8000-000000000001"),
                505.00m,
                momento,
                true,
                momento,
                momento
            ]);
    }
}
