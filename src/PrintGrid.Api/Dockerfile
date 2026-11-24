FROM debian:bookworm-slim AS cura
ARG CURA_VERSION=5.11.0
ARG CURA_APPIMAGE_SHA256=bacdfbe60c55ac6a867a6d84b7654d466e93dfcf49c733e6d5349c7ab9626bd9
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl \
    && rm -rf /var/lib/apt/lists/* \
    && curl --fail --location --retry 5 \
      "https://github.com/Ultimaker/Cura/releases/download/${CURA_VERSION}/UltiMaker-Cura-${CURA_VERSION}-linux-X64.AppImage" \
      --output /tmp/cura.AppImage \
    && echo "${CURA_APPIMAGE_SHA256}  /tmp/cura.AppImage" | sha256sum --check --strict \
    && chmod 755 /tmp/cura.AppImage \
    && cd /opt && /tmp/cura.AppImage --appimage-extract \
    && test -x /opt/squashfs-root/CuraEngine

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Build and migration tooling only; Cura comes from the checksum-pinned stage above.
RUN apt-get update \
    && apt-get install -y --no-install-recommends postgresql-client \
    && rm -rf /var/lib/apt/lists/*

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
    && apt-get install -y --no-install-recommends postgresql-client python3 prusa-slicer=2.5.0+dfsg-4 xvfb xauth \
    && rm -rf /var/lib/apt/lists/*

RUN groupadd --system --gid 2000 printgrid \
    && useradd --system --uid 2000 --gid printgrid printgrid \
    && mkdir -p /app/logs && chown -R printgrid:printgrid /app

COPY --from=build --chown=printgrid:printgrid /app/publish ./
COPY --from=build --chown=printgrid:printgrid /app/migrations.sql ./migrations.sql
# Copy the complete release runtime so its shared libraries stay aligned with the engine.
COPY --from=cura /opt/squashfs-root/ /opt/cura/runtime/
COPY infra/slicer/cura-resources/5.11.0/ /opt/cura/resources/
COPY tools/cura-library/ /opt/cura/tools/
COPY tools/slicer/prusa-quote.sh /opt/printgrid/prusa-quote
RUN sed -i 's/\r$//' /opt/printgrid/prusa-quote && chmod 755 /opt/printgrid/prusa-quote
RUN sed -i 's/\r$//' /opt/cura/tools/cura-engine.sh \
    && cp /opt/cura/tools/cura-engine.sh /opt/cura/CuraEngine \
    && chmod 755 /opt/cura/CuraEngine /opt/cura/runtime/CuraEngine /opt/cura/tools/resolve_profile.py

# Fail the image build if the extracted engine cannot start from the API's cwd.
RUN /opt/cura/CuraEngine help 2>&1 | grep -F "Cura_SteamEngine version 5.11.0"

USER printgrid
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV QuoteSlicer__EngineVersion=5.11.0
ENV QuoteSlicer__ExecutablePath=/opt/cura/CuraEngine
ENV QuoteSlicer__PaintSlicerExecutablePath=/opt/printgrid/prusa-quote
ENV CuraLibrary__ResourcesPath=/opt/cura/resources
ENV CuraLibrary__ResolverPath=/opt/cura/tools/resolve_profile.py

ENTRYPOINT ["dotnet", "PrintGrid.Api.dll"]
