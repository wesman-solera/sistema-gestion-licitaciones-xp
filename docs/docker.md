# Docker y Docker Compose

## Puesta en marcha

```bash
cp .env.example .env
# Edite .env y defina POSTGRES_PASSWORD
docker compose up --build
```

Eso es todo. La aplicación aplica sus propias migraciones al arrancar y reintenta mientras
PostgreSQL termina de aceptar conexiones, de modo que no hay pasos manuales intermedios.

| Recurso | Dirección |
|---|---|
| Interfaz web | <http://localhost:8080> |
| Documentación interactiva | <http://localhost:8080/swagger> |
| Sonda de vida | <http://localhost:8080/health/vivo> |
| Sonda de disponibilidad | <http://localhost:8080/health/listo> |
| PostgreSQL | `localhost:5432` |

---

## Variables de entorno

Se definen en `.env`, que **no se versiona**. La plantilla `.env.example` sí se versiona y documenta
cada variable.

| Variable | Descripción | Ejemplo |
|---|---|---|
| `POSTGRES_DB` | Nombre de la base de datos | `licitaciones` |
| `POSTGRES_USER` | Usuario de la base | `licitaciones_app` |
| `POSTGRES_PASSWORD` | Clave del usuario. **Obligatoria** | — |
| `ConnectionStrings__Licitaciones` | Cadena de conexión de la aplicación | Ver plantilla |
| `ASPNETCORE_ENVIRONMENT` | Entorno de ejecución | `Production` |
| `ASPNETCORE_HTTP_PORTS` | Puerto de escucha | `8080` |

`POSTGRES_PASSWORD` no tiene valor por defecto. Compose falla con un mensaje explícito si no está
definida, en lugar de arrancar con una clave conocida.

---

## El Dockerfile

Construcción en dos etapas.

### Etapa de compilación

Parte de `mcr.microsoft.com/dotnet/sdk:9.0`. Copia **primero los archivos de proyecto** y restaura
las dependencias, y solo después copia el código fuente.

Ese orden no es casual: Docker cachea cada capa, y un cambio en el código que no toque las
referencias reutiliza la capa de restauración. Copiar todo de una vez invalidaría el caché en cada
compilación y volvería a descargar los paquetes cada vez.

### Etapa de ejecución

Parte de `mcr.microsoft.com/dotnet/aspnet:9.0`, que solo trae el runtime. El SDK completo queda
descartado: la imagen final es sustancialmente más pequeña y expone menos superficie.

Se instalan dos paquetes:

- **`tzdata`** — necesario para resolver `America/Costa_Rica`. Sin él, la conversión de UTC a hora
  local caería al valor de reserva y las fechas se mostrarían desplazadas.
- **`curl`** — lo usa la comprobación de salud del contenedor.

### Usuario sin privilegios

La imagen base define el usuario `app` (UID 1654) y el contenedor se ejecuta como ese usuario. Un
proceso que corre como `root` dentro del contenedor amplía innecesariamente el daño posible ante
una vulnerabilidad de la aplicación.

### Comprobación de salud

```dockerfile
HEALTHCHECK --interval=20s --timeout=5s --start-period=40s --retries=5 \
    CMD curl --fail --silent http://localhost:8080/health/vivo || exit 1
```

Apunta a `/health/vivo`, que **no consulta la base de datos**. Es deliberado: si la comprobación
dependiera de PostgreSQL, una caída de la base marcaría como no saludable al contenedor de la
aplicación y provocaría reinicios en bucle que no arreglarían nada.

El `start-period` de 40 segundos da margen a que se apliquen las migraciones antes de que los
fallos empiecen a contar.

---

## El archivo Compose

### Servicio `postgres`

- Imagen `postgres:16-alpine`
- Volumen con nombre `licitaciones_datos_postgres`, de modo que los datos sobreviven a
  `docker compose down` y al reinicio de los contenedores
- Comprobación de salud con `pg_isready`
- Configuración regional fijada en la inicialización, para que los ordenamientos de texto sean
  estables entre entornos

### Servicio `aplicacion`

- Se construye desde el `Dockerfile` del repositorio
- Depende de `postgres` con `condition: service_healthy`. No basta con que el contenedor exista:
  debe estar aceptando conexiones
- Recibe la cadena de conexión por variable de entorno; el repositorio no contiene credenciales

---

## Persistencia de datos

La demostración que pide el enunciado:

```bash
# 1. Levantar y crear datos desde la interfaz
docker compose up --detach --wait

# 2. Verificar que hay datos
docker compose exec postgres \
  psql -U licitaciones_app -d licitaciones \
  -c "SELECT codigo, titulo, estado FROM licitaciones;"

# 3. Detener los contenedores (sin borrar volúmenes)
docker compose down

# 4. Levantar de nuevo
docker compose up --detach --wait

# 5. Los datos siguen ahí
docker compose exec postgres \
  psql -U licitaciones_app -d licitaciones \
  -c "SELECT codigo, titulo, estado FROM licitaciones;"
```

Para empezar de cero, incluyendo el borrado del volumen:

```bash
docker compose down --volumes
```

---

## Comandos útiles

```bash
# Levantar en segundo plano y esperar a que esté saludable
docker compose up --build --detach --wait

# Ver el estado y la salud de los servicios
docker compose ps

# Seguir los registros de la aplicación
docker compose logs --follow aplicacion

# Abrir una consola de PostgreSQL
docker compose exec postgres psql -U licitaciones_app -d licitaciones

# Reconstruir solo la aplicación
docker compose up --build --detach aplicacion

# Detener y limpiar todo, incluidos los datos
docker compose down --volumes --remove-orphans
```

---

## Diagnóstico

### La aplicación no arranca

```bash
docker compose logs aplicacion
```

Casos habituales:

| Síntoma en el registro | Causa | Solución |
|---|---|---|
| `No se configuro la cadena de conexion 'Licitaciones'` | Falta `ConnectionStrings__Licitaciones` | Revisar `.env` |
| `La base de datos aun no responde (intento N de 12)` | PostgreSQL todavía arranca | Es normal durante el primer minuto; el reintento lo resuelve |
| `password authentication failed` | La clave de `.env` no coincide con la del volumen | `docker compose down --volumes` y volver a levantar |

La tercera merece explicación: PostgreSQL fija la clave del usuario al **inicializar el volumen**.
Cambiar `POSTGRES_PASSWORD` en `.env` con un volumen ya creado no cambia la clave dentro de la base,
y la aplicación queda sin poder autenticarse. Hay que recrear el volumen.

### El puerto está ocupado

```bash
# Ver qué lo ocupa
sudo lsof -i :8080
```

O cambiar la publicación en `docker-compose.yml`, por ejemplo a `"8081:8080"`.

### Las fechas se muestran desplazadas

Indica que `tzdata` no está disponible en la imagen. Verificar:

```bash
docker compose exec aplicacion cat /usr/share/zoneinfo/America/Costa_Rica > /dev/null && echo OK
```

---

## Consideraciones para producción

Este archivo Compose está pensado para desarrollo y demostración. Para un entorno real:

1. **No publicar el puerto de PostgreSQL.** Aquí se expone en `5432` para poder inspeccionar la
   base; en producción la base solo debe ser accesible desde la red interna.
2. **Usar secretos en lugar de variables de entorno.** Los manifiestos de Kubernetes ya lo hacen;
   ver [kubernetes.md](kubernetes.md).
3. **Terminar TLS antes de la aplicación**, en un proxy inverso o en el Ingress.
4. **Fijar la etiqueta de la imagen** en lugar de reconstruir desde el contexto local.
