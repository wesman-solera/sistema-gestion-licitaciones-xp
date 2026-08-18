# Despliegue en Kubernetes

## Manifiestos

| Archivo | Objeto | Función |
|---|---|---|
| `namespace.yaml` | Namespace | Aísla los objetos del proyecto |
| `app-configmap.yaml` | ConfigMap | Configuración no sensible |
| `app-secret.example.yaml` | Secret (plantilla) | Modelo del secreto real, que no se versiona |
| `postgres-pvc.yaml` | PersistentVolumeClaim | Almacenamiento persistente de la base |
| `postgres-statefulset.yaml` | StatefulSet | Instancia de PostgreSQL |
| `postgres-service.yaml` | Service | Nombre DNS estable de la base |
| `app-deployment.yaml` | Deployment | Réplicas de la aplicación |
| `app-service.yaml` | Service | Punto de entrada de la aplicación |

---

## Despliegue paso a paso

### 1. Crear el espacio de nombres

```bash
kubectl apply -f k8s/namespace.yaml
```

### 2. Crear el secreto

**El archivo `k8s/app-secret.yaml` no se versiona.** El repositorio no puede contener credenciales
reales, así que el secreto se crea directamente contra el clúster:

```bash
CLAVE='reemplace-por-una-clave-fuerte'

kubectl create secret generic licitaciones-secret \
  --namespace licitaciones \
  --from-literal=POSTGRES_USER='licitaciones_app' \
  --from-literal=POSTGRES_PASSWORD="$CLAVE" \
  --from-literal=ConnectionStrings__Licitaciones="Host=licitaciones-postgres;Port=5432;Database=licitaciones;Username=licitaciones_app;Password=$CLAVE"
```

`app-secret.example.yaml` documenta la forma esperada y sirve de referencia. La integración
continua comprueba que `app-secret.yaml` no exista en el repositorio.

### 3. Aplicar la configuración y el almacenamiento

```bash
kubectl apply -f k8s/app-configmap.yaml
kubectl apply -f k8s/postgres-pvc.yaml
```

### 4. Desplegar PostgreSQL

```bash
kubectl apply -f k8s/postgres-service.yaml
kubectl apply -f k8s/postgres-statefulset.yaml

kubectl rollout status statefulset/licitaciones-postgres --namespace licitaciones
```

### 5. Desplegar la aplicación

```bash
kubectl apply -f k8s/app-deployment.yaml
kubectl apply -f k8s/app-service.yaml

kubectl rollout status deployment/licitaciones-aplicacion --namespace licitaciones
```

### 6. Acceder

```bash
kubectl port-forward --namespace licitaciones service/licitaciones-aplicacion 8080:80
```

Y abrir <http://localhost:8080>.

### Todo de una vez

```bash
kubectl apply -f k8s/namespace.yaml
# crear el secreto como en el paso 2
kubectl apply -f k8s/ --namespace licitaciones
```

---

## Decisiones de despliegue

### PostgreSQL como StatefulSet, no como Deployment

Un Deployment con estrategia de actualización progresiva puede crear el pod nuevo **antes** de
terminar el viejo. Ambos montarían el mismo volumen a la vez, y PostgreSQL no lo admite: la base
podría corromperse.

El StatefulSet garantiza identidad estable y reemplazo ordenado, que es exactamente lo que necesita
un motor de base de datos.

### Servicio sin dirección IP propia para la base

`licitaciones-postgres` es un servicio *headless* (`clusterIP: None`). Junto con el StatefulSet, da
a la instancia un nombre DNS estable, que es el que usa la cadena de conexión de la aplicación.

### La variable `PGDATA`

```yaml
- name: PGDATA
  value: /var/lib/postgresql/data/pgdata
```

La imagen oficial de PostgreSQL crea la base en el directorio que indique `PGDATA`. Si se dejara el
valor por defecto apuntando directamente al punto de montaje, `initdb` fallaría porque el volumen
no está vacío: contiene el directorio `lost+found` que crea el sistema de archivos. Por eso se
apunta a un subdirectorio.

### Tres sondas, y cada una mira algo distinto

Esta es la decisión con más consecuencias operativas del despliegue.

| Sonda | Ruta | Consulta la base | Qué pasa si falla |
|---|---|---|---|
| `startupProbe` | `/health/vivo` | No | Las demás sondas no empiezan a evaluar |
| `readinessProbe` | `/health/listo` | **Sí** | El pod sale del balanceo, pero no se reinicia |
| `livenessProbe` | `/health/vivo` | **No** | El pod se reinicia |

**Por qué la sonda de vida no consulta la base de datos.** Si lo hiciera, una caída de PostgreSQL
marcaría como muertos a todos los pods de la aplicación y Kubernetes los reiniciaría en bucle. El
reinicio no arreglaría nada —el problema está en la base— y además destruiría cualquier estado en
memoria y saturaría el clúster justo cuando ya hay una incidencia.

La sonda de disponibilidad sí la consulta, y esa es la correcta: un pod sin base no puede atender
peticiones útiles, así que debe salir del balanceo. Pero salir del balanceo es reversible; el
reinicio no lo es.

La sonda de arranque da hasta 150 segundos (30 intentos cada 5 segundos) para que se apliquen las
migraciones antes de que las otras dos empiecen a contar fallos.

### Solicitudes y límites de recursos

| Componente | CPU solicitada | CPU límite | Memoria solicitada | Memoria límite |
|---|---|---|---|---|
| Aplicación | 100m | 1000m | 256Mi | 768Mi |
| PostgreSQL | 100m | 1000m | 256Mi | 1Gi |

Las **solicitudes** son lo que el planificador reserva; sin ellas, los pods podrían programarse en
un nodo sin capacidad real. Los **límites** impiden que un componente con una fuga consuma el nodo
completo.

### Actualización progresiva sin caída

```yaml
strategy:
  type: RollingUpdate
  rollingUpdate:
    maxSurge: 1
    maxUnavailable: 0
```

`maxUnavailable: 0` garantiza que siempre haya réplicas atendiendo durante una actualización.

### Contexto de seguridad

```yaml
securityContext:
  runAsNonRoot: true
  runAsUser: 1654
  allowPrivilegeEscalation: false
  capabilities:
    drop: ["ALL"]
```

El UID 1654 corresponde al usuario `app` de la imagen base de ASP.NET. Se descartan todas las
capacidades de Linux porque una aplicación web no necesita ninguna.

---

## Verificación del despliegue

### Estado general

```bash
kubectl get all --namespace licitaciones
kubectl get pvc --namespace licitaciones
```

### Registros

```bash
kubectl logs --namespace licitaciones deployment/licitaciones-aplicacion --follow
kubectl logs --namespace licitaciones statefulset/licitaciones-postgres
```

### Comprobar las sondas

```bash
kubectl describe pod --namespace licitaciones --selector app.kubernetes.io/component=aplicacion
```

En la salida deben aparecer las tres sondas configuradas y sin fallos acumulados.

### Comprobar la persistencia tras un reinicio

Esta es la evidencia que pide la sección 13.2 del enunciado:

```bash
# 1. Crear datos desde la interfaz, o directamente:
kubectl exec --namespace licitaciones statefulset/licitaciones-postgres -- \
  psql -U licitaciones_app -d licitaciones \
  -c "SELECT count(*) FROM licitaciones;"

# 2. Eliminar el pod de la base; el StatefulSet lo recrea
kubectl delete pod --namespace licitaciones licitaciones-postgres-0

# 3. Esperar a que vuelva
kubectl rollout status statefulset/licitaciones-postgres --namespace licitaciones

# 4. Los datos siguen ahí
kubectl exec --namespace licitaciones statefulset/licitaciones-postgres -- \
  psql -U licitaciones_app -d licitaciones \
  -c "SELECT count(*) FROM licitaciones;"
```

---

## Diagnóstico

| Síntoma | Causa probable | Solución |
|---|---|---|
| `CreateContainerConfigError` | Falta el secreto `licitaciones-secret` | Crearlo según el paso 2 |
| Pod en `Pending` | No hay volumen disponible que satisfaga el PVC | Revisar la clase de almacenamiento del clúster |
| `CrashLoopBackOff` en la aplicación | La cadena de conexión es incorrecta | Revisar el secreto y los registros del pod |
| `initdb: directory not empty` | `PGDATA` apunta al punto de montaje | Verificar la variable en el StatefulSet |
| La aplicación reinicia en bucle con la base caída | La sonda de vida consulta la base | Comprobar que apunta a `/health/vivo` |

### Ver los eventos del espacio de nombres

```bash
kubectl get events --namespace licitaciones --sort-by=.lastTimestamp
```

---

## Validación de los manifiestos

La integración continua valida los manifiestos contra el esquema de Kubernetes con `kubeconform`.
Para ejecutarlo localmente:

```bash
kubeconform -strict -summary -kubernetes-version 1.29.0 \
  -ignore-filename-pattern 'app-secret.example.yaml' \
  k8s/
```

La plantilla de secretos se excluye a propósito: contiene marcadores de sustitución, no valores
reales.

---

## Limpieza

```bash
kubectl delete namespace licitaciones
```

Elimina todos los objetos del proyecto. **El PersistentVolume puede sobrevivir** según la política
de recuperación de la clase de almacenamiento; conviene verificarlo:

```bash
kubectl get pv
```
