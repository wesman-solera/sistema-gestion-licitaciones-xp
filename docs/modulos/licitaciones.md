# Módulo: Licitaciones

## Propósito

Administra el proceso de compra completo: su creación, su ciclo de vida y la evaluación de las
ofertas que recibe. Es el módulo central del sistema, y el que concentra más reglas del enunciado.

## Responsabilidades

- Crear licitaciones con código único normalizado
- Gobernar el ciclo de estados Borrador → Publicada → Cerrada
- Determinar si una licitación está cerrada funcionalmente por vencimiento
- Impedir que el presupuesto quede por debajo de una oferta existente
- Coordinar la evaluación de ofertas: mejor oferta, ahorro y clasificación
- Consultar el aprobador aplicable al monto ganador

## Lo que no hace

- No valida ofertas individuales: eso es del módulo de Ofertas
- No decide quién aprueba: consulta la tabla de Niveles de aprobación
- No convierte moneda: consume el servicio de Tipo de cambio

---

## Dependencias

| Depende de | Para qué |
|---|---|
| Ofertas | Leer las ofertas registradas y obtener la más alta |
| Niveles de aprobación | Consultar el aprobador del monto ganador |
| Tipo de cambio | Calcular el equivalente en dólares de los montos |
| Persistencia | Almacenar y recuperar licitaciones |

---

## Entradas y salidas

### Entradas

| Contrato | Campos |
|---|---|
| `CrearLicitacionRequest` | `Codigo`, `Titulo`, `PresupuestoEstimadoCrc`, `FechaCierre` |
| `ActualizarLicitacionRequest` | `Codigo`, `Titulo`, `PresupuestoEstimadoCrc`, `FechaCierre` |
| `CambiarEstadoRequest` | `Estado` |
| `ParametrosConsulta` | Paginación, búsqueda, orden, inclusión de eliminados |

### Salidas

| Contrato | Contenido |
|---|---|
| `LicitacionResumenDto` | Datos básicos, estado efectivo y cantidad de ofertas |
| `LicitacionDetalleDto` | Todo lo anterior más evaluación, ofertas y tipo de cambio aplicado |
| `EvaluacionLicitacionDto` | Mejor oferta, ahorro, clasificación y aprobador |

---

## Reglas de negocio

### Ciclo de estados (sección 8.1)

| Origen | Destino | Permitida | Condición |
|---|---|---|---|
| Borrador | Publicada | Sí | Datos completos, presupuesto válido y fecha de cierre futura |
| Borrador | Cerrada | Sí | Cancelación documentada |
| Publicada | Cerrada | Sí | Por acción autorizada o al alcanzar la fecha de cierre |
| Publicada | Borrador | **No** | — |
| Cerrada | Publicada o Borrador | **No** | Salvo autorización expresa de la persona docente |
| Cualquiera | El mismo | **No** | Ya se encuentra en ese estado |

La política está implementada como **tabla de datos**, no como cadena de condiciones. Agregar o
retirar una transición es modificar el conjunto `Permitidas`, no la lógica.

```csharp
private static readonly HashSet<(EstadoLicitacion Origen, EstadoLicitacion Destino)> Permitidas =
[
    (EstadoLicitacion.Borrador, EstadoLicitacion.Publicada),
    (EstadoLicitacion.Borrador, EstadoLicitacion.Cerrada),
    (EstadoLicitacion.Publicada, EstadoLicitacion.Cerrada)
];
```

### Estado persistido frente a estado efectivo

Esta distinción es la que más confusión genera al leer el código, así que conviene tenerla clara.

El enunciado dice: *«Una licitación cuya fecha de cierre haya sido alcanzada se considera cerrada
funcionalmente, aunque una actualización tardía del campo de estado todavía indique Publicada»*.

| Concepto | Qué es | Cuándo se usa |
|---|---|---|
| `Estado` | La columna persistida | Para decidir transiciones válidas |
| `EstadoEfectivo` | `Cerrada` si venció, si no el persistido | Para mostrar en pantalla |
| `EstaCerradaFuncionalmente(ahora)` | El método que decide | Para aceptar o rechazar actividad |

**Toda decisión sobre aceptación de ofertas consulta el método, nunca la columna sola.** La interfaz
muestra el estado efectivo y, cuando difiere del persistido, lo aclara para que no parezca un error.

### Unicidad del código (sección 8.3)

El código es único ignorando espacios laterales y diferencias de mayúsculas. Se materializa en la
columna `codigo_normalizado`, respaldada por el índice único `ux_licitaciones_codigo_normalizado`.

`LIC-2026-001`, `  lic-2026-001  ` y `Lic-2026-001` son el mismo código.

El código **solo puede modificarse mientras la licitación está en Borrador**. Una vez publicada, los
proveedores ya la conocen por ese identificador.

### Presupuesto (sección 8.5)

- Debe ser mayor que cero, validado en interfaz, servidor y restricción `CHECK`
- Admite como máximo dos decimales, acorde con `numeric(18,2)`
- **No puede reducirse por debajo de una oferta ya registrada**

La última regla necesita un dato que la entidad no puede conocer. Se resuelve pasándoselo:

```csharp
public void ActualizarDatos(
    string titulo,
    decimal presupuestoEstimadoCrc,
    DateTimeOffset fechaCierre,
    decimal? mayorOfertaRegistradaCrc,   // ← lo aporta el servicio
    DateTimeOffset ahoraUtc)
```

El servicio consulta `ObtenerMayorMontoAsync` —una agregación en la base de datos, no una carga de
todas las ofertas— y se lo entrega a la entidad, que decide.

### Evaluación de ofertas (sección 8.6)

| Paso | Regla |
|---|---|
| Mejor oferta | La de menor monto en colones |
| Desempate 1 | La registrada primero |
| Desempate 2 | El identificador ordenable, para que el resultado sea determinista |
| Ahorro | `((Presupuesto − Mejor oferta) / Presupuesto) × 100` |
| Clasificación | ≥ 10 % conveniente · > 0 % y < 10 % aceptable · = 0 % válida sin ahorro · sin ofertas, sin ofertas válidas |

**Detalle que causó un defecto real.** La primera implementación redondeaba el porcentaje antes de
clasificar, y un ahorro de 9,996 % ascendía a «conveniente». Se separaron los dos usos: el valor
exacto decide la clasificación, el redondeado solo se muestra.

El resultado **nunca se persiste**. Se calcula a demanda, de modo que no puede quedar
desincronizado con las ofertas reales.

---

## Errores

| Código | Estado | Situación |
|---|---|---|
| `LIC-001` | 409 | Código duplicado |
| `LIC-002` | 409 | Transición no permitida |
| `LIC-003` | 422 | Fecha de cierre no futura |
| `LIC-004` | 422 | Presupuesto por debajo de una oferta existente |
| `GEN-001` | 422 | Presupuesto cero o negativo |
| `GEN-002` | 404 | Licitación inexistente |
| `GEN-003` | 409 | Conflicto de concurrencia |

---

## Pruebas

| Prueba | Verifica |
|---|---|
| `LicitacionPruebas.Crear_*` | Creación, validaciones y normalización del código |
| `LicitacionPruebas.Publicar_*` | Precondiciones de publicación |
| `LicitacionPruebas.EstaCerradaFuncionalmente_*` | Vencimiento, incluido el instante exacto |
| `LicitacionPruebas.ActualizarDatos_*` | Regla del presupuesto no reducible |
| `PoliticaTransicionEstadoPruebas` | Las nueve combinaciones de origen y destino |
| `EvaluadorOfertasPruebas` | Mejor oferta, desempate, ahorro y clasificación |
| `LicitacionServicioPruebas` | Coordinación, unicidad y evaluación con aprobador |
| `LicitacionesEndpointsPruebas` | Contrato HTTP completo |
| `FlujoCompletoPruebas` | Recorrido desde el navegador |

---

## Archivos

| Archivo | Contenido |
|---|---|
| `Domain/Entidades/Licitacion.cs` | Entidad y sus reglas |
| `Domain/Enums/EstadoLicitacion.cs` | Estados del ciclo de vida |
| `Domain/Servicios/PoliticaTransicionEstado.cs` | Tabla de transiciones |
| `Domain/Servicios/EvaluadorOfertas.cs` | Mejor oferta y clasificación |
| `Domain/ObjetosValor/ResultadoEvaluacionOfertas.cs` | Resultado de la evaluación |
| `Application/Servicios/LicitacionServicio.cs` | Coordinación de los casos de uso |
| `Infrastructure/Repositorios/LicitacionRepositorio.cs` | Acceso a datos |
| `Api/Controladores/LicitacionesApiController.cs` | Endpoints REST |
| `Web/Controladores/LicitacionesController.cs` | Pantallas |
