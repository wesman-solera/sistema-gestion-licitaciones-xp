using Licitaciones.Application.Comun;
using Licitaciones.Application.Dtos;

namespace Licitaciones.Application.Servicios;

/// <summary>Casos de uso del modulo de proveedores.</summary>
public interface IProveedorServicio
{
    /// <summary>Devuelve una pagina de proveedores.</summary>
    /// <param name="parametros">Parametros de paginacion, filtrado y ordenamiento.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>La pagina de resultados.</returns>
    Task<PaginaResultado<ProveedorDto>> ListarAsync(
        ParametrosConsulta parametros,
        CancellationToken cancelacion = default);

    /// <summary>Devuelve todos los proveedores activos para poblar listas de seleccion.</summary>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>Proveedores activos ordenados por nombre.</returns>
    Task<IReadOnlyList<ProveedorDto>> ListarActivosAsync(CancellationToken cancelacion = default);

    /// <summary>Consulta un proveedor por identificador.</summary>
    /// <param name="id">Identificador del proveedor.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El proveedor solicitado.</returns>
    Task<ProveedorDto> ObtenerAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Crea un proveedor.</summary>
    /// <param name="peticion">Datos del proveedor.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El proveedor creado.</returns>
    Task<ProveedorDto> CrearAsync(CrearProveedorRequest peticion, CancellationToken cancelacion = default);

    /// <summary>Modifica un proveedor.</summary>
    /// <param name="id">Identificador del proveedor.</param>
    /// <param name="peticion">Nuevos datos.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns>El proveedor actualizado.</returns>
    Task<ProveedorDto> ActualizarAsync(
        Guid id,
        ActualizarProveedorRequest peticion,
        CancellationToken cancelacion = default);

    /// <summary>Elimina un proveedor de forma fisica o logica segun tenga ofertas asociadas.</summary>
    /// <param name="id">Identificador del proveedor.</param>
    /// <param name="cancelacion">Token de cancelacion de la peticion.</param>
    /// <returns><c>true</c> si el borrado fue logico, <c>false</c> si fue fisico.</returns>
    Task<bool> EliminarAsync(Guid id, CancellationToken cancelacion = default);
}
