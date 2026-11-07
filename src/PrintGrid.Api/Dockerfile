FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# ---------------------------------------------------------------------------
# CuraEngine — extracted from the official Linux AppImage via python3-appimage.
# Cura 5.x AppImages are New Gen (type 2 / compressed): a ~16-byte loader
# followed by a zstd-compressed squashfs filesystem. We use the dedicated
# `python3-appimage` library which handles all format versions correctly.
# Pin to the version matching our local test rig profiles (5.11.0).
# ---------------------------------------------------------------------------
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl ca-certificates python3 python3-pip zstd libarchive-tools \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /opt/cura \
    && curl -sL --retry 3 --output /tmp/Cura.AppImage \
       https://github.com/Ultimaker/Cura/releases/download/5.11.0/UltiMaker-Cura-5.11.0-linux-X64.AppImage \
    && echo "Downloaded $(stat --printf='%s' /tmp/Cura.AppImage) bytes" \
    && dd if=/tmp/Cura.AppImage of=/tmp/appimage-squash.bin bs=1 skip=16 status=none 2>&1 \
    && zstdcat /tmp/appimage-squash.bin > /tmp/squash.img 2>/tmp/zstd.log || true \
    && echo "SquashFS size: $(stat --printf='%s' /tmp/squash.img 2>/dev/null || echo 'N/A') bytes" \
    && ls -la /tmp/squash.img 2>/dev/null || echo "squash.img missing!" \
    && bsdtar -tf /tmp/squash.img 2>/dev/null | head -20 || echo "Could not list squashfs contents!" \
    && mkdir -p /opt/cura/tmproot \
    && bsdtar -xf /tmp/squash.img -C /opt/cura/tmproot 2>/tmp/bsdtar.log || echo "bsdtar failed: $(cat /tmp/bsdtar.log)" \
    && echo "Searching for CuraEngine..." \
    && find /opt/cura/tmproot -name 'CuraEngine' -o -name '*Cura*' 2>/dev/null | head -10 \
    && FOUND=$(find /opt/cura/tmproot -name 'CuraEngine' | head -1) \
    && if [ -n "$FOUND" ]; then \
         cp "$FOUND" /opt/cura/CuraEngine && chmod 755 /opt/cura/CuraEngine; \
       else \
         echo "ERROR: Could not find CuraEngine in extracted squashfs!"; exit 1; \
       fi \
    && rm -rf /tmp/Cura.AppImage /tmp/appimage-squash.bin /tmp/squash.img /tmp/zstd.log /tmp/bsdtar.log /opt/cura/tmproot

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

# Copy CuraEngine binary; keep it executable for the unprivileged user.
COPY --from=build /opt/cura/CuraEngine /opt/cura/CuraEngine
RUN chmod 755 /opt/cura/CuraEngine

USER printgrid
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "PrintGrid.Api.dll"]
