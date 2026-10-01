# syntax=docker/dockerfile:1.7

ARG DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0
ARG DOTNET_ASPNET_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0

FROM ${DOTNET_SDK_IMAGE} AS build
WORKDIR /src

COPY veterinarian_backend.slnx ./
COPY src/Domain/Domain.csproj src/Domain/
COPY src/Application/Application.csproj src/Application/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/
COPY src/Api/Api.csproj src/Api/

RUN dotnet restore src/Api/Api.csproj

COPY src/Domain/ src/Domain/
COPY src/Application/ src/Application/
COPY src/Infrastructure/ src/Infrastructure/
COPY src/Api/ src/Api/

RUN dotnet publish src/Api/Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM ${DOTNET_ASPNET_IMAGE} AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

COPY --from=build --chown=app:app /app/publish ./

USER app

EXPOSE 8080

ENTRYPOINT ["dotnet", "Api.dll"]
