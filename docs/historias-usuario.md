# Historias de usuario

Las historias están escritas desde la perspectiva del cliente, con criterios de aceptación
verificables. Cada una indica su iteración, su prioridad, su estimación en puntos y las pruebas
que la respaldan.

## Escala de estimación

Se usan puntos de historia con la serie 1, 2, 3, 5, 8. Un punto equivale aproximadamente a media
jornada de trabajo efectivo. La estimación es relativa: una historia de 5 puntos es
aproximadamente cinco veces más grande que una de 1, no necesariamente cinco veces más larga en
horas de reloj.

| Prioridad | Significado |
|---|---|
| Alta | Sin ella no existe el flujo funcional mínimo |
| Media | Necesaria para cumplir el enunciado, pero el flujo funciona sin ella |
| Baja | Mejora la experiencia o la operación |

## Resumen

| # | Historia | Iteración | Prioridad | Puntos |
|---|---|---|---|---|
| H-01 | Registrar un proveedor con nombre único | 1 | Alta | 3 |
| H-02 | Impedir nombres de proveedor equivalentes | 1 | Alta | 3 |
| H-03 | Restringir los caracteres del nombre de proveedor | 1 | Media | 2 |
| H-04 | Consultar, editar y eliminar proveedores | 1 | Alta | 3 |
| H-05 | Crear una licitación con código único | 1 | Alta | 3 |
| H-06 | Seleccionar la fecha de cierre con calendario | 1 | Alta | 2 |
| H-07 | Publicar una licitación | 2 | Alta | 3 |
| H-08 | Impedir transiciones de estado no permitidas | 2 | Alta | 3 |
| H-09 | Cerrar una licitación por acción o por vencimiento | 2 | Alta | 3 |
| H-10 | Registrar una oferta válida | 2 | Alta | 5 |
| H-11 | Rechazar una oferta duplicada del mismo proveedor | 2 | Alta | 3 |
| H-12 | Rechazar una oferta superior al presupuesto | 2 | Alta | 2 |
| H-13 | Rechazar una oferta vencida | 2 | Alta | 3 |
| H-14 | Impedir reducir el presupuesto bajo una oferta existente | 3 | Media | 3 |
| H-15 | Consultar la mejor oferta y su clasificación | 3 | Alta | 5 |
| H-16 | Determinar el aprobador desde una tabla parametrizable | 3 | Alta | 5 |
| H-17 | Administrar los niveles de aprobación | 3 | Media | 3 |
| H-18 | Administrar el tipo de cambio | 3 | Media | 3 |
| H-19 | Alternar la visualización entre colones y dólares | 3 | Alta | 3 |
| H-20 | Conservar las ofertas de licitaciones cerradas | 3 | Media | 3 |
| H-21 | Consultar la landing page con la explicación del sistema | 4 | Media | 2 |
| H-22 | Alternar entre modo claro y modo oscuro | 4 | Media | 2 |
| H-23 | Operar el sistema completo desde la API REST | 4 | Alta | 5 |
| H-24 | Recibir mensajes de error comprensibles y seguros | 4 | Alta | 3 |
| H-25 | Navegar listados con paginación, filtro y orden | 4 | Media | 3 |
| H-26 | Desplegar la solución con un solo comando | 4 | Alta | 5 |

**Total: 82 puntos.**

---

## Iteración 1 — Fundamentos y proveedores

### H-01 · Registrar un proveedor con nombre único

> Como encargado de compras quiero registrar un proveedor con su nombre para poder asociarle
> ofertas más adelante.

- **Prioridad:** Alta · **Estimación:** 3 puntos

**Criterios de aceptación**

1. El formulario pide el nombre y lo guarda al enviarlo.
2. Un nombre vacío o compuesto solo de espacios se rechaza con un mensaje junto al campo.
3. El proveedor guardado aparece en el listado.
4. Se registran las marcas de creación y modificación.

**Pruebas:** `ProveedorPruebas.Crear_*`, `ProveedorServicioPruebas.CrearAsync_ConNombreDisponible_*`,
`ProveedoresYTiposCambioEndpointsPruebas.Post_Proveedor_ConNombreValido_*`

---

### H-02 · Impedir nombres de proveedor equivalentes

> Como encargado de compras quiero que el sistema detecte que «Empresa Central», « empresa
> central» y «EMPRESA  CENTRAL» son el mismo proveedor, para no terminar con duplicados que
> ensucien las comparaciones de ofertas.

- **Prioridad:** Alta · **Estimación:** 3 puntos

**Criterios de aceptación**

1. Los tres ejemplos anteriores se consideran el mismo nombre.
2. El segundo intento de registro se rechaza con un mensaje junto al campo.
3. La comprobación se aplica en la interfaz, en el servidor y en un índice único de PostgreSQL.
4. Dos representaciones Unicode distintas del mismo carácter acentuado se tratan como iguales.

**Pruebas:** `ProveedorPruebas.Crear_NormalizaLosNombresEquivalentesAlMismoValor`,
`RestriccionesPruebas.IndiceUnico_RechazaDosProveedoresConNombreEquivalente`,
`FlujoCompletoPruebas.Proveedor_ConNombreEquivalente_*`

---

### H-03 · Restringir los caracteres del nombre de proveedor

> Como encargado de compras quiero que el nombre solo admita caracteres razonables, para evitar
> que se cuele contenido extraño en los listados y los reportes.

- **Prioridad:** Media · **Estimación:** 2 puntos

**Criterios de aceptación**

1. Se admiten letras, números, espacios, punto, coma y paréntesis.
2. Cualquier otro símbolo se rechaza con un mensaje explicativo.
3. La restricción se aplica tanto en la interfaz como en el servidor.

**Pruebas:** `ProveedorPruebas.Crear_RechazaLosCaracteresNoPermitidos`,
`ProveedoresYTiposCambioEndpointsPruebas.Post_Proveedor_ConCaracteresNoPermitidos_*`

---

### H-04 · Consultar, editar y eliminar proveedores

> Como encargado de compras quiero ver, corregir y dar de baja proveedores para mantener el
> catálogo al día.

- **Prioridad:** Alta · **Estimación:** 3 puntos

**Criterios de aceptación**

1. El listado muestra los proveedores con su cantidad de ofertas.
2. La edición aplica las mismas validaciones que el alta.
3. La eliminación pide confirmación antes de ejecutarse.
4. Un proveedor con ofertas asociadas se marca como eliminado en lugar de borrarse.
5. Un proveedor sin ofertas se elimina definitivamente.

**Pruebas:** `ProveedorServicioPruebas.EliminarAsync_*`

---

### H-05 · Crear una licitación con código único

> Como encargado de compras quiero registrar una licitación con su código, título, presupuesto y
> fecha de cierre para poder publicarla después.

- **Prioridad:** Alta · **Estimación:** 3 puntos

**Criterios de aceptación**

1. La licitación nace en estado Borrador.
2. El código es único ignorando espacios laterales y mayúsculas.
3. El presupuesto debe ser mayor que cero.
4. El presupuesto admite como máximo dos decimales.

**Pruebas:** `LicitacionPruebas.Crear_*`, `LicitacionServicioPruebas.CrearAsync_*`,
`LicitacionesEndpointsPruebas.Post_CrearLicitacion_*`

---

### H-06 · Seleccionar la fecha de cierre con calendario

> Como encargado de compras quiero elegir la fecha y hora de cierre en un calendario, para no
> equivocarme escribiendo el formato a mano.

- **Prioridad:** Alta · **Estimación:** 2 puntos

**Criterios de aceptación**

1. El formulario ofrece un control de fecha y hora, no un campo de texto libre.
2. La hora mostrada corresponde a la zona horaria de Costa Rica.
3. El valor se almacena en UTC.
4. La fecha debe ser posterior al momento actual.

**Pruebas:** `ConcurrenciaYTransaccionPruebas.Fechas_SeConservanEnUtcAlPersistirYRecuperar`,
`FlujoCompletoPruebas.FlujoMinimo_*`

---

## Iteración 2 — Ciclo de vida y ofertas

### H-07 · Publicar una licitación

> Como encargado de compras quiero publicar una licitación para que empiece a recibir ofertas.

- **Prioridad:** Alta · **Estimación:** 3 puntos

**Criterios de aceptación**

1. Solo se publica desde Borrador.
2. Se exige título, presupuesto válido y fecha de cierre futura.
3. Tras publicar, el estado visible cambia a Publicada.

**Pruebas:** `LicitacionPruebas.Publicar_*`

---

### H-08 · Impedir transiciones de estado no permitidas

> Como encargado de compras quiero que el sistema impida deshacer una publicación, para que el
> proceso sea confiable frente a los proveedores.

- **Prioridad:** Alta · **Estimación:** 3 puntos

**Criterios de aceptación**

1. De Publicada a Borrador no se permite.
2. De Cerrada a cualquier otro estado no se permite.
3. El intento devuelve un mensaje que explica el motivo.
4. La interfaz solo ofrece las transiciones válidas desde el estado actual.

**Pruebas:** `PoliticaTransicionEstadoPruebas.*`,
`LicitacionesEndpointsPruebas.Patch_TransicionDePublicadaABorrador_*`

---

### H-09 · Cerrar una licitación por acción o por vencimiento

> Como encargado de compras quiero cerrar una licitación cuando corresponda, y que el sistema la
> considere cerrada automáticamente al llegar su fecha de cierre.

- **Prioridad:** Alta · **Estimación:** 3 puntos

**Criterios de aceptación**

1. Se puede cerrar desde Borrador, como cancelación documentada.
2. Se puede cerrar desde Publicada.
3. Alcanzada la fecha de cierre, la licitación se considera cerrada aunque su columna diga
   Publicada.
4. La interfaz distingue ese caso para que no parezca un error.

**Pruebas:** `LicitacionPruebas.EstaCerradaFuncionalmente_*`

---

### H-10 · Registrar una oferta válida

> Como encargado de compras quiero registrar la oferta económica de un proveedor para una
> licitación publicada.

- **Prioridad:** Alta · **Estimación:** 5 puntos

**Criterios de aceptación**

1. Se selecciona licitación y proveedor de listas desplegables.
2. El monto debe ser mayor que cero y admite dos decimales.
3. La oferta queda con su fecha de registro.
4. Solo se ofrecen licitaciones publicadas y vigentes.

**Pruebas:** `OfertaPruebas.Registrar_*`, `OfertaServicioPruebas.RegistrarAsync_*`

---

### H-11 · Rechazar una oferta duplicada del mismo proveedor

> Como encargado de compras quiero que un proveedor no pueda presentar dos ofertas para la misma
> licitación, para que la comparación sea limpia.

- **Prioridad:** Alta · **Estimación:** 3 puntos

**Criterios de aceptación**

1. El segundo intento del mismo proveedor se rechaza.
2. Existe un índice único compuesto de licitación y proveedor en la base de datos.
3. El mensaje explica el motivo con claridad.

**Pruebas:** `OfertaServicioPruebas.RegistrarAsync_ConOfertaDuplicadaDelMismoProveedor_Falla`,
`RestriccionesPruebas.IndiceUnicoCompuesto_*`

---

### H-12 · Rechazar una oferta superior al presupuesto

> Como encargado de compras quiero que el sistema rechace ofertas por encima del presupuesto, para
> no perder tiempo evaluando propuestas inviables.

- **Prioridad:** Alta · **Estimación:** 2 puntos

**Criterios de aceptación**

1. Una oferta mayor que el presupuesto se rechaza.
2. Una oferta exactamente igual al presupuesto se acepta.
3. El mensaje indica el presupuesto vigente.

**Pruebas:** `OfertaPruebas.Registrar_ConMontoSuperiorAlPresupuesto_Falla`,
`OfertaPruebas.Registrar_ConMontoIgualAlPresupuesto_EsValida`

---

### H-13 · Rechazar una oferta vencida

> Como encargado de compras quiero que no se acepten ofertas después del cierre, para que el
> proceso sea justo.

- **Prioridad:** Alta · **Estimación:** 3 puntos

**Criterios de aceptación**

1. Una oferta presentada después de la fecha de cierre se rechaza.
2. Una oferta presentada en el instante exacto del cierre también se rechaza.
3. Un segundo antes del cierre todavía se acepta.
4. El comportamiento se puede probar sin depender de la hora real de ejecución.

**Pruebas:** `OfertaPruebas.Registrar_EnElInstanteExactoDelCierre_Falla`,
`OfertaPruebas.Registrar_UnSegundoAntesDelCierre_EsValida`

---

## Iteración 3 — Evaluación, aprobación y moneda

### H-14 · Impedir reducir el presupuesto bajo una oferta existente

> Como encargado de compras quiero que el sistema impida bajar el presupuesto por debajo de una
> oferta ya recibida, porque dejaría esa oferta en un estado contradictorio.

- **Prioridad:** Media · **Estimación:** 3 puntos

**Criterios de aceptación**

1. El intento se rechaza indicando la oferta más alta registrada.
2. Reducir el presupuesto hasta igualar esa oferta sí se permite.

**Pruebas:** `LicitacionPruebas.ActualizarDatos_ReduciendoElPresupuesto*`,
`LicitacionServicioPruebas.ActualizarAsync_ConsultaLaOfertaMasAlta*`

---

### H-15 · Consultar la mejor oferta y su clasificación

> Como encargado de compras quiero ver cuál es la mejor oferta y cuánto ahorro representa, para
> sustentar la adjudicación.

- **Prioridad:** Alta · **Estimación:** 5 puntos

**Criterios de aceptación**

1. La mejor oferta es la de menor monto en colones.
2. En empate gana la registrada primero.
3. El ahorro se calcula como `((Presupuesto − Mejor oferta) / Presupuesto) × 100`.
4. Se muestra la etiqueta exacta: «Oferta conveniente» (≥ 10 %), «Oferta aceptable» (> 0 % y
   < 10 %), «Oferta válida sin ahorro» (0 %) o «Sin ofertas válidas».

**Pruebas:** `EvaluadorOfertasPruebas.*`

---

### H-16 · Determinar el aprobador desde una tabla parametrizable

> Como administrador quiero que el aprobador se obtenga de una tabla configurable, para poder
> cambiar los rangos sin pedir una modificación al sistema.

- **Prioridad:** Alta · **Estimación:** 5 puntos

**Criterios de aceptación**

1. El aprobador se resuelve recorriendo la tabla, no con condiciones fijas en el código.
2. Los rangos son inclusivos en ambos extremos.
3. Si ningún rango cubre el monto, se informa sin interrumpir la consulta.

**Pruebas:** `SelectorNivelAprobacionPruebas.*`,
`LicitacionServicioPruebas.ObtenerMejorOfertaAsync_DevuelveElAprobador*`

---

### H-17 · Administrar los niveles de aprobación

> Como administrador quiero crear, editar y eliminar rangos de aprobación desde la interfaz.

- **Prioridad:** Media · **Estimación:** 3 puntos

**Criterios de aceptación**

1. Los rangos no pueden traslaparse entre sí.
2. Solo puede existir un rango abierto sin monto máximo.
3. El monto máximo no puede ser menor que el mínimo.
4. Las tres condiciones se comprueban también en la base de datos donde es posible.

**Pruebas:** `SelectorNivelAprobacionPruebas.AsegurarConjuntoValido_*`

---

### H-18 · Administrar el tipo de cambio

> Como administrador quiero registrar y activar tipos de cambio localmente, para que el sistema
> funcione sin acceso a Internet.

- **Prioridad:** Media · **Estimación:** 3 puntos

**Criterios de aceptación**

1. El valor debe ser mayor que cero.
2. Solo puede haber un tipo de cambio activo.
3. Activar uno desactiva el anterior en una sola transacción.
4. El tipo de cambio activo no puede eliminarse.
5. Se muestra la fecha del tipo de cambio utilizado.

**Pruebas:** `ConcurrenciaYTransaccionPruebas.Transaccion_ActivarUnTipoDeCambio*`,
`ProveedoresYTiposCambioEndpointsPruebas.Patch_ActivarTipoCambio_*`

---

### H-19 · Alternar la visualización entre colones y dólares

> Como encargado de compras quiero ver los montos en dólares sin que eso cambie los valores
> registrados.

- **Prioridad:** Alta · **Estimación:** 3 puntos

**Criterios de aceptación**

1. Un botón visible alterna entre CRC y USD.
2. La conversión aplica `Monto USD = Monto CRC / Tipo de cambio`.
3. Los valores almacenados no se modifican.
4. Al volver a colones reaparece exactamente el valor original.
5. Sin tipo de cambio activo, la lectura sigue funcionando en colones.

**Pruebas:** `ConversorMonedaPruebas.*`,
`InterfazPruebas.AlternarMoneda_CambiaLaVisualizacionSinAlterarLosDatos`

---

### H-20 · Conservar las ofertas de licitaciones cerradas

> Como encargado de compras quiero que las ofertas de un proceso cerrado no se puedan alterar,
> para que sirvan como evidencia.

- **Prioridad:** Media · **Estimación:** 3 puntos

**Criterios de aceptación**

1. Una oferta de licitación cerrada o vencida no puede editarse ni eliminarse.
2. Eliminar una licitación con ofertas aplica borrado lógico.
3. La base de datos impide el borrado físico mediante clave foránea restrictiva.

**Pruebas:** `OfertaPruebas.CambiarMonto_TrasElVencimiento*`,
`RestriccionesPruebas.ClaveForanea_ImpideBorrarUnaLicitacionConOfertas`

---

## Iteración 4 — Experiencia, API y despliegue

### H-21 · Consultar la landing page con la explicación del sistema

> Como visitante quiero entender qué hace el sistema antes de usarlo.

- **Prioridad:** Media · **Estimación:** 2 puntos

**Criterios de aceptación**

1. Explica el propósito, el flujo, las ofertas, la mejor oferta, el nivel de aprobación y la
   conversión monetaria.
2. Ofrece acceso directo a las secciones principales.
3. Muestra el tipo de cambio vigente y su fecha.

**Pruebas:** `InterfazPruebas.LandingPage_ExplicaElPropositoDelSistema`

---

### H-22 · Alternar entre modo claro y modo oscuro

> Como usuario quiero elegir el tema visual y que el sistema lo recuerde.

- **Prioridad:** Media · **Estimación:** 2 puntos

**Criterios de aceptación**

1. Un control visible alterna el tema.
2. La preferencia persiste entre páginas y entre sesiones.
3. La página se renderiza ya con el tema correcto, sin parpadeo.

**Pruebas:** `InterfazPruebas.ModoOscuro_SeActivaConElControlYPersisteAlNavegar`

---

### H-23 · Operar el sistema completo desde la API REST

> Como sistema externo quiero realizar las mismas operaciones por HTTP.

- **Prioridad:** Alta · **Estimación:** 5 puntos

**Criterios de aceptación**

1. Existen los endpoints mínimos del enunciado, versionados bajo `/api/v1`.
2. La API expone DTO, no entidades de Entity Framework Core.
3. Hay documentación interactiva OpenAPI.
4. Los listados admiten paginación, filtrado y ordenamiento.

**Pruebas:** `LicitacionesEndpointsPruebas.*`, `ProveedoresYTiposCambioEndpointsPruebas.Get_Swagger_*`

---

### H-24 · Recibir mensajes de error comprensibles y seguros

> Como usuario quiero entender qué salió mal, y como responsable del sistema quiero que ningún
> detalle interno se filtre al cliente.

- **Prioridad:** Alta · **Estimación:** 3 puntos

**Criterios de aceptación**

1. Los errores se devuelven como ProblemDetails con título, estado, detalle, código de error e
   identificador de correlación.
2. Se usan 400, 404, 409 y 422 según corresponda.
3. No se exponen trazas de pila, rutas internas, consultas ni credenciales.
4. En la interfaz, el mensaje aparece junto al campo correspondiente.

**Pruebas:** `LicitacionesEndpointsPruebas.ProblemDetails_NoExponeDetallesInternos`

---

### H-25 · Navegar listados con paginación, filtro y orden

> Como encargado de compras quiero encontrar rápido lo que busco aunque haya muchos registros.

- **Prioridad:** Media · **Estimación:** 3 puntos

**Criterios de aceptación**

1. Los listados se paginan con un tamaño configurable y acotado.
2. Se puede buscar por texto libre.
3. Se puede ordenar pulsando el encabezado de la columna.
4. Los filtros se conservan al cambiar de página.

**Pruebas:** `LicitacionesEndpointsPruebas.Get_Listado_DevuelveLaEstructuraDePaginacion`

---

### H-26 · Desplegar la solución con un solo comando

> Como responsable del sistema quiero levantar todo el entorno sin pasos manuales.

- **Prioridad:** Alta · **Estimación:** 5 puntos

**Criterios de aceptación**

1. `docker compose up --build` levanta aplicación y base de datos.
2. Las migraciones se aplican solas, con reintentos mientras la base arranca.
3. Los datos sobreviven al reinicio de los contenedores.
4. Existen manifiestos de Kubernetes con sondas, almacenamiento persistente y configuración
   separada de los secretos.

**Pruebas:** Trabajo `pruebas-funcionales` de la integración continua, que levanta la solución con
Docker Compose antes de ejecutar Playwright.
