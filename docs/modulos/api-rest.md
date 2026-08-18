# Módulo: API REST

## Propósito

Expone las operaciones del sistema por HTTP, con versionado, documentación interactiva y errores
uniformes. La referencia completa de endpoints está en [../api.md](../api.md); este documento
explica **cómo está construida**.

## Responsabilidades

- Publicar los endpoints bajo `/api/v1`
- Traducir peticiones HTTP a llamadas de los servicios de aplicación
- Traducir excepciones a códigos de estado y ProblemDetails
- Generar la documentación OpenAPI

## Lo que no hace

- No contiene reglas de negocio ni acceso a datos

---

## Controladores delgados

El requisito 6.4 pide *«controladores delgados; la lógica de negocio debe residir en servicios o
capas apropiadas»*. Un método típico:

```csharp
[HttpPost]
public async Task<ActionResult<LicitacionDetalleDto>> Crear(
    [FromBody] CrearLicitacionRequest peticion,
    CancellationToken cancelacion)
{
    LicitacionDetalleDto creada = await _servicio.CrearAsync(peticion, cancelacion);

    return CreatedAtRoute("ObtenerLicitacion", new { id = creada.Id, version = "1.0" }, creada);
}
```

Tres líneas: llamar al servicio y traducir el resultado.

**No hay bloques `try/catch`.** Todas las excepciones las traduce el manejador global, de modo que
la respuesta a un mismo error es idéntica venga del endpoint que venga.

---

## DTO, nunca entidades

La sección 10 prohíbe exponer directamente las entidades de Entity Framework Core. La razón no es
formal:

| Problema | Consecuencia |
|---|---|
| Ciclos de referencia | `Licitacion → Ofertas → Licitacion` haría fallar la serialización |
| Carga perezosa accidental | Serializar podría disparar consultas no previstas |
| Acoplamiento del contrato al esquema | Renombrar una columna rompería a todos los clientes |
| Exposición de campos internos | El token de concurrencia no le interesa al cliente |

Con DTO propios, el contrato es una decisión explícita.

---

## Versionado

```csharp
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/licitaciones")]
```

La versión viaja en la ruta. Se eligió sobre la alternativa de cabecera porque hace el contrato
visible en la propia dirección: `/api/v1/licitaciones` dice qué versión se está usando sin
inspeccionar nada más.

La respuesta incluye la cabecera `api-supported-versions`, de modo que el cliente descubre las
versiones disponibles sin leer la documentación.

---

## Documentación OpenAPI

Se genera con Swashbuckle y **se alimenta de los comentarios XML del propio código**:

```csharp
IncluirComentariosXml(opciones, Assembly.GetExecutingAssembly());
IncluirComentariosXml(opciones, typeof(Application.Dtos.MontoDto).Assembly);
```

Lo que se escribe una vez en el código aparece en Swagger. No hay una segunda copia de la
documentación que pueda quedar desactualizada.

`ConfiguracionSwagger` genera un documento por cada versión descubierta en tiempo de ejecución, en
lugar de declararlas a mano. Agregar una versión nueva no requiere tocar la configuración.

La ausencia del archivo XML no se trata como error: la API sigue levantando, solo que con menos
descripciones.

---

## Manejo de errores

`ManejadorExcepcionesGlobal` es el **único punto** donde una excepción se convierte en respuesta
HTTP.

### Por qué concentrarlo

| Alternativa | Problema |
|---|---|
| `try/catch` en cada controlador | Se repite en veinte métodos; olvidarlo en uno filtra el error crudo |
| Filtro de excepciones de MVC | No cubre los errores del enlace de modelo |
| Middleware propio | Reimplementaría lo que `IExceptionHandler` ya resuelve |

### Correspondencia

| Excepción | Estado | Razón |
|---|---|---|
| `ValidacionException` | 400 | La petición está mal formada |
| `RecursoNoEncontradoException` | 404 | El recurso no existe |
| `ConflictoUnicidadException` | 409 | El valor ya está en uso |
| `TransicionEstadoInvalidaException` | 409 | El estado actual no permite la operación |
| `DbUpdateConcurrencyException` | 409 | Otro usuario modificó el registro |
| `ExcepcionDominio` | 422 | Bien formada, pero incumple una regla |
| Violación de índice único | 409 | Con el código deducido del nombre del índice |
| Violación de clave foránea | 422 | Integridad referencial |
| Cualquier otra | 500 | Con mensaje genérico |

### Qué se expone y qué no

```csharp
Detail = estado >= StatusCodes.Status500InternalServerError
    ? "Ocurrio un error inesperado al procesar la solicitud. Intente de nuevo mas tarde."
    : exception.Message,
```

Los mensajes del dominio están escritos para el usuario final y se exponen. Ante un error inesperado
se devuelve un texto genérico y el detalle completo queda solo en el registro del servidor,
localizable por el identificador de correlación.

La prueba `LicitacionesEndpointsPruebas.ProblemDetails_NoExponeDetallesInternos` comprueba que el
cuerpo de una respuesta de error no contenga `Npgsql`, `SELECT`, rutas de código, `Password`,
`Host=` ni trazas de pila.

### Un solo formato de error

ASP.NET Core produce su propio formato cuando falla el enlace de modelo, distinto del que genera el
manejador global. Se sustituye para que toda la API responda igual:

```csharp
opciones.InvalidModelStateResponseFactory = contexto => { /* mismo formato */ };
```

Sin esto, un cliente tendría que manejar dos formas distintas de error según dónde fallara la
petición.

---

## Paginación

Todos los listados devuelven `PaginaResultado<T>` con elementos, página, tamaño, total, total de
páginas y las banderas de navegación.

El tamaño de página se acota entre 1 y 100. Sin ese límite superior, un cliente podría pedir el
listado completo en una sola petición y degradar la base de datos.

---

## Sondas de estado

| Ruta | Consulta la base | Uso |
|---|---|---|
| `/health` | Sí | Estado general |
| `/health/listo` | Sí | Sonda de disponibilidad de Kubernetes |
| `/health/vivo` | **No** | Sondas de arranque y de vida |

La separación es deliberada y está explicada en [../kubernetes.md](../kubernetes.md): si la sonda de
vida consultara la base de datos, una caída de PostgreSQL provocaría el reinicio en bucle de todos
los pods de la aplicación sin resolver nada.

---

## Dos endpoints para registrar una oferta

| Endpoint | Contrato | Cuándo usarlo |
|---|---|---|
| `POST /api/v1/licitaciones/{id}/ofertas` | `RegistrarOfertaEnLicitacionRequest` | Desde el contexto de una licitación |
| `POST /api/v1/ofertas` | `CrearOfertaRequest` | Cuando la licitación es un dato más |

Los contratos son distintos a propósito. En el primero la licitación viaja en la ruta; incluirla
también en el cuerpo permitiría enviar dos valores distintos y obligaría a decidir cuál gana.

---

## Pruebas

| Prueba | Verifica |
|---|---|
| `LicitacionesEndpointsPruebas.Post_CrearLicitacion_*` | 201 con cabecera `Location` |
| `LicitacionesEndpointsPruebas.Post_ConCodigoDuplicado_*` | 409 con código de error |
| `LicitacionesEndpointsPruebas.Post_ConPresupuestoNoPositivo_*` | 400 frente a 422 |
| `LicitacionesEndpointsPruebas.Get_Listado_*` | Estructura de paginación |
| `LicitacionesEndpointsPruebas.ProblemDetails_NoExponeDetalles*` | Seguridad de los mensajes |
| `LicitacionesEndpointsPruebas.FlujoCompleto_*` | Flujo del enunciado por API |
| `ProveedoresYTiposCambioEndpointsPruebas.Get_Swagger_*` | Documento OpenAPI completo |

---

## Archivos

| Archivo | Contenido |
|---|---|
| `Api/Controladores/*.cs` | Controladores REST |
| `Api/Comun/ManejadorExcepcionesGlobal.cs` | Traducción de excepciones |
| `Api/Comun/ParametrosConsultaApi.cs` | Enlace de los parámetros de consulta |
| `Api/Configuracion/ConfiguracionSwagger.cs` | Documento OpenAPI por versión |
| `Api/RegistroServiciosApi.cs` | Versionado, Swagger y manejo de errores |
