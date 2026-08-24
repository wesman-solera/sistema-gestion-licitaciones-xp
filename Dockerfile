# syntax=docker/dockerfile:1

# ---------------------------------------------------------------------------------------------
# Imagen de la aplicacion web del Sistema de Gestion de Licitaciones.
#
# Construccion multi-etapa: la etapa de compilacion trae el SDK completo de .NET 9 y queda
# descartada, de modo que la imagen final solo contiene el runtime de ASP.NET y los binarios
# publicados. Eso reduce el tamano y la superficie expuesta.
# ---------------------------------------------------------------------------------------------

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS compilacion
WORKDIR /origen

# Se copian primero los archivos de proyecto y se restauran las dependencias. Docker cachea esta
# capa, de modo que un cambio de codigo que no toque las referencias no vuelve a descargar nada.
#
# Solo se copian los proyectos que la aplicacion web necesita para restaurar: Domain, Application,
# Infrastructure y Api. Los proyectos de prueba quedan fuera a proposito, y por partida doble:
# .dockerignore excluye la carpeta tests del contexto de construccion, y la imagen de ejecucion no
# tiene por que cargar con dependencias que solo sirven para probar.
COPY global.json Directory.Build.props ./
COPY src/Licitaciones.Domain/Licitaciones.Domain.csproj src/Licitaciones.Domain/
COPY src/Licitaciones.Application/Licitaciones.Application.csproj src/Licitaciones.Application/
COPY src/Licitaciones.Infrastructure/Licitaciones.Infrastructure.csproj src/Licitaciones.Infrastructure/
COPY src/Licitaciones.Api/Licitaciones.Api.csproj src/Licitaciones.Api/
COPY src/Licitaciones.Web/Licitaciones.Web.csproj src/Licitaciones.Web/
RUN dotnet restore src/Licitaciones.Web/Licitaciones.Web.csproj

COPY src/ src/

RUN dotnet publish src/Licitaciones.Web/Licitaciones.Web.csproj \
        --configuration Release \
        --no-restore \
        --output /publicacion \
        /p:UseAppHost=false

# ---------------------------------------------------------------------------------------------

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS ejecucion
WORKDIR /aplicacion

# tzdata es necesario para resolver America/Costa_Rica: sin el, la conversion de UTC a hora
# local caeria al valor de reserva y las fechas se mostrarian desplazadas (seccion 8.2).
RUN apt-get update \
    && apt-get install --yes --no-install-recommends tzdata curl \
    && rm --recursive --force /var/lib/apt/lists/*

# La imagen base define el usuario sin privilegios "app" (UID 1654). Ejecutar como ese usuario
# en lugar de root limita el dano posible ante una vulnerabilidad en la aplicacion (seccion 13.1).
COPY --from=compilacion --chown=app:app /publicacion ./
USER app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    TZ=America/Costa_Rica

EXPOSE 8080

# La sonda de vida responde sin consultar la base de datos: un fallo de PostgreSQL no debe
# provocar que el orquestador reinicie el contenedor de la aplicacion en bucle.
HEALTHCHECK --interval=20s --timeout=5s --start-period=40s --retries=5 \
    CMD curl --fail --silent http://localhost:8080/health/vivo || exit 1

ENTRYPOINT ["dotnet", "Licitaciones.Web.dll"]
