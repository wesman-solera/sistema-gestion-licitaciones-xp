using System.ComponentModel.DataAnnotations;
using Licitaciones.Application.Dtos;
using Licitaciones.Web.Servicios;

namespace Licitaciones.Web.Modelos;

/// <summary>
/// Modelo del formulario de creacion y edicion de licitaciones.
/// </summary>
/// <remarks>
/// Las anotaciones de datos cubren la validacion inmediata del navegador y la primera pasada del
/// servidor. No sustituyen a las reglas de negocio: el dominio vuelve a validar todo y PostgreSQL
/// tiene sus propias restricciones. Es la validacion en tres capas que pide la seccion 8.3.
/// <para>
/// La fecha se declara como <see cref="DateTime"/> y no como <see cref="DateTimeOffset"/> porque
/// el control <c>datetime-local</c> del navegador envia la hora local sin desplazamiento. La
/// conversion a UTC ocurre en <see cref="FormateadorFecha.DesdeControlCalendario"/>.
/// </para>
/// </remarks>
public sealed class LicitacionFormulario
{
    /// <summary>Identificador de la licitacion. Es vacio al crear.</summary>
    public Guid Id { get; set; }

    /// <summary>Codigo unico de la licitacion.</summary>
    [Required(ErrorMessage = "El codigo de la licitacion es obligatorio.")]
    [StringLength(50, ErrorMessage = "El codigo no puede superar 50 caracteres.")]
    [Display(Name = "Codigo")]
    public string Codigo { get; set; } = string.Empty;

    /// <summary>Titulo descriptivo.</summary>
    [Required(ErrorMessage = "El titulo de la licitacion es obligatorio.")]
    [StringLength(300, ErrorMessage = "El titulo no puede superar 300 caracteres.")]
    [Display(Name = "Titulo")]
    public string Titulo { get; set; } = string.Empty;

    /// <summary>Presupuesto estimado en colones.</summary>
    [Required(ErrorMessage = "El presupuesto estimado es obligatorio.")]
    [Range(0.01, 9_999_999_999_999_999.99, ErrorMessage = "El presupuesto debe ser mayor que cero.")]
    [Display(Name = "Presupuesto estimado (CRC)")]
    public decimal PresupuestoEstimadoCrc { get; set; }

    /// <summary>Fecha y hora de cierre en hora de Costa Rica.</summary>
    [Required(ErrorMessage = "La fecha y hora de cierre es obligatoria.")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Fecha y hora de cierre")]
    public DateTime FechaCierre { get; set; }

    /// <summary>Indica si el formulario corresponde a una edicion.</summary>
    public bool EsEdicion => Id != Guid.Empty;

    /// <summary>Indica si el codigo puede editarse. Solo es posible en estado Borrador.</summary>
    public bool CodigoEditable { get; set; } = true;

    /// <summary>Construye el modelo a partir del detalle devuelto por la API.</summary>
    /// <param name="detalle">Detalle de la licitacion.</param>
    /// <returns>El formulario poblado con los datos actuales.</returns>
    public static LicitacionFormulario Desde(LicitacionDetalleDto detalle)
    {
        ArgumentNullException.ThrowIfNull(detalle);

        return new LicitacionFormulario
        {
            Id = detalle.Id,
            Codigo = detalle.Codigo,
            Titulo = detalle.Titulo,
            PresupuestoEstimadoCrc = detalle.PresupuestoEstimado.Crc,
            FechaCierre = FormateadorFecha.AHoraLocal(detalle.FechaCierre).DateTime,
            CodigoEditable = detalle.Estado == Domain.Enums.EstadoLicitacion.Borrador
        };
    }

    /// <summary>Convierte el formulario en la peticion de creacion de la API.</summary>
    /// <returns>La peticion lista para enviarse al servicio de aplicacion.</returns>
    public CrearLicitacionRequest ACrearRequest() => new(
        Codigo,
        Titulo,
        PresupuestoEstimadoCrc,
        FormateadorFecha.DesdeControlCalendario(FechaCierre));

    /// <summary>Convierte el formulario en la peticion de modificacion de la API.</summary>
    /// <returns>La peticion lista para enviarse al servicio de aplicacion.</returns>
    public ActualizarLicitacionRequest AActualizarRequest() => new(
        Codigo,
        Titulo,
        PresupuestoEstimadoCrc,
        FormateadorFecha.DesdeControlCalendario(FechaCierre));
}
