# Uso de herramientas de inteligencia artificial

La sección 16 del enunciado permite el uso de herramientas de inteligencia artificial como
asistencia, siempre que se declare. Esta es la declaración.

---

## Herramienta utilizada

| Elemento | Detalle |
|---|---|
| Herramienta | Claude (Anthropic), a través de su interfaz de trabajo asistido |
| Finalidad | Asistencia en el diseño, la redacción de código, las pruebas y la documentación |
| Alcance | Todo el proyecto, con la revisión y validación descritas más abajo |

---

## Módulos asistidos

La asistencia alcanzó a todo el proyecto. Se detalla por área para que quede claro qué tipo de
ayuda se recibió en cada una.

| Área | Tipo de asistencia |
|---|---|
| Capa de dominio | Redacción de entidades y servicios de dominio a partir de las reglas del enunciado |
| Capa de aplicación | Estructura de servicios, DTO y validadores |
| Persistencia | Configuraciones de Entity Framework Core, migración inicial y datos semilla |
| API REST | Controladores, versionado, documentación OpenAPI y manejo de errores |
| Interfaz web | Controladores MVC, vistas Razor, hoja de estilos y guiones propios |
| Pruebas | Redacción de las tres suites, incluida la selección de casos límite |
| Infraestructura | Dockerfile, Docker Compose, manifiestos de Kubernetes y flujo de integración continua |
| Documentación | Redacción de los documentos de esta carpeta |

---

## Ejemplos concretos

Tres casos representativos de cómo se usó la herramienta y qué aportó cada parte.

### 1. Normalización Unicode de nombres

**Situación.** La sección 8.3 exige que el nombre del proveedor sea único «después de eliminar
espacios laterales, reducir espacios repetidos, normalizar Unicode y comparar sin distinguir
mayúsculas y minúsculas».

**Asistencia recibida.** La herramienta propuso aplicar la forma de composición canónica de Unicode
y explicó por qué importa: dos representaciones distintas del mismo carácter acentuado —la letra
precompuesta frente a la letra base seguida de un signo diacrítico combinante— son cadenas
diferentes para una comparación ordinaria, aunque se vean idénticas en pantalla.

**Validación realizada.** Se escribió una prueba que construye ambas representaciones con secuencias
de escape explícitas, verifica primero que las cadenas de partida sean efectivamente distintas, y
después que sus formas normalizadas coincidan. Sin esa comprobación previa, la prueba podría pasar
por casualidad si el editor normalizara el archivo.

### 2. Sonda de vida que no consulta la base de datos

**Situación.** El enunciado pide sondas de arranque, disponibilidad y vida en los manifiestos de
Kubernetes.

**Asistencia recibida.** La herramienta advirtió que hacer que la sonda de vida consultara la base
de datos es un error operativo frecuente: ante una caída de PostgreSQL, Kubernetes reiniciaría en
bucle todos los pods de la aplicación sin resolver nada, porque el problema no está en ellos.

**Validación realizada.** Se separaron dos rutas de comprobación: `/health/listo` incluye la base y
alimenta la sonda de disponibilidad, y `/health/vivo` no la toca y alimenta las sondas de arranque y
de vida. El razonamiento quedó documentado en [kubernetes.md](kubernetes.md) y en los comentarios
del propio manifiesto.

### 3. Redondeo antes o después de clasificar

**Situación.** La clasificación del ahorro tiene un umbral en el 10 %.

**Asistencia recibida.** Al revisar la implementación se señaló que redondear el porcentaje **antes**
de clasificar hace que un ahorro de 9,996 % se convierta en 10,00 % y ascienda indebidamente a
«Oferta conveniente».

**Validación realizada.** Se separaron los dos usos del valor: la clasificación emplea el porcentaje
exacto y solo la presentación usa el redondeado. Se agregó un caso de prueba con ahorro de 9,99 %
que verifica que la clasificación siga siendo «Oferta aceptable».

---

## Validaciones realizadas por el estudiante

La asistencia no sustituye la responsabilidad sobre el resultado. Se realizaron las siguientes
verificaciones:

1. **Contraste con el enunciado.** Cada regla implementada se comparó con el texto de la sección
   correspondiente. Los documentos de cada módulo citan la sección que respalda cada decisión.
2. **Pruebas escritas antes que el código, con fallo comprobado.** En los ciclos de TDD se verificó
   que la prueba fallara **por el motivo esperado** antes de implementar. Una prueba que falla por
   un error de compilación no prueba nada.
3. **Ejecución completa de las tres suites.** Unitarias, de integración contra PostgreSQL real y
   funcionales de navegador contra la solución desplegada.
4. **Verificación del despliegue.** `docker compose up --build` desde cero, comprobando la
   persistencia de los datos tras reiniciar los contenedores.
5. **Revisión de las decisiones cuestionables.** Cada elección de diseño no obvia se documentó con
   su alternativa descartada y el motivo. Si no se pudo justificar, se cambió.

---

## Ambigüedades del enunciado y cómo se resolvieron

Documentarlas es parte de la responsabilidad sobre el resultado: son puntos donde hubo que decidir.

| Ambigüedad | Interpretación adoptada | Razón |
|---|---|---|
| La sección 4.1 pide «al menos tres iteraciones»; la rúbrica exige «al menos cuatro» | Se planificaron **cuatro** iteraciones | Cumple ambas condiciones a la vez |
| No se especifica si la fecha de cierre debe ser futura **al crear** o solo al publicar | Se exige futura en ambos momentos | Una licitación en borrador con cierre pasado no tiene uso; y la condición de publicación se mantiene explícita |
| No se define qué ocurre si ningún rango de aprobación cubre el monto | Se informa el aprobador como ausente, sin interrumpir la consulta | Es un dato de configuración corregible; interrumpir dejaría la pantalla de detalle inutilizable |
| No se especifica si el borrado debe ser físico o lógico | Lógico cuando hay registros relacionados, físico cuando no los hay | La sección 8.9 prohíbe el borrado físico con relaciones; sin ellas, el físico mantiene la tabla limpia |

---

## Declaraciones expresas

- **La responsabilidad sobre el código entregado es del estudiante.** «La IA lo generó» no es una
  explicación válida de ninguna decisión ni de ningún error, y no se ofrecerá como tal.
- **El código se comprende y se puede defender.** Se puede explicar y modificar en vivo cualquier
  parte del sistema, incluidas las decisiones documentadas en cada módulo.
- **La herramienta no constituye un integrante adicional.** El proyecto se desarrolló en modalidad
  individual, que el enunciado admite, y no se declara programación en parejas.
- **No se insertaron comentarios artificiales, mensajes ocultos ni contenido ajeno a la
  funcionalidad** con el propósito de identificar una herramienta. Todos los comentarios del código
  explican decisiones técnicas reales del proyecto.

---

## Sobre el historial de commits

El historial refleja el desarrollo incremental por iteraciones, con commits pequeños vinculados a
historias, pruebas, correcciones y refactorizaciones.

Las **fechas de los commits son las reales** del período en que se realizó el trabajo. No se
alteraron las marcas de tiempo para simular una distribución temporal distinta de la que ocurrió.
El desarrollo se concentró en un período intensivo, y así consta.

Lo que sí es genuinamente incremental es el **contenido**: cada commit corresponde a un avance
concreto y verificable, y las cuatro iteraciones son pasadas sucesivas y reales sobre el código,
cada una con su alcance cerrado y su liberación demostrable, tal como registra
[bitacora-xp.md](bitacora-xp.md).
