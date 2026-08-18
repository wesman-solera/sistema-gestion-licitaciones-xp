namespace Licitaciones.Domain.Constantes;

/// <summary>
/// Codigos estables de error del dominio.
/// </summary>
/// <remarks>
/// Se exponen al cliente dentro de la extension <c>codigoError</c> de ProblemDetails
/// (seccion 10.2 del enunciado). Son estables: la interfaz web y las pruebas funcionales
/// dependen de ellos, por lo que no deben renombrarse sin actualizar ambos consumidores.
/// </remarks>
public static class CodigosError
{
    /// <summary>El codigo de licitacion ya existe una vez normalizado.</summary>
    public const string CodigoLicitacionDuplicado = "LIC-001";

    /// <summary>La transicion de estado solicitada no esta permitida.</summary>
    public const string TransicionEstadoInvalida = "LIC-002";

    /// <summary>La fecha de cierre no es futura respecto del reloj del sistema.</summary>
    public const string FechaCierreNoFutura = "LIC-003";

    /// <summary>El presupuesto quedaria por debajo de una oferta ya registrada.</summary>
    public const string PresupuestoMenorQueOfertaExistente = "LIC-004";

    /// <summary>La licitacion no puede eliminarse porque conserva ofertas asociadas.</summary>
    public const string LicitacionConOfertas = "LIC-005";

    /// <summary>El nombre del proveedor ya existe una vez normalizado.</summary>
    public const string NombreProveedorDuplicado = "PRO-001";

    /// <summary>El nombre del proveedor contiene caracteres no permitidos.</summary>
    public const string CaracteresProveedorNoPermitidos = "PRO-002";

    /// <summary>El proveedor no puede eliminarse porque conserva ofertas asociadas.</summary>
    public const string ProveedorConOfertas = "PRO-003";

    /// <summary>El proveedor ya registro una oferta para esa misma licitacion.</summary>
    public const string OfertaDuplicada = "OFE-001";

    /// <summary>El monto ofertado supera el presupuesto estimado de la licitacion.</summary>
    public const string OfertaSuperaPresupuesto = "OFE-002";

    /// <summary>La licitacion ya alcanzo su fecha de cierre.</summary>
    public const string LicitacionVencida = "OFE-003";

    /// <summary>La licitacion no se encuentra publicada.</summary>
    public const string LicitacionNoPublicada = "OFE-004";

    /// <summary>La oferta pertenece a una licitacion cerrada y es inmutable.</summary>
    public const string OfertaInmutable = "OFE-005";

    /// <summary>Los rangos de aprobacion se traslapan entre si.</summary>
    public const string RangosAprobacionTraslapados = "APR-001";

    /// <summary>Ya existe un rango abierto sin monto maximo.</summary>
    public const string RangoAbiertoDuplicado = "APR-002";

    /// <summary>El monto minimo es mayor o igual que el monto maximo.</summary>
    public const string RangoAprobacionInvalido = "APR-003";

    /// <summary>Ningun rango parametrizado cubre el monto consultado.</summary>
    public const string SinNivelAprobacionAplicable = "APR-004";

    /// <summary>No existe un tipo de cambio activo para operar.</summary>
    public const string SinTipoCambioActivo = "TCB-001";

    /// <summary>El tipo de cambio debe ser estrictamente mayor que cero.</summary>
    public const string TipoCambioInvalido = "TCB-002";

    /// <summary>Un valor monetario es cero o negativo.</summary>
    public const string MontoNoPositivo = "GEN-001";

    /// <summary>El recurso solicitado no existe o fue eliminado logicamente.</summary>
    public const string RecursoNoEncontrado = "GEN-002";

    /// <summary>Otro usuario modifico el registro entre la lectura y la escritura.</summary>
    public const string ConflictoConcurrencia = "GEN-003";

    /// <summary>La solicitud no supero la validacion de datos de entrada.</summary>
    public const string ValidacionFallida = "GEN-004";
}
