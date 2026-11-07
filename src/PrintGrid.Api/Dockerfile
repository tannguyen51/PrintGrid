FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# ---------------------------------------------------------------------------
# Install CuraEngine from Debian bookworm package (cura-engine 4.13.0).
# No external downloads — all deps come from cached Docker image layers.
# CLI slice command is stable across 4.x/5.x; profiles are resolved JSON.
# When multi-color slicing is ready, set QuoteSlicer__ExecutablePath=/usr/bin/CuraEngine
# on Railway. See documentation/project/design/18-Slicer-Quotation.md for details.
# ---------------------------------------------------------------------------
RUN apt-get update \
    && apt-get install -y --no-install-recommends cura-engine postgresql-client wget \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /opt/cura \
    && cp $(which CuraEngine) /opt/cura/CuraEngine \
    && chmod 755 /opt/cura/CuraEngine \
    && echo "CuraEngine installed: $($(/opt/cura/CuraEngine --version 2>&1 || true))"

COPY Directory.Build.props Directory.Packages.props ./
COPY src/ src/
RUN dotnet restore src/PrintGrid.Api/PrintGrid.Api.csproj
RUN dotnet publish src/PrintGrid.Api/PrintGrid.Api.csproj -c Release -o /app/publish --no-restore

# Generate the idempotent migration script used by Railway's pre-deploy step.
# Running it at build time keeps the EF tooling out of the runtime image.
RUN dotnet tool install --global dotnet-ef --version 9.0.0 \
    && export PATH="$PATH:/root/.dotnet/tools" \
    && dotnet ef migrations script --idempotent \
        --no-build --configuration Release \
        --project src/PrintGrid.Infrastructure.Shared/PrintGrid.Infrastructure.Shared.csproj \
        --startup-project src/PrintGrid.Api/PrintGrid.Api.csproj \
        -o /app/migrations.sql

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# psql is required only by the pre-deploy migration command.
RUN apt-get update \
    && apt-get install -y --no-install-recommends postgresql-client \
    && rm -rf /var/lib/apt/lists/*

RUN groupadd --system --gid 2000 printgrid \
    && useradd --system --uid 2000 --gid printgrid printgrid \
    && mkdir -p /app/logs && chown -R printgrid:printgrid /app

COPY --from=build --chown=printgrid:printgrid /app/publish ./
COPY --from=build --chown=printgrid:printgrid /app/migrations.sql ./migrations.sql
# Copy CuraEngine; runtime calls it during quote creation
COPY --from=build /opt/cura/CuraEngine /opt/cura/CuraEngine
RUN chmod 755 /opt/cura/CuraEngine

USER printgrid
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "PrintGrid.Api.dll"]
