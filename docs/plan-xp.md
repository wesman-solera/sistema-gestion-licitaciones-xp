# Plan XP

Este documento recoge el Planning Game, el plan de liberación, el plan de cada iteración y las
reglas de trabajo. **Extreme Programming es el único marco del proyecto.** No se usan roles,
ceremonias ni artefactos de Scrum o Kanban, ni terminología de esos marcos.

## Terminología

Se usa deliberadamente el vocabulario de XP:

| Se usa | No se usa |
|---|---|
| Historia de usuario | Elemento del backlog |
| Planning Game | Sprint Planning |
| Iteración | Sprint |
| Plan de liberación | Release backlog |
| Pequeña liberación | Incremento de sprint |
| Velocidad | Velocidad de sprint |
| Cliente, programador | Product Owner, Scrum Master, equipo Scrum |

---

## Planning Game

El Planning Game es la conversación entre el cliente y el programador que decide **qué** se
construye y **cuándo**. El cliente aporta el valor y la prioridad; el programador aporta la
estimación y la capacidad. Ninguno invade el terreno del otro.

En este proyecto, el papel del cliente lo cumple el enunciado del curso, que fija el alcance y las
reglas de aceptación. La prioridad se derivó de una pregunta concreta: *¿qué hace falta para que
el flujo funcional mínimo de la sección 5.3 se pueda recorrer de principio a fin?*

### Reglas aplicadas

1. **El cliente prioriza, el programador estima.** Ninguna historia se estimó bajando el número
   para que cupiera en la iteración.
2. **La historia es una conversación, no un contrato.** Los criterios de aceptación fijan el
   resultado esperado, no la implementación.
3. **Si una historia no cabe, se divide.** Las historias de más de 5 puntos se partieron. El caso
   más claro fue el registro de ofertas, que se separó en registro válido (H-10) y los tres
   rechazos (H-11, H-12, H-13), cada uno con su propia prueba.
4. **La velocidad observada manda sobre la deseada.** El plan de la iteración siguiente se ajusta
   con lo que realmente se completó, no con lo que se esperaba completar.

---

## Plan de liberación

El plan de liberación reparte las 26 historias en cuatro iteraciones de duración uniforme. Cada
iteración cierra con una **pequeña liberación**: una versión ejecutable y demostrable, no una
rama a medio terminar.

| Iteración | Tema | Historias | Puntos | Liberación al cierre |
|---|---|---|---|---|
| 1 | Fundamentos y proveedores | H-01 … H-06 | 16 | CRUD de proveedores y alta de licitaciones funcionando sobre PostgreSQL |
| 2 | Ciclo de vida y ofertas | H-07 … H-13 | 22 | Flujo publicar → ofertar → rechazar operativo |
| 3 | Evaluación, aprobación y moneda | H-14 … H-20 | 25 | Mejor oferta, clasificación, aprobador y conversión CRC/USD |
| 4 | Experiencia, API y despliegue | H-21 … H-26 | 19 | Sistema completo desplegable con un comando |

**Total: 82 puntos en 4 iteraciones.**

### Criterio de orden

El orden no es arbitrario. Responde a tres reglas:

1. **Primero lo que otras historias necesitan.** No tiene sentido registrar ofertas antes de tener
   proveedores y licitaciones.
2. **Antes el riesgo técnico que la comodidad.** La normalización de nombres y el ciclo de estados
   se atacaron temprano porque son las reglas donde un error se propaga a todo lo demás. La
   landing page y el modo oscuro, que son aislados y de bajo riesgo, se dejaron para el final.
3. **Cada iteración produce algo demostrable.** Ninguna termina con «falta la mitad para que se
   pueda ver».

---

## Plan de cada iteración

### Iteración 1 — Fundamentos y proveedores

**Objetivo:** que exista una base de datos real con dos entidades que se puedan crear, consultar,
editar y eliminar respetando la unicidad.

| Historia | Puntos | Foco técnico |
|---|---|---|
| H-01 | 3 | Entidad `Proveedor`, factoría con validación |
| H-02 | 3 | `NormalizadorTexto`, columna normalizada, índice único |
| H-03 | 2 | Expresión regular de caracteres permitidos |
| H-04 | 3 | Repositorio, servicio de aplicación, vistas CRUD |
| H-05 | 3 | Entidad `Licitacion`, código normalizado |
| H-06 | 2 | Control de calendario, conversión UTC ↔ America/Costa_Rica |

**Riesgo identificado:** la normalización Unicode. Dos representaciones del mismo carácter
acentuado producirían formas normalizadas distintas y el índice único no las detectaría como
duplicadas. Se mitigó aplicando la forma de composición canónica y cubriéndolo con una prueba
específica.

---

### Iteración 2 — Ciclo de vida y ofertas

**Objetivo:** que el flujo publicar → ofertar funcione y que los tres rechazos del enunciado se
comporten exactamente como se especifica.

| Historia | Puntos | Foco técnico |
|---|---|---|
| H-07 | 3 | `Licitacion.Publicar` con sus precondiciones |
| H-08 | 3 | `PoliticaTransicionEstado` como tabla de datos |
| H-09 | 3 | `EstaCerradaFuncionalmente`, reloj inyectable |
| H-10 | 5 | Entidad `Oferta`, factoría `Registrar` |
| H-11 | 3 | Índice único compuesto y comprobación previa |
| H-12 | 2 | Validación contra presupuesto |
| H-13 | 3 | Comparación de vencimiento con límite inclusivo |

**Riesgo identificado:** las pruebas de vencimiento dependerían de la hora real de ejecución y
serían intermitentes. Se mitigó introduciendo `IRelojSistema` desde el primer día de la iteración
y prohibiendo el uso directo de `DateTimeOffset.UtcNow` fuera de su implementación.

---

### Iteración 3 — Evaluación, aprobación y moneda

**Objetivo:** que el sistema no solo registre datos, sino que produzca la información que sustenta
la decisión de compra.

| Historia | Puntos | Foco técnico |
|---|---|---|
| H-14 | 3 | Regla de presupuesto no reducible |
| H-15 | 5 | `EvaluadorOfertas`, desempate y clasificación |
| H-16 | 5 | `SelectorNivelAprobacion` recorriendo la tabla |
| H-17 | 3 | Validación de traslapes sobre el conjunto completo |
| H-18 | 3 | Activación transaccional, índice único parcial |
| H-19 | 3 | `ConversorMoneda`, `ContextoMoneda` por petición |
| H-20 | 3 | Inmutabilidad de ofertas cerradas, borrado lógico |

**Riesgo identificado:** el aprobador podría implementarse como una cadena de condiciones, que es
justo lo que el enunciado prohíbe. Se mitigó escribiendo primero la prueba que recorre los bordes
de los tres rangos con datos de la tabla, de modo que una implementación con umbrales fijos en el
código no habría podido pasarla sin duplicar esos umbrales de forma evidente.

---

### Iteración 4 — Experiencia, API y despliegue

**Objetivo:** que el sistema sea usable, integrable y desplegable.

| Historia | Puntos | Foco técnico |
|---|---|---|
| H-21 | 2 | Landing page |
| H-22 | 2 | Tema resuelto en el servidor mediante cookie |
| H-23 | 5 | API versionada, OpenAPI, paginación |
| H-24 | 3 | Manejador global de excepciones, ProblemDetails |
| H-25 | 3 | Paginación, filtro y orden en los listados |
| H-26 | 5 | Dockerfile, Compose, manifiestos, integración continua |

**Riesgo identificado:** depender de una CDN para los recursos del front-end dejaría la interfaz
inutilizable sin Internet, lo que el requisito 9 prohíbe expresamente. Se mitigó escribiendo hoja
de estilos y guiones propios, sin ningún recurso externo.

---

## Prácticas XP y cómo se aplican

| Práctica | Aplicación concreta en este proyecto |
|---|---|
| **Planning Game** | Historias con prioridad y estimación en [historias-usuario.md](historias-usuario.md); plan de liberación e iteraciones en este documento |
| **Historias de usuario** | 26 historias con criterios verificables y vínculo explícito a sus pruebas |
| **Iteraciones cortas** | Cuatro iteraciones de duración uniforme, cada una con alcance cerrado |
| **Pequeñas liberaciones** | Cada iteración cierra con una versión ejecutable y demostrable |
| **TDD** | Ciclo rojo → verde → refactorización. La prueba se escribe antes de la regla y se comprueba que falle por el motivo correcto |
| **Diseño simple** | Se implementa lo que las historias vigentes piden. El apartado «fuera del alcance» de [vision-alcance.md](vision-alcance.md) documenta lo que se decidió no construir |
| **Refactorización** | Mejora continua de estructura sin cambiar comportamiento observable, respaldada por las pruebas. Registrada por iteración en [bitacora-xp.md](bitacora-xp.md) |
| **Integración continua** | Flujo de GitHub Actions que compila, prueba, mide cobertura, construye la imagen, valida manifiestos y revisa dependencias |
| **Estándares de código** | `.editorconfig` con reglas de formato y nomenclatura, verificadas por `dotnet format` en la integración continua |
| **Propiedad colectiva** | En modalidad individual se traduce en que ninguna parte del código queda fuera del alcance de la revisión: todo módulo tiene documentación propia y pruebas |
| **Ritmo sostenible** | El trabajo se distribuyó por iteraciones con alcance cerrado, sin acumular el grueso al final |
| **Pruebas de aceptación** | Cada historia declara criterios verificables, cubiertos por pruebas unitarias, de integración o funcionales |
| **Cliente disponible** | El enunciado del curso cumple ese papel; las dudas de interpretación se resolvieron documentándolas explícitamente en cada módulo |
| **Metáfora** | El sistema se describe con el vocabulario del dominio de compras públicas: licitación, oferta, adjudicación, aprobador. Ese vocabulario se usa también en los nombres del código |

### Programación en parejas

El proyecto se desarrolla en **modalidad individual**, que el enunciado admite. La programación en
parejas no aplica. En su lugar se refuerzan las prácticas compatibles con esa modalidad:

- **Revisión propia diferida:** cada bloque de trabajo se relee antes de integrarse, con el diseño
  ya asentado en lugar de en caliente.
- **TDD como sustituto parcial del segundo par de ojos:** la prueba escrita antes obliga a
  explicitar el comportamiento esperado, que es una de las funciones que cumple el compañero al
  cuestionar el diseño.
- **Integración continua estricta:** el flujo automatizado es lo que revisa cada cambio, y bloquea
  la integración cuando algo falla.

---

## Definición de terminado

Una historia está terminada cuando cumple **todas** estas condiciones:

1. Sus criterios de aceptación se satisfacen.
2. Existen pruebas automatizadas que los verifican.
3. La suite completa pasa.
4. El código cumple los estándares y compila sin advertencias evitables.
5. La documentación del módulo afectado está actualizada.
6. Los commits están vinculados a la historia.
7. La integración continua termina satisfactoriamente.

---

## Gestión de defectos

Un defecto encontrado durante una iteración se trata así:

1. Se escribe primero una prueba que lo reproduce y falla.
2. Se corrige el código hasta que la prueba pasa.
3. La prueba queda permanentemente en la suite, de modo que el defecto no puede reaparecer sin que
   la integración continua lo detecte.

Nunca se corrige un defecto sin dejar antes la prueba que lo captura. Un defecto corregido sin
prueba es un defecto que va a volver.
