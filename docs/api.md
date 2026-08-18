# API REST

## Generalidades

| Aspecto | Valor |
|---|---|
| Base | `/api/v1` |
| Formato | `application/json` |
| Errores | `application/problem+json` (RFC 7807) |
| Versionado | Por ruta, con cabeceras `api-supported-versions` en la respuesta |
| Documentación interactiva | `/swagger` |
| Documento OpenAPI | `/swagger/v1/swagger.json` |
| Moneda oficial | CRC. El valor en USD acompaña cada monto y es calculado |

La API **no expone entidades de Entity Framework Core**. Todos los contratos son DTO propios, tal
como exige la sección 10 del enunciado.

---

## Endpoints

### Licitaciones

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/v1/licitaciones` | Lista con paginación, filtro y orden |
| `GET` | `/api/v1/licitaciones/{id}` | Detalle con evaluación de ofertas |
| `POST` | `/api/v1/licitaciones` | Crea en estado Borrador |
| `PUT` | `/api/v1/licitaciones/{id}` | Modifica |
| `PATCH` | `/api/v1/licitaciones/{id}/estado` | Aplica una transición de estado |
| `DELETE` | `/api/v1/licitaciones/{id}` | Elimina (físico o lógico según tenga ofertas) |
| `GET` | `/api/v1/licitaciones/{id}/ofertas` | Ofertas de la licitación |
| `POST` | `/api/v1/licitaciones/{id}/ofertas` | Registra una oferta |
| `GET` | `/api/v1/licitaciones/{id}/mejor-oferta` | Mejor oferta, ahorro, clasificación y aprobador |

### Proveedores

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/v1/proveedores` | Lista con paginación y filtro |
| `GET` | `/api/v1/proveedores/{id}` | Detalle |
| `GET` | `/api/v1/proveedores/{id}/ofertas` | Ofertas del proveedor |
| `POST` | `/api/v1/proveedores` | Crea |
| `PUT` | `/api/v1/proveedores/{id}` | Modifica |
| `DELETE` | `/api/v1/proveedores/{id}` | Elimina (físico o lógico según tenga ofertas) |

### Ofertas

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/v1/ofertas` | Lista, con filtros por licitación y proveedor |
| `GET` | `/api/v1/ofertas/{id}` | Detalle |
| `POST` | `/api/v1/ofertas` | Registra indicando la licitación en el cuerpo |
| `PUT` | `/api/v1/ofertas/{id}` | Modifica el monto |
| `DELETE` | `/api/v1/ofertas/{id}` | Elimina, si la licitación sigue abierta |

### Niveles de aprobación

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/v1/niveles-aprobacion` | Lista ordenada por monto mínimo |
| `GET` | `/api/v1/niveles-aprobacion/{id}` | Detalle |
| `GET` | `/api/v1/niveles-aprobacion/aplicable?montoCrc=…` | Aprobador que corresponde a un monto |
| `POST` | `/api/v1/niveles-aprobacion` | Crea un rango |
| `PUT` | `/api/v1/niveles-aprobacion/{id}` | Modifica un rango |
| `DELETE` | `/api/v1/niveles-aprobacion/{id}` | Elimina un rango |

### Tipos de cambio

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/v1/tipos-cambio` | Lista |
| `GET` | `/api/v1/tipos-cambio/activo` | Tipo de cambio en uso |
| `GET` | `/api/v1/tipos-cambio/{id}` | Detalle |
| `POST` | `/api/v1/tipos-cambio` | Crea |
| `PUT` | `/api/v1/tipos-cambio/{id}` | Modifica |
| `PATCH` | `/api/v1/tipos-cambio/{id}/activar` | Activa y desactiva el anterior |
| `DELETE` | `/api/v1/tipos-cambio/{id}` | Elimina, si no es el activo |

### Operación

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/health` | Estado general |
| `GET` | `/health/listo` | Disponibilidad, incluye la base de datos |
| `GET` | `/health/vivo` | Vida del proceso, sin tocar la base de datos |

---

## Parámetros de consulta de los listados

| Parámetro | Tipo | Por defecto | Descripción |
|---|---|---|---|
| `pagina` | entero | 1 | Número de página, empezando en 1 |
| `tamanoPagina` | entero | 20 | Elementos por página; se acota a 100 |
| `buscar` | texto | — | Búsqueda libre sobre los campos descriptivos |
| `ordenarPor` | texto | — | Campo de ordenamiento; cada módulo acepta una lista cerrada |
| `descendente` | booleano | `false` | Dirección del orden |
| `incluirEliminados` | booleano | `false` | Incluye los registros con borrado lógico |

El tamaño de página se acota por arriba a propósito: sin ese límite, un cliente podría pedir el
listado completo en una sola petición y degradar la base de datos.

### Respuesta de un listado

```json
{
  "elementos": [ ],
  "pagina": 1,
  "tamanoPagina": 20,
  "totalElementos": 42,
  "totalPaginas": 3,
  "tieneAnterior": false,
  "tieneSiguiente": true
}
```

---

## Ejemplos

### Crear una licitación

```http
POST /api/v1/licitaciones
Content-Type: application/json

{
  "codigo": "LIC-2026-001",
  "titulo": "Compra de equipo de cómputo",
  "presupuestoEstimadoCrc": 12000000.00,
  "fechaCierre": "2026-09-30T23:00:00Z"
}
```

```http
HTTP/1.1 201 Created
Location: /api/v1/licitaciones/019213a4-...

{
  "id": "019213a4-...",
  "codigo": "LIC-2026-001",
  "titulo": "Compra de equipo de cómputo",
  "estado": 0,
  "estadoEfectivo": 0,
  "fechaCierre": "2026-09-30T23:00:00+00:00",
  "presupuestoEstimado": { "crc": 12000000.00, "usd": 23762.38 },
  "eliminada": false,
  "transicionesDisponibles": [1, 2],
  "evaluacion": {
    "mejorOferta": null,
    "porcentajeAhorro": null,
    "clasificacion": 0,
    "etiquetaClasificacion": "Sin ofertas validas",
    "cantidadOfertas": 0,
    "aprobador": null,
    "nivelAprobacionId": null
  },
  "ofertas": [],
  "tipoCambioAplicado": { "crcPorUsd": 505.00, "fechaVigencia": "2026-08-18T00:00:00+00:00" }
}
```

### Publicar una licitación

```http
PATCH /api/v1/licitaciones/{id}/estado
Content-Type: application/json

{ "estado": 1 }
```

### Registrar una oferta

```http
POST /api/v1/licitaciones/{id}/ofertas
Content-Type: application/json

{
  "proveedorId": "019213a5-...",
  "montoOfertadoCrc": 9500000.00
}
```

### Consultar la mejor oferta

```http
GET /api/v1/licitaciones/{id}/mejor-oferta
```

```json
{
  "mejorOferta": {
    "id": "019213a6-...",
    "licitacionId": "019213a4-...",
    "codigoLicitacion": "LIC-2026-001",
    "proveedorId": "019213a5-...",
    "nombreProveedor": "Distribuidora del Norte",
    "monto": { "crc": 9500000.00, "usd": 18811.88 },
    "fechaRegistro": "2026-08-20T15:42:10+00:00",
    "updatedAt": "2026-08-20T15:42:10+00:00",
    "esMejorOferta": true
  },
  "porcentajeAhorro": 20.83,
  "clasificacion": 2,
  "etiquetaClasificacion": "Oferta conveniente",
  "cantidadOfertas": 3,
  "aprobador": "Gerencia",
  "nivelAprobacionId": "a1000000-0000-4000-8000-000000000002"
}
```

---

## Códigos de estado

| Código | Cuándo se devuelve |
|---|---|
| `200 OK` | Consulta o modificación exitosa |
| `201 Created` | Recurso creado; la cabecera `Location` apunta a él |
| `204 No Content` | Eliminación exitosa |
| `400 Bad Request` | Los datos de entrada no superaron la validación de formato |
| `404 Not Found` | El recurso no existe o fue eliminado lógicamente |
| `409 Conflict` | Unicidad, transición de estado no permitida o conflicto de concurrencia |
| `422 Unprocessable Entity` | Datos bien formados que incumplen una regla de negocio |
| `500 Internal Server Error` | Error inesperado, con respuesta controlada |

### La distinción entre 400 y 422

No es arbitraria y conviene tenerla clara al leer la implementación:

- **400** — la petición está mal formada. Falta un campo obligatorio, el monto tiene tres decimales,
  el texto excede el largo máximo. El cliente puede corregirlo mirando solo su propia petición.
- **422** — la petición está bien formada, pero el estado del sistema no permite procesarla. La
  oferta supera el presupuesto, la licitación ya venció, el rango se traslapa con otro. El cliente
  no podía saberlo sin consultar el sistema.

---

## Formato de error

Todas las respuestas de error siguen RFC 7807 con dos extensiones propias.

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflicto de unicidad",
  "status": 409,
  "detail": "Ya existe una licitacion registrada con ese codigo.",
  "instance": "/api/v1/licitaciones",
  "codigoError": "LIC-001",
  "correlacion": "00-8f3c2b1a9d4e5f60-1a2b3c4d5e6f7081-01",
  "errores": {
    "Codigo": ["Ya existe una licitacion registrada con ese codigo."]
  }
}
```

| Campo | Descripción |
|---|---|
| `codigoError` | Código estable definido en `CodigosError`. No cambia entre versiones |
| `correlacion` | Identificador con el que ubicar el registro completo en el servidor |
| `errores` | Presente cuando el error se puede atribuir a campos concretos |

**Ningún mensaje expone** trazas de pila, rutas del sistema de archivos, consultas SQL, nombres de
tabla o índice, ni cadenas de conexión. La prueba
`LicitacionesEndpointsPruebas.ProblemDetails_NoExponeDetallesInternos` lo verifica de forma
automática.

---

## Catálogo de códigos de error

### Licitaciones

| Código | Estado | Significado |
|---|---|---|
| `LIC-001` | 409 | Código de licitación duplicado |
| `LIC-002` | 409 | Transición de estado no permitida |
| `LIC-003` | 422 | La fecha de cierre no es futura |
| `LIC-004` | 422 | El presupuesto quedaría por debajo de una oferta existente |
| `LIC-005` | 422 | La licitación conserva ofertas asociadas |

### Proveedores

| Código | Estado | Significado |
|---|---|---|
| `PRO-001` | 409 | Nombre de proveedor duplicado |
| `PRO-002` | 400 | Caracteres no permitidos en el nombre |
| `PRO-003` | 422 | El proveedor conserva ofertas asociadas |

### Ofertas

| Código | Estado | Significado |
|---|---|---|
| `OFE-001` | 409 | El proveedor ya ofertó en esa licitación |
| `OFE-002` | 422 | La oferta supera el presupuesto |
| `OFE-003` | 422 | La licitación ya alcanzó su fecha de cierre |
| `OFE-004` | 422 | La licitación no está publicada |
| `OFE-005` | 422 | La oferta pertenece a una licitación cerrada y es inmutable |

### Niveles de aprobación

| Código | Estado | Significado |
|---|---|---|
| `APR-001` | 422 | Los rangos se traslapan |
| `APR-002` | 422 | Ya existe un rango abierto |
| `APR-003` | 422 | El monto máximo es menor que el mínimo |
| `APR-004` | 422 | Ningún rango cubre el monto consultado |

### Tipo de cambio

| Código | Estado | Significado |
|---|---|---|
| `TCB-001` | 422 | No hay tipo de cambio activo, o se intentó eliminar el activo |
| `TCB-002` | 422 | El tipo de cambio no es mayor que cero |

### Generales

| Código | Estado | Significado |
|---|---|---|
| `GEN-001` | 422 | Monto cero o negativo |
| `GEN-002` | 404 | Recurso no encontrado |
| `GEN-003` | 409 | Conflicto de concurrencia |
| `GEN-004` | 400 | Validación de entrada fallida |

---

## Colección reproducible de solicitudes

El archivo [`api-solicitudes.http`](api-solicitudes.http) contiene el flujo funcional completo en
formato de peticiones HTTP. Se puede ejecutar desde Visual Studio Code con la extensión REST
Client, desde Visual Studio o desde JetBrains Rider, y también sirve como referencia para
reproducir el flujo con `curl`.
