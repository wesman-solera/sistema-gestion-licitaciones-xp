# Bitácora XP

Registro por iteración: historias completadas, velocidad observada, ciclos de TDD,
refactorizaciones, pequeña liberación y ajustes para la iteración siguiente.

> **Nota sobre el ritmo de trabajo.** El proyecto se desarrolló en modalidad individual y en un
> período de trabajo intensivo, con asistencia de una herramienta de inteligencia artificial
> declarada en [uso-ia.md](uso-ia.md). Las cuatro iteraciones corresponden a pasadas sucesivas y
> reales sobre el código, cada una con su alcance cerrado y su liberación demostrable, y así se
> reflejan en el historial de commits. Esta bitácora no atribuye a esas iteraciones fechas de
> calendario que no ocurrieron.

## Velocidad observada

| Iteración | Puntos planificados | Puntos completados | Velocidad |
|---|---|---|---|
| 1 | 16 | 16 | 16 |
| 2 | 22 | 22 | 22 |
| 3 | 25 | 25 | 25 |
| 4 | 19 | 19 | 19 |
| **Total** | **82** | **82** | **20,5 promedio** |

La velocidad creció de la iteración 1 a la 3 y bajó en la 4. La subida se explica porque el
andamiaje —solución, capas, contexto de datos, contenedor de pruebas— se construyó completo en la
iteración 1 y se amortizó después. La bajada de la 4 no indica pérdida de ritmo: esa iteración
concentra el trabajo de infraestructura y despliegue, donde un punto de historia cuesta más
tiempo de reloj que uno de lógica de dominio.

---

## Iteración 1 — Fundamentos y proveedores

**Historias completadas:** H-01, H-02, H-03, H-04, H-05, H-06 · **16 puntos**

### Ciclos de TDD destacados

**Normalización de nombres equivalentes (H-02).**

- *Rojo.* Se escribió `Crear_NormalizaLosNombresEquivalentesAlMismoValor` con los tres ejemplos
  del enunciado. Falló porque `NormalizarNombre` todavía no existía.
- *Verde.* Se implementó la normalización mínima: recortar y pasar a mayúsculas. Dos de los tres
  casos pasaron; `EMPRESA  CENTRAL` seguía fallando por el espacio doble.
- *Verde de nuevo.* Se agregó la reducción de espacios repetidos.
- *Refactorización.* La normalización quedó extraída en `NormalizadorTexto`, fuera de la entidad,
  para poder reutilizarla desde el validador de la capa de aplicación sin instanciar un proveedor.

**Conversión de zona horaria (H-06).**

- *Rojo.* La prueba de persistencia comprobaba que una fecha guardada volviera idéntica. Falló:
  el control `datetime-local` envía la hora sin desplazamiento y se estaba interpretando como UTC,
  lo que desplazaba el cierre seis horas.
- *Verde.* Se agregó `FormateadorFecha.DesdeControlCalendario`, que interpreta el valor como hora
  de Costa Rica antes de convertirlo.

### Refactorizaciones

| Refactorización | Motivo |
|---|---|
| Extraer `NormalizadorTexto` de las entidades | La misma regla la necesitaban la entidad y el validador |
| Separar `Nombre` de `NombreNormalizado` como columnas distintas | Permite mostrar el nombre tal como lo escribió el usuario y comparar por la forma normalizada |
| Mover las configuraciones de EF Core a clases por entidad | El `OnModelCreating` empezaba a crecer sin control |

### Pequeña liberación

Aplicación web con CRUD de proveedores y alta de licitaciones sobre PostgreSQL real, con
migraciones versionadas y datos semilla.

### Retroalimentación y ajuste

Al probar el alta de licitaciones se hizo evidente que las pruebas de fecha iban a ser frágiles si
dependían del reloj del sistema. Se introdujo `IRelojSistema` al cierre de esta iteración, antes de
escribir ninguna regla de vencimiento. **Ajuste para la iteración 2:** prohibir el uso directo de
`DateTimeOffset.UtcNow` fuera de su única implementación, y construir sobre esa abstracción todas
las reglas de cierre y vencimiento.

---

## Iteración 2 — Ciclo de vida y ofertas

**Historias completadas:** H-07 … H-13 · **22 puntos**

### Ciclos de TDD destacados

**Ciclo de estados (H-08).**

- *Rojo.* En lugar de casos sueltos se escribió una prueba con las nueve combinaciones posibles de
  origen y destino. Falló completa: no había política.
- *Verde.* Primera implementación con `switch` anidado. Pasó, pero agregar un estado habría
  obligado a tocar la lógica en varios sitios.
- *Refactorización.* La política pasó a ser un conjunto de datos: un `HashSet` de pares permitidos
  y un diccionario de motivos. Las pruebas no cambiaron, que es la comprobación de que la
  refactorización no alteró el comportamiento.

**Límite exacto del vencimiento (H-13).**

- *Rojo.* Se escribieron dos pruebas complementarias: una para el instante exacto del cierre y otra
  para un segundo antes. La primera falló, porque la comparación usaba `>` en lugar de `>=`.
- *Verde.* Se corrigió el operador. Las dos pruebas juntas fijan el límite y hacen imposible que un
  cambio futuro lo desplace sin que la suite lo detecte.

### Refactorizaciones

| Refactorización | Motivo |
|---|---|
| `PoliticaTransicionEstado` de condicionales a tabla de datos | Agregar una transición debe ser cambiar datos, no lógica |
| Concentrar las cuatro validaciones de oferta en `Oferta.Registrar` | Estaban repartidas entre el servicio y la entidad |
| Introducir `CodigosError` | Los mensajes se comparaban por texto en las pruebas, lo que las volvía frágiles ante cualquier cambio de redacción |

### Pequeña liberación

Flujo publicar → ofertar operativo, con los tres rechazos del enunciado funcionando y probados.

### Retroalimentación y ajuste

La comprobación de oferta duplicada se implementó primero solo en el servicio. Al revisarlo se
concluyó que dos peticiones simultáneas podrían superarla ambas. **Ajuste para la iteración 3:**
toda regla de unicidad debe tener respaldo en un índice de PostgreSQL, y el manejador de errores
debe traducir la violación de índice a un mensaje controlado en lugar de dejar escapar el error
del motor.

---

## Iteración 3 — Evaluación, aprobación y moneda

**Historias completadas:** H-14 … H-20 · **25 puntos**

### Ciclos de TDD destacados

**Clasificación del ahorro (H-15).**

- *Rojo.* Se escribió una prueba con los umbrales del enunciado, incluyendo dos casos de borde:
  ahorro de exactamente 10 % y ahorro de 9,99 %.
- *Verde.* La primera implementación redondeaba el porcentaje antes de clasificar, y un ahorro de
  9,996 % ascendía a «conveniente». La prueba de borde lo detectó.
- *Refactorización.* Se separaron las dos responsabilidades: el valor exacto decide la
  clasificación, y el valor redondeado se usa solo para mostrar.

**Aprobador parametrizable (H-16).**

- *Rojo.* Se escribió una prueba que recorre ocho montos en los bordes de los tres rangos, con los
  datos tomados de la tabla. Falló porque no existía el selector.
- *Verde.* Implementación que ordena por monto mínimo y devuelve el primer rango que cubre el
  monto. Sin un solo umbral literal en el código.

**Activación transaccional del tipo de cambio (H-18).**

- *Rojo.* La prueba de integración comprobaba que tras activar uno nuevo quedara exactamente un
  activo. Falló con violación del índice único parcial: se estaba activando el nuevo antes de
  desactivar el anterior, y PostgreSQL evaluaba la restricción con dos filas activas.
- *Verde.* Se ordenaron las operaciones dentro de la transacción: primero desactivar y confirmar,
  después activar.

### Refactorizaciones

| Refactorización | Motivo |
|---|---|
| Introducir `ContextoMoneda` con ciclo de vida por petición | Cada monto convertido disparaba su propia consulta del tipo de cambio: un listado de 20 licitaciones hacía 20 consultas idénticas |
| Extraer `ResultadoEvaluacionOfertas` como objeto de valor | El servicio devolvía una tupla con cuatro elementos sin nombre |
| Agregar `ContarOfertasAsync` al repositorio de proveedores | El listado cargaba la colección de ofertas de cada proveedor para mostrar solo su cantidad |
| Índice único parcial para el tipo de cambio activo | La invariante «un solo activo» dependía únicamente del código de aplicación |

### Pequeña liberación

Sistema que evalúa ofertas, clasifica el ahorro, determina el aprobador desde la tabla y muestra
los montos en ambas monedas.

### Retroalimentación y ajuste

Al revisar el detalle de una licitación se detectó que, si la tabla de aprobación no cubría el
monto, la pantalla completa fallaba por un dato de configuración corregible. **Ajuste para la
iteración 4:** distinguir entre el error que impide continuar y el dato ausente que solo debe
informarse. La consulta pasó a devolver el aprobador en nulo con un aviso en pantalla.

---

## Iteración 4 — Experiencia, API y despliegue

**Historias completadas:** H-21 … H-26 · **19 puntos**

### Ciclos de TDD destacados

**Errores sin filtración de detalles internos (H-24).**

- *Rojo.* Se escribió una prueba que consulta un recurso inexistente y verifica que el cuerpo de la
  respuesta no contenga `Npgsql`, `SELECT`, rutas de código, `Password` ni trazas de pila. Falló:
  el manejador devolvía el mensaje de la excepción tal cual, incluso en los errores de servidor.
- *Verde.* Se separó el tratamiento: los mensajes del dominio, escritos para el usuario, sí se
  exponen; cualquier error inesperado devuelve un texto genérico y el detalle queda solo en el
  registro del servidor, localizable por el identificador de correlación.

**Persistencia del tema visual (H-22).**

- *Rojo.* La prueba funcional comprobaba que el modo oscuro sobreviviera a un cambio de página.
  Falló con la primera implementación, que aplicaba el tema desde el navegador después de cargar.
- *Verde.* El tema pasó a resolverse en el servidor a partir de una cookie, de modo que el HTML
  llega ya con el atributo correcto. Como efecto secundario desapareció el parpadeo inicial.

### Refactorizaciones

| Refactorización | Motivo |
|---|---|
| Extraer `ControladorBase` con la traducción de errores de negocio | Los cinco controladores MVC repetían el mismo bloque de captura |
| Unificar la respuesta de error del enlace de modelo con la del manejador global | La API devolvía dos formatos distintos de error según dónde fallara |
| Extraer `PaginacionViewModel` | El cálculo de «mostrando X a Y de Z» estaba repetido en los cinco listados |
| Renombrar las clases de registro de servicios por capa | Las tres se llamaban igual y obligaban a calificar el espacio de nombres en cada uso |
| Sustituir el framework visual por hoja de estilos propia | Una CDN dejaría la interfaz inutilizable sin Internet, lo que el requisito 9 prohíbe |

### Pequeña liberación

Versión 1.0.0: sistema completo, desplegable con un solo comando, con API documentada,
manifiestos de Kubernetes e integración continua en verde.

### Retroalimentación de cierre

Revisión final contra la rúbrica del enunciado, criterio por criterio, verificando que cada
elemento evaluado se pueda rastrear hasta una historia, una prueba, commits y documentación. El
resultado de esa revisión está en [integracion-modulos.md](integracion-modulos.md).

---

## Deuda técnica reconocida

Anotar la deuda es parte del ritmo sostenible: lo que no se anota se olvida y termina
convirtiéndose en una sorpresa.

| Deuda | Impacto | Cuándo abordarla |
|---|---|---|
| El listado de licitaciones carga las ofertas de cada fila para mostrar su cantidad | Bajo con los volúmenes actuales; crece con la cantidad de ofertas por licitación | Cuando el listado supere unos cientos de registros, con una agregación equivalente a `ContarOfertasAsync` |
| No hay caché del tipo de cambio activo entre peticiones | Una consulta ligera por petición | Solo si el perfilado lo señala; anticiparlo sería complejidad especulativa |
| Los mensajes de la interfaz están escritos directamente en las vistas | Ninguno mientras el sistema sea monolingüe | Si apareciera una historia de internacionalización |
| Las pruebas funcionales dependen de una instancia levantada aparte | Requiere Docker Compose antes de ejecutarlas | Aceptado: es lo que las hace verificar el despliegue real |
