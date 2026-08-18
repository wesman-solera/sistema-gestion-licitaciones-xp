using System.Diagnostics;
using Licitaciones.Application.Excepciones;
using Licitaciones.Domain.Constantes;
using Licitaciones.Domain.Excepciones;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Licitaciones.Api.Comun;

/// <summary>
/// Traduce cualquier excepcion no controlada a una respuesta ProblemDetails segura.
/// </summary>
/// <remarks>
/// Es el unico punto donde una excepcion se convierte en respuesta HTTP. Concentrarlo aqui evita
/// que cada controlador tenga su propio <c>try/catch</c> y garantiza que ningun detalle interno
/// se filtre al cliente: la seccion 10.2 prohibe exponer trazas de pila, rutas internas,
/// consultas y secretos. El detalle tecnico completo se registra en el log del servidor, y al
/// cliente se le entrega un identificador de correlacion con el que se puede ubicar ese registro.
/// </remarks>
public sealed class ManejadorExcepcionesGlobal : IExceptionHandler
{
    private readonly ILogger<ManejadorExcepcionesGlobal> _registrador;
    private readonly IProblemDetailsService _servicioProblemDetails;

    /// <summary>Codigo SQLSTATE de PostgreSQL para violacion de restriccion unica.</summary>
    private const string SqlStateViolacionUnica = "23505";

    /// <summary>Codigo SQLSTATE de PostgreSQL para violacion de clave foranea.</summary>
    private const string SqlStateViolacionForanea = "23503";

    /// <summary>Codigo SQLSTATE de PostgreSQL para violacion de restriccion CHECK.</summary>
    private const string SqlStateViolacionCheck = "23514";

    /// <summary>Codigo no estandar usado cuando el cliente aborta la peticion.</summary>
    private const int EstadoClienteCerroConexion = 499;

    /// <summary>Inicializa el manejador.</summary>
    /// <param name="registrador">Registrador de eventos.</param>
    /// <param name="servicioProblemDetails">Servicio que escribe la respuesta ProblemDetails.</param>
    public ManejadorExcepcionesGlobal(
        ILogger<ManejadorExcepcionesGlobal> registrador,
        IProblemDetailsService servicioProblemDetails)
    {
        _registrador = registrador;
        _servicioProblemDetails = servicioProblemDetails;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        string correlacion = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        (int estado, string titulo, string codigoError, IDictionary<string, string[]>? errores) =
            Traducir(exception);

        if (estado >= StatusCodes.Status500InternalServerError)
        {
            _registrador.LogError(
                exception,
                "Error no controlado en {Metodo} {Ruta}. Correlacion: {Correlacion}.",
                httpContext.Request.Method,
                httpContext.Request.Path,
                correlacion);
        }
        else
        {
            _registrador.LogInformation(
                "Peticion rechazada en {Metodo} {Ruta} con estado {Estado} y codigo {Codigo}.",
                httpContext.Request.Method,
                httpContext.Request.Path,
                estado,
                codigoError);
        }

        httpContext.Response.StatusCode = estado;

        var problema = new ProblemDetails
        {
            Status = estado,
            Title = titulo,
            // Solo se expone el mensaje cuando proviene del dominio, donde esta redactado para
            // el usuario final. Ante un error inesperado se devuelve un texto generico.
            Detail = estado >= StatusCodes.Status500InternalServerError
                ? "Ocurrio un error inesperado al procesar la solicitud. Intente de nuevo mas tarde."
                : exception.Message,
            Instance = httpContext.Request.Path
        };

        problema.Extensions["codigoError"] = codigoError;
        problema.Extensions["correlacion"] = correlacion;

        if (errores is not null)
        {
            problema.Extensions["errores"] = errores;
        }

        return await _servicioProblemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problema,
            Exception = exception
        });
    }

    /// <summary>Determina el estado HTTP, el titulo y el codigo de error de una excepcion.</summary>
    /// <param name="excepcion">Excepcion capturada.</param>
    /// <returns>Tupla con el estado, el titulo, el codigo y los errores de campo si los hay.</returns>
    private static (int Estado, string Titulo, string Codigo, IDictionary<string, string[]>? Errores)
        Traducir(Exception excepcion) => excepcion switch
    {
        ValidacionException validacion => (
            StatusCodes.Status400BadRequest,
            "Datos de entrada invalidos",
            CodigosError.ValidacionFallida,
            validacion.Errores.ToDictionary(e => e.Key, e => e.Value)),

        RecursoNoEncontradoException noEncontrado => (
            StatusCodes.Status404NotFound,
            "Recurso no encontrado",
            noEncontrado.CodigoError,
            null),

        ConflictoUnicidadException conflicto => (
            StatusCodes.Status409Conflict,
            "Conflicto de unicidad",
            conflicto.CodigoError,
            new Dictionary<string, string[]> { [conflicto.Campo] = [conflicto.Message] }),

        TransicionEstadoInvalidaException transicion => (
            StatusCodes.Status409Conflict,
            "Transicion de estado no permitida",
            transicion.CodigoError,
            null),

        // Toda otra violacion de regla de negocio es semanticamente correcta pero imposible de
        // procesar, que es exactamente lo que significa 422.
        ExcepcionDominio dominio => (
            StatusCodes.Status422UnprocessableEntity,
            "Regla de negocio incumplida",
            dominio.CodigoError,
            null),

        DbUpdateConcurrencyException => (
            StatusCodes.Status409Conflict,
            "Conflicto de concurrencia",
            CodigosError.ConflictoConcurrencia,
            null),

        DbUpdateException actualizacion => TraducirErrorBaseDatos(actualizacion),

        // 499 no esta en la lista de StatusCodes de ASP.NET Core; es la convencion de nginx
        // para "el cliente cerro la conexion", que es exactamente lo que ocurrio.
        OperationCanceledException => (
            EstadoClienteCerroConexion,
            "Solicitud cancelada",
            CodigosError.ValidacionFallida,
            null),

        _ => (
            StatusCodes.Status500InternalServerError,
            "Error interno del servidor",
            "GEN-500",
            null)
    };

    /// <summary>
    /// Traduce un error de PostgreSQL a un mensaje controlado.
    /// </summary>
    /// <param name="excepcion">Excepcion de actualizacion de Entity Framework Core.</param>
    /// <returns>Tupla con el estado, el titulo, el codigo y los errores de campo si los hay.</returns>
    /// <remarks>
    /// Los indices unicos y las restricciones CHECK son la ultima linea de defensa: cuando se
    /// disparan significa que dos peticiones concurrentes superaron la validacion de aplicacion.
    /// El mensaje del motor nunca llega al cliente porque contiene nombres de tabla y de indice.
    /// </remarks>
    private static (int, string, string, IDictionary<string, string[]>?) TraducirErrorBaseDatos(
        DbUpdateException excepcion)
    {
        if (excepcion.InnerException is not PostgresException postgres)
        {
            return (
                StatusCodes.Status500InternalServerError,
                "Error al guardar los datos",
                "GEN-500",
                null);
        }

        return postgres.SqlState switch
        {
            SqlStateViolacionUnica => (
                StatusCodes.Status409Conflict,
                "Conflicto de unicidad",
                DeducirCodigoUnicidad(postgres.ConstraintName),
                null),

            SqlStateViolacionForanea => (
                StatusCodes.Status422UnprocessableEntity,
                "Integridad referencial",
                CodigosError.RecursoNoEncontrado,
                null),

            SqlStateViolacionCheck => (
                StatusCodes.Status422UnprocessableEntity,
                "Regla de negocio incumplida",
                CodigosError.MontoNoPositivo,
                null),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Error al guardar los datos",
                "GEN-500",
                null)
        };
    }

    /// <summary>Deduce el codigo de error a partir del nombre del indice unico violado.</summary>
    /// <param name="restriccion">Nombre de la restriccion informado por PostgreSQL.</param>
    /// <returns>El codigo estable correspondiente.</returns>
    private static string DeducirCodigoUnicidad(string? restriccion) => restriccion switch
    {
        "ux_licitaciones_codigo_normalizado" => CodigosError.CodigoLicitacionDuplicado,
        "ux_proveedores_nombre_normalizado" => CodigosError.NombreProveedorDuplicado,
        "ux_ofertas_licitacion_proveedor" => CodigosError.OfertaDuplicada,
        "ux_tipos_cambio_unico_activo" => CodigosError.SinTipoCambioActivo,
        "ux_niveles_aprobacion_monto_minimo" => CodigosError.RangosAprobacionTraslapados,
        _ => "GEN-409"
    };
}
