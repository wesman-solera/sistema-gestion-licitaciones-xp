using Licitaciones.Application.Abstracciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Licitaciones.Infrastructure.Persistencia;

/// <inheritdoc cref="IUnidadTrabajo"/>
public sealed class UnidadTrabajo : IUnidadTrabajo
{
    private readonly LicitacionesDbContext _contexto;

    /// <summary>Inicializa la unidad de trabajo con el contexto de datos.</summary>
    /// <param name="contexto">Contexto de Entity Framework Core.</param>
    public UnidadTrabajo(LicitacionesDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public Task<int> GuardarCambiosAsync(CancellationToken cancelacion = default)
        => _contexto.SaveChangesAsync(cancelacion);

    /// <inheritdoc />
    /// <remarks>
    /// Si ya hay una transaccion abierta (por ejemplo porque una prueba de integracion envolvio
    /// la operacion) se reutiliza en lugar de anidar otra, que PostgreSQL no admite.
    /// </remarks>
    public async Task<T> EnTransaccionAsync<T>(
        Func<CancellationToken, Task<T>> operacion,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(operacion);

        if (_contexto.Database.CurrentTransaction is not null)
        {
            return await operacion(cancelacion);
        }

        await using IDbContextTransaction transaccion =
            await _contexto.Database.BeginTransactionAsync(cancelacion);

        try
        {
            T resultado = await operacion(cancelacion);

            await _contexto.SaveChangesAsync(cancelacion);
            await transaccion.CommitAsync(cancelacion);

            return resultado;
        }
        catch
        {
            await transaccion.RollbackAsync(cancelacion);
            throw;
        }
    }
}
