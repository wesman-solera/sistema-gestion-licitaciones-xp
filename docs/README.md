# Sistema de Gestión de Licitaciones

Proyecto final del curso **ITI-822 — Metodologías Ágiles de Desarrollo de Software**
Universidad Técnica Nacional · Ingeniería en Tecnologías de Información · II Cuatrimestre 2026

| Elemento | Detalle |
|---|---|
| Estudiante | Wesman Edel Solera Rodríguez |
| Modalidad | Individual |
| Metodología | Extreme Programming (XP), como marco único |
| Tecnologías | .NET 9, ASP.NET Core MVC + Web API, EF Core 9, PostgreSQL 16, Docker, Kubernetes |
| Moneda oficial | Colón costarricense (CRC) |

---

## Qué hace el sistema

Administra procesos de licitación de principio a fin: se registran proveedores, se crea y publica
una licitación con su presupuesto y fecha de cierre, los proveedores presentan ofertas económicas,
y el sistema identifica la mejor oferta, calcula el ahorro obtenido y determina quién debe
aprobar la adjudicación según el monto.

Todos los montos oficiales se almacenan en colones. La visualización en dólares es una
representación calculada con un tipo de cambio administrable localmente, y nunca modifica los
valores persistidos.

---

## Índice de la documentación

Toda la documentación del proyecto vive en esta carpeta, en Markdown. No hay documentos Word,
PDF ni anexos externos.

### Visión y proceso

| Documento | Contenido |
|---|---|
| [vision-alcance.md](vision-alcance.md) | Propósito, alcance funcional, fuera de alcance y glosario |
| [historias-usuario.md](historias-usuario.md) | Historias de usuario con prioridad, estimación y criterios de aceptación |
| [plan-xp.md](plan-xp.md) | Planning Game, plan de liberación, plan de cada iteración y reglas de trabajo XP |
| [bitacora-xp.md](bitacora-xp.md) | Registro por iteración: velocidad, TDD, refactorizaciones, retroalimentación |
| [uso-ia.md](uso-ia.md) | Declaración del uso de herramientas de inteligencia artificial |

### Técnica

| Documento | Contenido |
|---|---|
| [arquitectura-general.md](arquitectura-general.md) | Capas, dependencias, decisiones y diagramas |
| [modelo-datos.md](modelo-datos.md) | Entidades, relaciones, restricciones e índices |
| [integracion-modulos.md](integracion-modulos.md) | Cómo cooperan los módulos y los flujos de extremo a extremo |
| [api.md](api.md) | Endpoints, contratos, ejemplos y errores |
| [pruebas.md](pruebas.md) | Estrategia de pruebas, ejecución y cobertura |
| [docker.md](docker.md) | Construcción y ejecución con Docker y Docker Compose |
| [kubernetes.md](kubernetes.md) | Despliegue en Kubernetes, sondas y almacenamiento |

### Módulos

| Módulo | Documento |
|---|---|
| Licitaciones | [modulos/licitaciones.md](modulos/licitaciones.md) |
| Proveedores | [modulos/proveedores.md](modulos/proveedores.md) |
| Ofertas | [modulos/ofertas.md](modulos/ofertas.md) |
| Niveles de aprobación | [modulos/niveles-aprobacion.md](modulos/niveles-aprobacion.md) |
| Tipo de cambio | [modulos/tipo-cambio.md](modulos/tipo-cambio.md) |
| Interfaz web | [modulos/interfaz-web.md](modulos/interfaz-web.md) |
| API REST | [modulos/api-rest.md](modulos/api-rest.md) |
| Persistencia | [modulos/persistencia.md](modulos/persistencia.md) |

---

## Puesta en marcha rápida

```bash
cp .env.example .env
# Edite .env y defina POSTGRES_PASSWORD
docker compose up --build
```

- Interfaz web: <http://localhost:8080>
- Documentación interactiva de la API: <http://localhost:8080/swagger>
- Sonda de vida: <http://localhost:8080/health/vivo>

Detalle completo en [docker.md](docker.md).

---

## Ejecución de las pruebas

```bash
# Unitarias (rápidas, sin infraestructura)
dotnet test tests/Licitaciones.UnitTests

# Integración (levanta PostgreSQL 16 real con Testcontainers; requiere Docker)
dotnet test tests/Licitaciones.IntegrationTests

# Funcionales de navegador (requieren la aplicación levantada)
docker compose up --detach --wait
dotnet test tests/Licitaciones.FunctionalTests
```

Detalle completo en [pruebas.md](pruebas.md).

---

## Estructura del repositorio

```
/src
  /Licitaciones.Domain          Entidades y reglas de negocio, sin dependencias de infraestructura
  /Licitaciones.Application     Casos de uso, DTO, validadores y puertos
  /Licitaciones.Infrastructure  EF Core, PostgreSQL, repositorios y migraciones
  /Licitaciones.Api             Controladores REST, OpenAPI y ProblemDetails
  /Licitaciones.Web             MVC, vistas, temas y experiencia de usuario
/tests
  /Licitaciones.UnitTests        Pruebas unitarias
  /Licitaciones.IntegrationTests Pruebas contra PostgreSQL real
  /Licitaciones.FunctionalTests  Pruebas de extremo a extremo con Playwright
/docs                            Toda la documentación del proyecto
/k8s                             Manifiestos de Kubernetes
/.github/workflows               Integración continua
```
