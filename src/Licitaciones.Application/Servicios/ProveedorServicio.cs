using FluentValidation;
using Licitaciones.Application.Abstracciones;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;
using Licitaciones.Domain.Abstracciones;
using Licitaciones.Domain.Constantes;
using Licitaciones.Domain.Entidades;
using Licitaciones.Domain.Excepciones;
using Licitaciones.Domain.Servicios;

namespace Licitaciones.Application.Servicios;

/// <inheritdoc cref="IProveedorServicio"/>
/// <remarks>
/// El servicio coordina: valida el formato, comprueba la unicidad contra la base de datos,
/// delega la construccion de la entidad al dominio y confirma la transaccion. No reimplementa
/// ninguna regla que ya viva en <see cref="Proveedor"/>.
/// </remarks>
public sealed class ProveedorServicio : IProveedorServicio
{
    private readonly IProveedorRepositorio _proveedores;
    private readonly IOfertaRepositorio _ofertas;
    private readonly IUnidadTrabajo _unidadTrabajo;
    private readonly IRelojSistema _reloj;
    private readonly IValidator<CrearProveedorRequest> _validadorCrear;
    private readonly IValidator<ActualizarProveedorRequest> _validadorActualizar;

    /// <summary>Inicializa el servicio con sus dependencias.</summary>
    /// <param name="proveedores">Repositorio de proveedores.</param>
    /// <param name="ofertas">Repositorio de ofertas, necesario para decidir el tipo de borrado.</param>
    /// <param name="unidadTrabajo">Unidad de trabajo que confirma los cambios.</param>
    /// <param name="reloj">Reloj del sistema inyectado.</param>
    /// <param name="validadorCrear">Validador de la peticion de creacion.</param>
    /// <param name="validadorActualizar">Validador de la peticion de modificacion.</param>
    public ProveedorServicio(
        IProveedorRepositorio proveedores,
        IOfertaRepositorio ofertas,
        IUnidadTrabajo unidadTrabajo,
        IRelojSistema reloj,
        IValidator<CrearProveedorRequest> validadorCrear,
        IValidator<ActualizarProveedorRequest> validadorActualizar)
    {
        _proveedores = proveedores;
        _ofertas = ofertas;
        _unidadTrabajo = unidadTrabajo;
        _reloj = reloj;
        _validadorCrear = validadorCrear;
        _validadorActualizar = validadorActualizar;
    }

    /// <inheritdoc />
    public async Task<PaginaResultado<ProveedorDto>> ListarAsync(
        ParametrosConsulta parametros,
        CancellationToken cancelacion = default)
    {
        var pagina = await _proveedores.ListarAsync(parametros, cancelacion);

        // Una sola agregacion para toda la pagina en lugar de una consulta por proveedor.
        var conteos = await _proveedores.ContarOfertasAsync(
            pagina.Elementos.Select(p => p.Id),
            cancelacion);

        return new PaginaResultado<ProveedorDto>(
            pagina.Elementos.Select(p => Mapear(p, conteos)).ToArray(),
            pagina.Pagina,
            pagina.TamanoPagina,
            pagina.TotalElementos);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProveedorDto>> ListarActivosAsync(CancellationToken cancelacion = default)
    {
        var activos = await _proveedores.ListarActivosAsync(cancelacion);
        var conteos = await _proveedores.ContarOfertasAsync(activos.Select(p => p.Id), cancelacion);

        return activos.Select(p => Mapear(p, conteos)).ToArray();
    }

    /// <inheritdoc />
    public async Task<ProveedorDto> ObtenerAsync(Guid id, CancellationToken cancelacion = default)
    {
        Proveedor proveedor = await ObtenerOFallarAsync(id, cancelacion);
        var conteos = await _proveedores.ContarOfertasAsync([id], cancelacion);

        return Mapear(proveedor, conteos);
    }

    /// <inheritdoc />
    public async Task<ProveedorDto> CrearAsync(
        CrearProveedorRequest peticion,
        CancellationToken cancelacion = default)
    {
        await _validadorCrear.AsegurarValidoAsync(peticion, cancelacion);

        string nombreNormalizado = NormalizadorTexto.NormalizarNombre(peticion.Nombre);
        await AsegurarNombreDisponibleAsync(nombreNormalizado, idExcluido: null, cancelacion);

        Proveedor proveedor = Proveedor.Crear(peticion.Nombre, _reloj.AhoraUtc);

        _proveedores.Agregar(proveedor);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        return Mapear(proveedor, ConteoVacio);
    }

    /// <inheritdoc />
    public async Task<ProveedorDto> ActualizarAsync(
        Guid id,
        ActualizarProveedorRequest peticion,
        CancellationToken cancelacion = default)
    {
        await _validadorActualizar.AsegurarValidoAsync(peticion, cancelacion);

        Proveedor proveedor = await ObtenerOFallarAsync(id, cancelacion);

        string nombreNormalizado = NormalizadorTexto.NormalizarNombre(peticion.Nombre);
        await AsegurarNombreDisponibleAsync(nombreNormalizado, idExcluido: id, cancelacion);

        proveedor.CambiarNombre(peticion.Nombre, _reloj.AhoraUtc);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        var conteos = await _proveedores.ContarOfertasAsync([id], cancelacion);

        return Mapear(proveedor, conteos);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Seccion 8.9: un proveedor con ofertas no puede borrarse fisicamente porque destruiria
    /// la evidencia de esas ofertas. En ese caso se aplica borrado logico; si no tiene ofertas,
    /// el borrado fisico es seguro y mantiene la tabla limpia.
    /// </remarks>
    public async Task<bool> EliminarAsync(Guid id, CancellationToken cancelacion = default)
    {
        Proveedor proveedor = await ObtenerOFallarAsync(id, cancelacion);

        bool tieneOfertas = await _ofertas.ProveedorTieneOfertasAsync(id, cancelacion);

        if (tieneOfertas)
        {
            proveedor.EliminarLogicamente(_reloj.AhoraUtc);
        }
        else
        {
            _proveedores.Eliminar(proveedor);
        }

        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        return tieneOfertas;
    }

    private async Task<Proveedor> ObtenerOFallarAsync(Guid id, CancellationToken cancelacion)
    {
        return await _proveedores.ObtenerPorIdAsync(id, incluirEliminados: false, cancelacion)
            ?? throw new RecursoNoEncontradoException("Proveedor", id);
    }

    private async Task AsegurarNombreDisponibleAsync(
        string nombreNormalizado,
        Guid? idExcluido,
        CancellationToken cancelacion)
    {
        if (await _proveedores.ExisteNombreAsync(nombreNormalizado, idExcluido, cancelacion))
        {
            throw new ConflictoUnicidadException(
                nameof(CrearProveedorRequest.Nombre),
                "Ya existe un proveedor registrado con ese nombre.",
                CodigosError.NombreProveedorDuplicado);
        }
    }

    private static readonly IReadOnlyDictionary<Guid, int> ConteoVacio =
        new Dictionary<Guid, int>();

    private static ProveedorDto Mapear(
        Proveedor proveedor,
        IReadOnlyDictionary<Guid, int> conteoOfertas) => new(
        proveedor.Id,
        proveedor.Nombre,
        proveedor.NombreNormalizado,
        conteoOfertas.TryGetValue(proveedor.Id, out int cantidad) ? cantidad : 0,
        proveedor.EstaEliminado,
        proveedor.CreatedAt,
        proveedor.UpdatedAt);
}
