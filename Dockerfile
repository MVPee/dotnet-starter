# syntax=docker/dockerfile:1

# Not pinned to $BUILDPLATFORM: dotnet-ef must load the startup assembly, which fails when the bundle RID differs from the SDK's arch.
# Same SDK as global.json so analyzers (AnalysisLevel=latest) match CI.
FROM mcr.microsoft.com/dotnet/sdk:11.0.100 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY .config/dotnet-tools.json .config/
COPY src/Starter.Domain/Starter.Domain.csproj src/Starter.Domain/
COPY src/Starter.Application/Starter.Application.csproj src/Starter.Application/
COPY src/Starter.Infrastructure/Starter.Infrastructure.csproj src/Starter.Infrastructure/
COPY src/Starter.Api/Starter.Api.csproj src/Starter.Api/
RUN dotnet restore src/Starter.Api/Starter.Api.csproj \
    && dotnet tool restore

COPY src/ src/

FROM build AS publish
# Framework-dependent and RID-agnostic, so the same output runs on amd64 and arm64.
RUN dotnet publish src/Starter.Api/Starter.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    -p:UseAppHost=false

FROM build AS migrations-build
ARG TARGETARCH
# Generating the bundle never connects; this only satisfies the design-time factory, which refuses to guess inside a container.
ARG BUNDLE_BUILD_CONNECTION_PLACEHOLDER="Host=bundle-build-placeholder"
RUN case "$TARGETARCH" in \
        amd64) runtime=linux-x64 ;; \
        arm64) runtime=linux-arm64 ;; \
        *) echo "Unsupported TARGETARCH: $TARGETARCH" >&2; exit 1 ;; \
    esac \
    && dotnet restore src/Starter.Infrastructure --runtime "$runtime" \
    && ConnectionStrings__Database="$BUNDLE_BUILD_CONNECTION_PLACEHOLDER" dotnet ef migrations bundle \
        --project src/Starter.Infrastructure \
        --startup-project src/Starter.Infrastructure \
        --configuration Release \
        --self-contained \
        --runtime "$runtime" \
        --output /app/efbundle

# No --connection argument: the bundle reads ConnectionStrings__Database itself, so the password never appears in the process list.
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled-extra AS migrations
WORKDIR /app
COPY --from=migrations-build /app/efbundle ./efbundle
USER $APP_UID
ENTRYPOINT ["./efbundle"]

# chiseled-extra adds ICU and tzdata for culture- and time-zone-aware code, still with no shell or package manager.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=publish /app/publish ./
USER $APP_UID
ENTRYPOINT ["dotnet", "Starter.Api.dll"]
