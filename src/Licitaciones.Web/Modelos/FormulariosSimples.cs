using System.ComponentModel.DataAnnotations;
using Licitaciones.Application.Dtos;
using Licitaciones.Domain.Servicios;
using Licitaciones.Web.Servicios;

namespace Licitaciones.Web.Modelos;

/// <summary>Modelo del formulario de creacion y edicion de proveedores.</summary>
public sealed class ProveedorFormulario
{
    /// <summary>Identificador del proveedor. Es vacio al crear.</summary>
    public Guid Id { get; set; }

    /// <summary>Nombre de la empresa o persona oferente.</summary>
    /// <remarks>
    /// La expresion regular es la misma que define la seccion 8.4 del enunciado y que aplica el
    /// dominio. Se repite aqui para que el navegador rechace el caracter invalido antes de
    /// enviar, no para sustituir la validacion del servidor.
    /// </remarks>
    [Required(ErrorMessage = "El nombre del proveedor es obligatorio.")]
    [StringLength(200, ErrorMessage = "El nombre no puede superar 200 caracteres.")]
    [RegularExpression(
        NormalizadorTexto.PatronNombreProveedor,
        ErrorMessage = "El nombre solo admite letras, numeros, espacios, punto, coma y parentesis.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Indica si el formulario corresponde a una edicion.</summary>
    public bool EsEdicion => Id != Guid.Empty;

    /// <summary>Construye el modelo a partir del proveedor devuelto por la API.</summary>
    /// <param name="dto">Proveedor consultado.</param>
    /// <returns>El formulario poblado.</returns>
    public static ProveedorFormulario Desde(ProveedorDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new ProveedorFormulario { Id = dto.Id, Nombre = dto.Nombre };
    }
}

/// <summary>Modelo del formulario de registro y edicion de ofertas.</summary>
public sealed class OfertaFormulario
{
    /// <summary>Identificador de la oferta. Es vacio al registrar.</summary>
    public Guid Id { get; set; }

    /// <summary>Licitacion a la que se oferta.</summary>
    [Required(ErrorMessage = "Debe seleccionar una licitacion.")]
    [Display(Name = "Licitacion")]
    public Guid LicitacionId { get; set; }

    /// <summary>Proveedor que presenta la oferta.</summary>
    [Required(ErrorMessage = "Debe seleccionar un proveedor.")]
    [Display(Name = "Proveedor")]
    public Guid ProveedorId { get; set; }

    /// <summary>Monto ofertado en colones.</summary>
    [Required(ErrorMessage = "El monto ofertado es obligatorio.")]
    [Range(0.01, 9_999_999_999_999_999.99, ErrorMessage = "El monto debe ser mayor que cero.")]
    [Display(Name = "Monto ofertado (CRC)")]
    public decimal MontoOfertadoCrc { get; set; }

    /// <summary>Indica si el formulario corresponde a una edicion.</summary>
    public bool EsEdicion => Id != Guid.Empty;

    /// <summary>Licitaciones disponibles para seleccionar.</summary>
    /// <remarks>Solo se ofrecen las que pueden recibir ofertas: publicadas y no vencidas.</remarks>
    public IReadOnlyList<LicitacionResumenDto> LicitacionesDisponibles { get; set; } = [];

    /// <summary>Proveedores disponibles para seleccionar.</summary>
    public IReadOnlyList<ProveedorDto> ProveedoresDisponibles { get; set; } = [];

    /// <summary>Presupuesto de la licitacion seleccionada, para mostrarlo como referencia.</summary>
    public decimal? PresupuestoReferencia { get; set; }
}

/// <summary>Modelo del formulario de creacion y edicion de rangos de aprobacion.</summary>
public sealed class NivelAprobacionFormulario
{
    /// <summary>Identificador del rango. Es vacio al crear.</summary>
    public Guid Id { get; set; }

    /// <summary>Monto minimo inclusivo, en colones.</summary>
    [Required(ErrorMessage = "El monto minimo es obligatorio.")]
    [Range(0.01, 9_999_999_999_999_999.99, ErrorMessage = "El monto minimo debe ser mayor que cero.")]
    [Display(Name = "Monto minimo (CRC)")]
    public decimal MontoMinimoCrc { get; set; }

    /// <summary>Monto maximo inclusivo, o vacio para el rango abierto.</summary>
    [Range(0.01, 9_999_999_999_999_999.99, ErrorMessage = "El monto maximo debe ser mayor que cero.")]
    [Display(Name = "Monto maximo (CRC)")]
    public decimal? MontoMaximoCrc { get; set; }

    /// <summary>Cargo o instancia responsable de aprobar.</summary>
    [Required(ErrorMessage = "El aprobador es obligatorio.")]
    [StringLength(150, ErrorMessage = "El aprobador no puede superar 150 caracteres.")]
    [Display(Name = "Aprobador")]
    public string Aprobador { get; set; } = string.Empty;

    /// <summary>Indica si el formulario corresponde a una edicion.</summary>
    public bool EsEdicion => Id != Guid.Empty;

    /// <summary>Construye el modelo a partir del rango devuelto por la API.</summary>
    /// <param name="dto">Rango consultado.</param>
    /// <returns>El formulario poblado.</returns>
    public static NivelAprobacionFormulario Desde(NivelAprobacionDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new NivelAprobacionFormulario
        {
            Id = dto.Id,
            MontoMinimoCrc = dto.MontoMinimo.Crc,
            MontoMaximoCrc = dto.MontoMaximo?.Crc,
            Aprobador = dto.Aprobador
        };
    }
}

/// <summary>Modelo del formulario de creacion y edicion de tipos de cambio.</summary>
public sealed class TipoCambioFormulario
{
    /// <summary>Identificador del tipo de cambio. Es vacio al crear.</summary>
    public Guid Id { get; set; }

    /// <summary>Colones que equivalen a un dolar.</summary>
    [Required(ErrorMessage = "El tipo de cambio es obligatorio.")]
    [Range(0.01, 9_999_999_999_999_999.99, ErrorMessage = "El tipo de cambio debe ser mayor que cero.")]
    [Display(Name = "Colones por dolar")]
    public decimal CrcPorUsd { get; set; }

    /// <summary>Fecha desde la que rige, en hora de Costa Rica.</summary>
    [Required(ErrorMessage = "La fecha de vigencia es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de vigencia")]
    public DateTime FechaVigencia { get; set; } = DateTime.Today;

    /// <summary>Indica si debe quedar activo de inmediato.</summary>
    [Display(Name = "Marcar como activo")]
    public bool Activo { get; set; }

    /// <summary>Indica si el formulario corresponde a una edicion.</summary>
    public bool EsEdicion => Id != Guid.Empty;

    /// <summary>Construye el modelo a partir del tipo de cambio devuelto por la API.</summary>
    /// <param name="dto">Tipo de cambio consultado.</param>
    /// <returns>El formulario poblado.</returns>
    public static TipoCambioFormulario Desde(TipoCambioDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new TipoCambioFormulario
        {
            Id = dto.Id,
            CrcPorUsd = dto.CrcPorUsd,
            FechaVigencia = FormateadorFecha.AHoraLocal(dto.FechaVigencia).Date,
            Activo = dto.Activo
        };
    }

    /// <summary>Convierte la fecha del formulario en un instante UTC.</summary>
    /// <returns>La fecha de vigencia expresada en UTC.</returns>
    public DateTimeOffset FechaVigenciaUtc()
        => FormateadorFecha.DesdeControlCalendario(FechaVigencia);
}
