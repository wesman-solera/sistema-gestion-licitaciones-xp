using Licitaciones.Application.Abstracciones;
using Licitaciones.Domain.Entidades;
using Licitaciones.IntegrationTests.Infraestructura;
using Licitaciones.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.IntegrationTests.Persistencia;

/// <summary>
/// Concurrencia optimista y transacciones (seccion 11).
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
public sealed class ConcurrenciaYTransaccionPruebas : IAsyncLifetime
{
    private static readonly DateTimeOffset Ahora = new(2026, 8, 18, 12, 0, 0, TimeSpan.Zero);

    private readonly PostgresFixture _postgres;

    /// <summary>Inicializa la prueba con el contenedor compartido.</summary>
    /// <param name="postgres">Contenedor de PostgreSQL.</param>
    public ConcurrenciaYTransaccionPruebas(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    /// <inheritdoc />
    public Task InitializeAsync() => _postgres.LimpiarDatosTransaccionalesAsync();

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Dos usuarios cargan la misma licitacion y ambos la guardan. El segundo debe fallar en
    /// lugar de sobrescribir en silencio el cambio del primero.
    /// </summary>
    [Fact]
    public async Task ConcurrenciaOptimista_DetectaLaEscrituraSimultaneaDeDosContextos()
    {
        Guid id;

        await using (LicitacionesDbContext preparacion = _postgres.CrearContexto())
        {
            Licitacion licitacion =
                Licitacion.Crear("LIC-CONC-001", "Titulo original", 1_000_000m, Ahora.AddDays(7), Ahora);

            preparacion.Licitaciones.Add(licitacion);
            await preparacion.SaveChangesAsync();

            id = licitacion.Id;
        }

        await using LicitacionesDbContext usuarioA = _postgres.CrearContexto();
        await using LicitacionesDbContext usuarioB = _postgres.CrearContexto();

        Licitacion versionA = await usuarioA.Licitaciones.SingleAsync(l => l.Id == id);
        Licitacion versionB = await usuarioB.Licitaciones.SingleAsync(l => l.Id == id);

        versionA.ActualizarDatos("Titulo del usuario A", 900_000m, versionA.FechaCierre, null, Ahora);
        await usuarioA.SaveChangesAsync();

        versionB.ActualizarDatos("Titulo del usuario B", 800_000m, versionB.FechaCierre, null, Ahora);

        Func<Task> accion = () => usuarioB.SaveChangesAsync();

        await accion.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task ConcurrenciaOptimista_LaVersionCambiaEnCadaActualizacion()
    {
        await using LicitacionesDbContext contexto = _postgres.CrearContexto();

        Licitacion licitacion =
            Licitacion.Crear("LIC-CONC-002", "Titulo", 1_000_000m, Ahora.AddDays(7), Ahora);

        contexto.Licitaciones.Add(licitacion);
        await contexto.SaveChangesAsync();

        uint versionInicial = licitacion.Version;

        licitacion.ActualizarDatos("Titulo modificado", 900_000m, licitacion.FechaCierre, null, Ahora);
        await contexto.SaveChangesAsync();

        licitacion.Version.Should().NotBe(versionInicial);
    }

    /// <summary>
    /// La activacion de un tipo de cambio desactiva el anterior dentro de una sola transaccion.
    /// Si no fuera transaccional, el indice unico parcial rechazaria el paso intermedio con dos
    /// registros activos.
    /// </summary>
    [Fact]
    public async Task Transaccion_ActivarUnTipoDeCambioDesactivaElAnteriorEnUnSoloPaso()
    {
        await using LicitacionesDbContext contexto = _postgres.CrearContexto();

        TipoCambio nuevo = TipoCambio.Crear(530m, Ahora, activo: false, Ahora);
        contexto.TiposCambio.Add(nuevo);
        await contexto.SaveChangesAsync();

        IUnidadTrabajo unidadTrabajo = new UnidadTrabajo(contexto);

        await unidadTrabajo.EnTransaccionAsync(async ct =>
        {
            var activos = await contexto.TiposCambio.Where(t => t.Activo).ToListAsync(ct);

            foreach (TipoCambio activo in activos)
            {
                activo.Desactivar(Ahora);
            }

            // Se confirma la desactivacion antes de activar el nuevo: si ambas escrituras
            // viajaran juntas, PostgreSQL evaluaria el indice unico con dos filas activas.
            await contexto.SaveChangesAsync(ct);

            nuevo.Activar(Ahora);

            return true;
        });

        var activosFinales = await contexto.TiposCambio
            .AsNoTracking()
            .Where(t => t.Activo)
            .ToListAsync();

        activosFinales.Should().ContainSingle().Which.Id.Should().Be(nuevo.Id);
    }

    /// <summary>
    /// Si la operacion falla a mitad de camino, la transaccion debe revertir todo lo escrito.
    /// </summary>
    [Fact]
    public async Task Transaccion_RevierteTodoCuandoLaOperacionFalla()
    {
        await using LicitacionesDbContext contexto = _postgres.CrearContexto();

        IUnidadTrabajo unidadTrabajo = new UnidadTrabajo(contexto);

        Func<Task> accion = () => unidadTrabajo.EnTransaccionAsync<bool>(async ct =>
        {
            contexto.Proveedores.Add(Proveedor.Crear("Proveedor que no debe quedar", Ahora));
            await contexto.SaveChangesAsync(ct);

            throw new InvalidOperationException("Fallo simulado a mitad de la operacion.");
        });

        await accion.Should().ThrowAsync<InvalidOperationException>();

        await using LicitacionesDbContext verificacion = _postgres.CrearContexto();

        bool existe = await verificacion.Proveedores
            .AnyAsync(p => p.NombreNormalizado == "PROVEEDOR QUE NO DEBE QUEDAR");

        existe.Should().BeFalse();
    }

    /// <summary>
    /// Las fechas se comparan en UTC. Al recuperarlas, el instante debe ser el mismo que se
    /// guardo, sin desplazamiento introducido por la zona horaria del servidor (seccion 8.2).
    /// </summary>
    [Fact]
    public async Task Fechas_SeConservanEnUtcAlPersistirYRecuperar()
    {
        var fechaCierre = new DateTimeOffset(2026, 12, 31, 23, 30, 0, TimeSpan.Zero);
        Guid id;

        await using (LicitacionesDbContext escritura = _postgres.CrearContexto())
        {
            Licitacion licitacion =
                Licitacion.Crear("LIC-UTC-001", "Titulo", 1_000_000m, fechaCierre, Ahora);

            escritura.Licitaciones.Add(licitacion);
            await escritura.SaveChangesAsync();

            id = licitacion.Id;
        }

        await using LicitacionesDbContext lectura = _postgres.CrearContexto();
        Licitacion recuperada = await lectura.Licitaciones.SingleAsync(l => l.Id == id);

        recuperada.FechaCierre.ToUniversalTime().Should().Be(fechaCierre);
    }
}
