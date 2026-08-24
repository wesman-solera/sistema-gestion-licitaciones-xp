# Módulo: Persistencia

## Propósito

Implementa el acceso a datos sobre PostgreSQL con Entity Framework Core, y actúa como **última
línea de defensa** de las reglas de integridad del sistema.

## Responsabilidades

- Mapear las entidades del dominio a tablas
- Implementar los puertos de repositorio que define la capa de aplicación
- Coordinar transacciones y concurrencia optimista
- Aplicar migraciones versionadas y datos semilla al arrancar

## Lo que no hace

- No contiene reglas de negocio
- No conoce HTTP ni la interfaz de usuario

---

## Persistencia exclusiva con PostgreSQL

La sección 11 del enunciado es explícita: *«Persistencia exclusiva con PostgreSQL local; SQLite no
puede sustituirlo en la aplicación ni en las pruebas de integración»*.

La razón es concreta y se nota en varias decisiones de este módulo:

| Característica usada | Existe en SQLite |
|---|---|
| Índice único parcial con `WHERE` | No |
| Columna de sistema `xmin` para concurrencia | No |
| `numeric(18,2)` con precisión exacta | No de la misma forma |
| `timestamptz` con semántica de zona horaria | No |
| `ILIKE` para búsqueda sin distinguir mayúsculas | No |
| Códigos SQLSTATE diferenciados por tipo de violación | Parcialmente |

Probar contra SQLite habría dado una falsa sensación de cobertura: las pruebas pasarían sin
verificar ninguna de esas garantías.

---

## Mapeo explícito

Los nombres de tabla y columna se declaran uno por uno en snake_case, dentro de una clase de
configuración por entidad.

**Por qué no se usa la convención por defecto.** Depender de ella significa que un cambio de versión
del proveedor podría renombrar columnas de forma silenciosa, o que renombrar una propiedad en C#
rompería la base sin previo aviso. Con el mapeo explícito, la relación entre modelo y esquema es un
contrato escrito.

---

## Concurrencia optimista con `xmin`

`xmin` es una columna de sistema que PostgreSQL mantiene en toda tabla y actualiza automáticamente
en cada `UPDATE`.

```csharp
builder.Property(l => l.Version)
    .HasColumnName("xmin")
    .HasColumnType("xid")
    .ValueGeneratedOnAddOrUpdate()
    .IsConcurrencyToken();
```

**Ventaja sobre una columna propia.** Una columna `version` que hubiera que incrementar a mano
requeriría acordarse de hacerlo en cada camino de escritura. Olvidarlo en uno solo desactivaría la
protección en silencio, y nadie lo notaría hasta perder datos.

**Consecuencia en las migraciones.** La columna **no se crea** en el DDL, porque ya existe en toda
tabla de PostgreSQL. El modelo la mapea, pero la migración inicial no la declara.

### Qué ocurre ante un conflicto

Si dos usuarios cargan el mismo registro y ambos lo guardan, el segundo recibe
`DbUpdateConcurrencyException`, que el manejador global traduce a `409 Conflict` con código
`GEN-003`. El cambio del primero no se pierde en silencio, que es exactamente el fallo que esta
protección evita.

---

## Unidad de trabajo

Los repositorios no guardan por su cuenta: acumulan cambios y `UnidadTrabajo` los confirma en un
solo punto. Eso permite que una operación que toca varias entidades quede en una sola transacción.

```csharp
public async Task<T> EnTransaccionAsync<T>(
    Func<CancellationToken, Task<T>> operacion,
    CancellationToken cancelacion = default)
{
    if (_contexto.Database.CurrentTransaction is not null)
    {
        return await operacion(cancelacion);   // reutiliza la transacción existente
    }
    ...
}
```

La comprobación de transacción existente evita anidar, que PostgreSQL no admite. Es lo que permite
que una prueba de integración envuelva la operación en su propia transacción sin que el servicio
falle.

---

## Restricciones en la base de datos

Cada regla de negocio con expresión declarativa tiene su respaldo en el esquema:

| Regla | Restricción |
|---|---|
| Presupuesto positivo | `ck_licitaciones_presupuesto_positivo` |
| Estado válido | `ck_licitaciones_estado_valido` |
| Monto de oferta positivo | `ck_ofertas_monto_positivo` |
| Rango de aprobación coherente | `ck_niveles_aprobacion_rango_coherente` |
| Tipo de cambio positivo | `ck_tipos_cambio_valor_positivo` |
| Código de licitación único | `ux_licitaciones_codigo_normalizado` |
| Nombre de proveedor único | `ux_proveedores_nombre_normalizado` |
| Una oferta por proveedor y licitación | `ux_ofertas_licitacion_proveedor` |
| Un solo tipo de cambio activo | `ux_tipos_cambio_unico_activo` (parcial) |
| Ofertas no huérfanas | Claves foráneas con `ON DELETE RESTRICT` |

La prueba `RestriccionesPruebas.RestriccionCheck_RechazaUnPresupuestoNoPositivoEscritoEnSql` inserta
directamente con SQL, saltándose entidades, servicios y validadores, para comprobar que esta capa de
defensa existe de verdad.

---

## Traducción de errores de PostgreSQL

Los errores del motor **nunca llegan al cliente**: contienen nombres de tabla, de índice y a veces
fragmentos de la consulta. El manejador global los traduce por código SQLSTATE:

| SQLSTATE | Significado | Traducción |
|---|---|---|
| `23505` | Violación de restricción única | 409, con el código deducido del nombre del índice |
| `23503` | Violación de clave foránea | 422 con `GEN-002` |
| `23514` | Violación de restricción `CHECK` | 422 con `GEN-001` |

La deducción del código a partir del nombre del índice es lo que hace que el cliente reciba el mismo
`LIC-001` tanto si la duplicación la detectó la aplicación como si la detectó la base de datos.

---

## Migraciones

| Migración | Contenido |
|---|---|
| `20260818120000_InicialEsquemaLicitaciones` | Esquema completo, restricciones, índices y semilla |

### Aplicación automática al arrancar

`IniciadorBaseDatos.MigrarAsync` aplica las migraciones pendientes con reintentos de espera
creciente:

```csharp
for (int intento = 1; intento <= IntentosMaximos; intento++)
{
    try
    {
        await contexto.Database.MigrateAsync(cancelacion);
        return;
    }
    catch (Exception excepcion) when (intento < IntentosMaximos)
    {
        await Task.Delay(TimeSpan.FromSeconds(EsperaBaseSegundos * intento), cancelacion);
    }
}
```

En Docker Compose y en Kubernetes el contenedor de la aplicación puede arrancar antes de que
PostgreSQL acepte conexiones. El reintento es lo que permite que `docker compose up --build`
funcione sin pasos manuales intermedios.

El total de espera acumulada es de unos 56 segundos repartidos en 8 intentos, suficiente para el
arranque inicial de un cluster de PostgreSQL vacío.

### Orden de arranque

`Program.Main` abre el puerto antes de migrar:

```csharp
await aplicacion.StartAsync();
await IniciadorBaseDatos.MigrarAsync(aplicacion.Services);
await aplicacion.WaitForShutdownAsync();
```

El orden no es cosmético. Si se migrara antes de escuchar, el proceso pasaría su ventana de
arranque sin contestar la sonda de vida y el orquestador lo declararía enfermo aunque estuviera
trabajando con normalidad. Con este orden, `/health/vivo` responde desde el primer segundo y
`/health/listo` es el que se mantiene en rojo hasta que `GetPendingMigrationsAsync` devuelve una
lista vacía: mientras el esquema no esté completo, el pod no recibe tráfico.

### Comandos de migración

```bash
# Crear una migración nueva
dotnet ef migrations add NombreDescriptivo \
  --project src/Licitaciones.Infrastructure \
  --startup-project src/Licitaciones.Web \
  --output-dir Migraciones

# Aplicar manualmente
dotnet ef database update \
  --project src/Licitaciones.Infrastructure \
  --startup-project src/Licitaciones.Web

# Generar el script SQL sin aplicarlo
dotnet ef migrations script \
  --project src/Licitaciones.Infrastructure \
  --startup-project src/Licitaciones.Web \
  --output migracion.sql
```

`FabricaContextoDisenio` permite que estos comandos funcionen sin arrancar la aplicación web. Toma
la cadena de conexión de la variable de entorno `ConnectionStrings__Licitaciones` y solo cae a un
valor local de desarrollo si no está definida.

---

## Consultas y rendimiento

### Consultas divididas

Las consultas que incluyen colecciones usan `AsSplitQuery`. Un `JOIN` duplicaría las columnas de la
licitación por cada oferta asociada, multiplicando el volumen transferido sin aportar información.

### Agregaciones en lugar de carga completa

| Necesidad | Implementación | Alternativa descartada |
|---|---|---|
| Oferta más alta de una licitación | `MaxAsync` sobre `decimal?` | Cargar todas las ofertas y recorrerlas |
| Cantidad de ofertas por proveedor | `GROUP BY` para toda la página | Cargar la colección de cada proveedor |
| Existencia de ofertas | `AnyAsync` | `CountAsync() > 0` |

`MaxAsync` sobre `decimal?` devuelve nulo si no hay filas, en lugar de lanzar excepción. Es lo que
permite que la regla del presupuesto no reducible funcione también cuando la licitación no tiene
ofertas.

### Ordenamiento con lista cerrada

Los listados aceptan un campo de ordenamiento del cliente, pero solo lo mapean contra una lista
cerrada de valores conocidos:

```csharp
return (parametros.OrdenarPor?.ToLowerInvariant()) switch
{
    "codigo" => ...,
    "titulo" => ...,
    _ => consulta.OrderByDescending(l => l.CreatedAt)
};
```

Aceptar un nombre de columna arbitrario abriría la puerta a construir expresiones no previstas.

### Búsqueda con `ILIKE`

Se usa `EF.Functions.ILike`, que se traduce al operador `ILIKE` de PostgreSQL. La alternativa
—traer las filas y filtrarlas con `ToLower()` en memoria— cargaría la tabla completa en cada
búsqueda.

---

## Pruebas

| Prueba | Verifica |
|---|---|
| `EsquemaYSemillaPruebas.Migraciones_SeAplicanSinDejarPendientes` | Migraciones completas |
| `EsquemaYSemillaPruebas.ColumnasMonetarias_UsanNumeric*` | Tipo exacto de las columnas monetarias |
| `EsquemaYSemillaPruebas.IndicesUnicos_ExistenEnLaBaseDeDatos` | Presencia de los índices |
| `RestriccionesPruebas.*` | Que cada restricción rechace lo que debe |
| `ConcurrenciaYTransaccionPruebas.ConcurrenciaOptimista_*` | Detección de escrituras simultáneas |
| `ConcurrenciaYTransaccionPruebas.Transaccion_RevierteTodo*` | Reversión completa ante fallo |
| `RestriccionesPruebas.Montos_ConservanLaPrecisionDecimal*` | Precisión decimal exacta |

---

## Archivos

| Archivo | Contenido |
|---|---|
| `Persistencia/LicitacionesDbContext.cs` | Contexto de datos |
| `Persistencia/Configuraciones/*.cs` | Mapeo de cada entidad |
| `Persistencia/UnidadTrabajo.cs` | Confirmación y transacciones |
| `Persistencia/IniciadorBaseDatos.cs` | Migración con reintentos |
| `Persistencia/FabricaContextoDisenio.cs` | Contexto para las herramientas de línea de comandos |
| `Repositorios/*.cs` | Implementación de los puertos |
| `Migraciones/*.cs` | Migración inicial y snapshot del modelo |
