# Estrategia de pruebas

## Por qué tres suites

Cada suite responde una pregunta distinta. Tener las tres no es redundancia: es que ninguna puede
responder por las otras.

| Suite | Pregunta que responde | Qué no puede verificar |
|---|---|---|
| **Unitarias** | ¿La regla de negocio hace lo correcto en todos sus casos límite? | Que la regla llegue a ejecutarse en el sistema real |
| **Integración** | ¿El sistema completo, con PostgreSQL real, se comporta según el contrato? | Que la interfaz sea usable |
| **Funcionales** | ¿Un usuario puede completar el flujo desde el navegador? | Los casos límite, uno por uno |

Las unitarias cubren la combinatoria porque son rápidas. Las funcionales cubren el camino feliz y
unos pocos rechazos representativos porque son lentas. Intentar cubrir todos los casos límite desde
el navegador produciría una suite que tarda media hora y que nadie ejecuta.

---

## Pruebas unitarias

**Proyecto:** `tests/Licitaciones.UnitTests` · **Herramientas:** xUnit, FluentAssertions, NSubstitute

Se ejercitan las capas `Domain` y `Application` sin infraestructura. No tocan disco, red ni reloj
del sistema.

### Cobertura por área

| Área | Clase de prueba | Qué verifica |
|---|---|---|
| Ciclo de estados | `PoliticaTransicionEstadoPruebas` | Las nueve combinaciones de origen y destino |
| Licitación | `LicitacionPruebas` | Creación, publicación, cierre, vencimiento, presupuesto |
| Oferta | `OfertaPruebas` | Los cuatro rechazos y los límites exactos |
| Proveedor | `ProveedorPruebas` | Normalización, caracteres permitidos, borrado lógico |
| Mejor oferta | `EvaluadorOfertasPruebas` | Selección, desempate, ahorro, clasificación |
| Aprobación | `SelectorNivelAprobacionPruebas` | Bordes de los rangos, traslapes, rango abierto |
| Moneda | `ConversorMonedaPruebas` | Fórmula, redondeo, ausencia de tipo de cambio |
| Servicios | `*ServicioPruebas` | Coordinación, unicidad, borrado físico o lógico |

### Cómo se eligieron los casos

No se probó «que funcione». Se probaron los puntos donde un error es probable y silencioso:

- **Bordes exactos.** Ahorro de exactamente 10 % y de 9,99 %. Oferta igual al presupuesto y un
  céntimo por encima. Vencimiento en el instante exacto del cierre y un segundo antes.
- **Los ejemplos literales del enunciado.** Los tres nombres de proveedor de la sección 8.3 se
  prueban tal como aparecen escritos.
- **Combinatoria completa donde es finita.** El ciclo de estados tiene nueve combinaciones; se
  prueban las nueve, no una muestra.
- **Regresiones.** Cada defecto encontrado durante el desarrollo dejó su prueba permanentemente en
  la suite.

### Sustitución de dependencias

Los repositorios se sustituyen con NSubstitute. Los **validadores no**: se usan los reales, porque
sustituirlos dejaría sin probar la conexión entre el servicio y sus reglas de entrada, que es
justamente lo que esas pruebas deben verificar.

El reloj se sustituye por `RelojFijo`. Es la razón por la que `IRelojSistema` existe: sin él, una
prueba de vencimiento pasaría o fallaría según la hora a la que se ejecute.

---

## Pruebas de integración

**Proyecto:** `tests/Licitaciones.IntegrationTests` · **Herramientas:** xUnit, Testcontainers,
`WebApplicationFactory`

Se ejecutan contra **PostgreSQL 16 real** levantado en un contenedor. El enunciado prohíbe
sustituirlo por SQLite, y con razón: los índices únicos parciales, las restricciones `CHECK`, la
columna `xmin` y el comportamiento de `timestamptz` son específicos de PostgreSQL. Una prueba
contra un motor en memoria pasaría sin verificar nada de eso.

### Qué se verifica

| Clase | Verifica |
|---|---|
| `EsquemaYSemillaPruebas` | Que las migraciones creen el esquema, que los montos sean `numeric(18,2)` y que la semilla cargue los tres rangos y el tipo de cambio |
| `RestriccionesPruebas` | Que los índices únicos, las claves foráneas y las restricciones `CHECK` rechacen lo que deben, incluso ante SQL escrito a mano |
| `ConcurrenciaYTransaccionPruebas` | Que la concurrencia optimista detecte escrituras simultáneas y que las transacciones reviertan por completo |
| `LicitacionesEndpointsPruebas` | El contrato HTTP completo: códigos, cuerpos y ProblemDetails |
| `ProveedoresYTiposCambioEndpointsPruebas` | Unicidad, activación transaccional y conversión monetaria por API |

### Aislamiento

El contenedor se comparte entre todas las clases mediante una colección de xUnit. Arrancar uno por
clase multiplicaría el tiempo sin aportar aislamiento real, porque cada prueba limpia sus propios
datos con `TRUNCATE ... CASCADE` antes de ejecutarse.

Los niveles de aprobación y el tipo de cambio semilla **no se borran**: son datos de configuración,
no datos de prueba, y varias pruebas dependen de que estén presentes.

### Una prueba que merece atención

`RestriccionesPruebas.RestriccionCheck_RechazaUnPresupuestoNoPositivoEscritoEnSql` inserta
directamente con SQL, saltándose las entidades, los servicios y los validadores. Es la comprobación
de que la tercera capa de defensa existe de verdad: aunque alguien evite todo el código de
aplicación, la base de datos rechaza el dato inválido.

---

## Pruebas funcionales de extremo a extremo

**Proyecto:** `tests/Licitaciones.FunctionalTests` · **Herramientas:** xUnit, Playwright con Chromium

A diferencia de las de integración, **no levantan la aplicación en memoria**: atacan una instancia
ya desplegada, la misma que produce `docker compose up --build`. Eso es lo que las hace verificar
el despliegue real, el HTML servido, la hoja de estilos local y los guiones del navegador.

La dirección se toma de la variable de entorno `URL_BASE_PRUEBAS`.

### Qué se verifica

| Clase | Verifica |
|---|---|
| `InterfazPruebas` | Landing page, menú, modo claro y oscuro con persistencia, conversión CRC/USD, diseño adaptable, documentación de la API |
| `FlujoCompletoPruebas` | El flujo funcional mínimo completo, los rechazos, la confirmación de eliminación y los niveles de aprobación |

### La prueba central

`FlujoCompletoPruebas.FlujoMinimo_DesdeElRegistroDelProveedorHastaLaMejorOferta` recorre los ocho
pasos del enunciado en una sola sesión de navegador: registra dos proveedores, crea y publica una
licitación, registra una oferta válida, comprueba que se rechacen la duplicada y la que supera el
presupuesto, registra una segunda oferta más baja, y verifica la mejor oferta con su porcentaje de
ahorro exacto y su nivel de aprobación.

Si esa prueba pasa, el sistema hace lo que el enunciado pide.

### Sufijos únicos

Los códigos de licitación y los nombres de proveedor son únicos. Sin un sufijo aleatorio, la
segunda ejecución de la misma prueba fallaría por duplicado en lugar de por un defecto real. Por
eso `SufijoUnico()` acompaña a todos los identificadores generados en las pruebas.

---

## Cobertura

| Ámbito | Mínimo exigido | Cómo se verifica |
|---|---|---|
| `Licitaciones.Domain` | 80 % de líneas | Comprobación automática en la integración continua |
| `Licitaciones.Application` | 80 % de líneas | Comprobación automática en la integración continua |
| Proyecto completo | 70 % de líneas | Comprobación automática en la integración continua |

El umbral **bloquea la integración**. No es un informe que alguien deba revisar: el trabajo
`pruebas-unitarias` falla si la cobertura baja de los mínimos, y el trabajo `resultado` impide que
el cambio se integre.

### Sobre el número de cobertura

La cobertura mide qué líneas se ejecutaron, no si se verificó algo útil. Una suite que ejecute todo
el código sin comprobar ningún resultado alcanzaría el 100 %. Por eso el enunciado advierte que la
cobertura numérica no sustituye la calidad de los escenarios probados.

En este proyecto el número es alto porque los escenarios son los del enunciado, no al revés: no se
escribieron pruebas para subir la métrica.

---

## Ejecución

### Todas las pruebas que no requieren la aplicación levantada

```bash
dotnet test tests/Licitaciones.UnitTests
dotnet test tests/Licitaciones.IntegrationTests   # requiere Docker
```

### Con informe de cobertura

```bash
dotnet test tests/Licitaciones.UnitTests \
  --collect:"XPlat Code Coverage" \
  --results-directory ./resultados

dotnet tool install --global dotnet-reportgenerator-globaltool

reportgenerator \
  -reports:"./resultados/**/coverage.cobertura.xml" \
  -targetdir:"./resultados/cobertura" \
  -reporttypes:"Html;TextSummary"

# El informe queda en ./resultados/cobertura/index.html
```

### Funcionales

```bash
# 1. Levantar la solución
cp .env.example .env
docker compose up --detach --wait

# 2. Instalar los navegadores de Playwright, una sola vez
dotnet build tests/Licitaciones.FunctionalTests
pwsh tests/Licitaciones.FunctionalTests/bin/Debug/net9.0/playwright.ps1 install --with-deps chromium

# 3. Ejecutar
URL_BASE_PRUEBAS=http://localhost:8080 dotnet test tests/Licitaciones.FunctionalTests
```

### Ver el navegador durante la ejecución

Para depurar una prueba funcional, cambie `Headless = true` por `false` en
`PruebaNavegadorBase.InitializeAsync`.

---

## Ejecución en la integración continua

| Trabajo | Qué ejecuta |
|---|---|
| `calidad` | Formato del código y compilación con analizadores en modo estricto |
| `pruebas-unitarias` | Suite unitaria, informe de cobertura y verificación de umbrales |
| `pruebas-integracion` | Suite de integración con PostgreSQL levantado por Testcontainers |
| `imagen-docker` | Construcción de la imagen |
| `pruebas-funcionales` | Levanta la solución con Compose y ejecuta Playwright |
| `manifiestos-kubernetes` | Valida los manifiestos contra el esquema de Kubernetes |
| `dependencias` | Busca paquetes con vulnerabilidades conocidas |
| `resultado` | Falla si cualquiera de los anteriores falló |

`resultado` es el trabajo marcado como comprobación obligatoria en la rama protegida. Su única
función es concentrar el veredicto: si algo falló, el cambio no se integra.

---

## Convenciones

**Nombres.** `Metodo_Escenario_ResultadoEsperado`, en español. Por ejemplo:
`Registrar_ConMontoSuperiorAlPresupuesto_Falla`. El nombre debe permitir entender qué se rompió
leyendo solo el informe de fallos.

**Estructura.** Preparar, actuar, verificar, separados por líneas en blanco. Sin comentarios que
etiqueten las secciones: la estructura ya es visible.

**Una razón para fallar.** Cada prueba verifica un comportamiento. Si una prueba puede fallar por
dos motivos distintos, son dos pruebas.

**Comentarios donde aportan.** Se comenta el *porqué* de un caso límite, no el *qué*. La prueba del
instante exacto del cierre explica que la sección 8.2 dice «iguales o posteriores»; sin ese
comentario, alguien podría pensar que el `>=` es un error y relajarlo a `>`.
