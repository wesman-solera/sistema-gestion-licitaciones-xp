using Licitaciones.Application.Excepciones;
using Licitaciones.Domain.Excepciones;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.Controladores;

/// <summary>
/// Base de los controladores MVC con las utilidades de mensajes y de traduccion de errores.
/// </summary>
/// <remarks>
/// La interfaz web reutiliza los mismos servicios de aplicacion que la API. Cuando una regla de
/// negocio falla, el servicio lanza una excepcion; aqui se traduce a un mensaje junto al campo
/// correspondiente en lugar de dejar que llegue a la pagina de error. Concentrarlo en la clase
/// base evita repetir el mismo <c>try/catch</c> en los cinco controladores.
/// </remarks>
public abstract class ControladorBase : Controller
{
    /// <summary>Clave de TempData donde viaja el mensaje de exito entre peticiones.</summary>
    public const string ClaveMensajeExito = "MensajeExito";

    /// <summary>Clave de TempData donde viaja el mensaje de advertencia entre peticiones.</summary>
    public const string ClaveMensajeAdvertencia = "MensajeAdvertencia";

    /// <summary>Clave de TempData donde viaja el mensaje de error entre peticiones.</summary>
    public const string ClaveMensajeError = "MensajeError";

    /// <summary>Registra un mensaje de exito que se mostrara tras la redireccion.</summary>
    /// <param name="mensaje">Texto para el usuario.</param>
    protected void AvisarExito(string mensaje) => TempData[ClaveMensajeExito] = mensaje;

    /// <summary>Registra un mensaje de advertencia que se mostrara tras la redireccion.</summary>
    /// <param name="mensaje">Texto para el usuario.</param>
    protected void AvisarAdvertencia(string mensaje) => TempData[ClaveMensajeAdvertencia] = mensaje;

    /// <summary>Registra un mensaje de error que se mostrara tras la redireccion.</summary>
    /// <param name="mensaje">Texto para el usuario.</param>
    protected void AvisarError(string mensaje) => TempData[ClaveMensajeError] = mensaje;

    /// <summary>
    /// Ejecuta una operacion de escritura y traduce sus errores de negocio al estado del modelo.
    /// </summary>
    /// <param name="operacion">Operacion a ejecutar.</param>
    /// <param name="campoPorDefecto">
    /// Campo del formulario al que se asocia el error cuando la excepcion no indica uno.
    /// Si se deja vacio, el mensaje aparece en el resumen general del formulario.
    /// </param>
    /// <returns><c>true</c> si la operacion se completo sin errores de negocio.</returns>
    /// <remarks>
    /// Solo se capturan las excepciones previstas del dominio y de la validacion. Cualquier otra
    /// se deja propagar para que la maneje la pagina de error: ocultarla aqui convertiria un
    /// fallo real en un mensaje de formulario enganoso.
    /// </remarks>
    protected async Task<bool> EjecutarAsync(Func<Task> operacion, string campoPorDefecto = "")
    {
        ArgumentNullException.ThrowIfNull(operacion);

        try
        {
            await operacion();

            return true;
        }
        catch (ValidacionException validacion)
        {
            foreach (var error in validacion.Errores)
            {
                foreach (string mensaje in error.Value)
                {
                    ModelState.AddModelError(error.Key, mensaje);
                }
            }

            return false;
        }
        catch (ConflictoUnicidadException conflicto)
        {
            ModelState.AddModelError(conflicto.Campo, conflicto.Message);

            return false;
        }
        catch (ExcepcionDominio dominio)
        {
            ModelState.AddModelError(campoPorDefecto, dominio.Message);

            return false;
        }
    }
}
