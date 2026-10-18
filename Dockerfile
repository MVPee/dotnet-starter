# syntax=docker/dockerfile:1

# Not pinned to $BUILDPLATFORM: dotnet-ef must load the startup assembly, which fails when the bundle RID differs from the SDK's arch.
# Same SDK as global.json so analyzers (AnalysisLevel=latest) match CI.
FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
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
RUN case "$TARGETARCH" in \
        amd64) runtime=linux-x64 ;; \
        arm64) runtime=linux-arm64 ;; \
        *) echo "Unsupported TARGETARCH: $TARGETARCH" >&2; exit 1 ;; \
    esac \
    && dotnet restore src/Starter.Infrastructure --runtime "$runtime" \
    && dotnet ef migrations bundle \
        --project src/Starter.Infrastructure \
        --startup-project src/Starter.Infrastructure \
        --configuration Release \
        --self-contained \
        --runtime "$runtime" \
        --output /app/efbundle

# Not chiseled: the entrypoint needs a shell to expand the connection string from the environment.
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS migrations
WORKDIR /app
COPY --from=migrations-build /app/efbundle ./efbundle
USER $APP_UID
ENTRYPOINT ["/bin/sh", "-c", "exec ./efbundle --connection \"${ConnectionStrings__Database:?ConnectionStrings__Database is required}\""]

# chiseled-extra ships ICU + tzdata (needed by Npgsql/culture-aware code) with no shell or package manager.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=publish /app/publish ./
USER $APP_UID
ENTRYPOINT ["dotnet", "Starter.Api.dll"]
