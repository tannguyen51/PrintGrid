FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# ---------------------------------------------------------------------------
# CuraEngine — extracted from the official Linux AppImage (New Gen / type 2).
# Cura 5.x "compressed" AppImages prepend a ~16-byte binary loader to a zstd-
# compressed squashfs payload. We strip those first 16 bytes and let zstd
# decompress the remainder into a raw squashfs image.
# Pin to the version matching our local test rig profiles (5.11.0).
# ---------------------------------------------------------------------------
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl ca-certificates zstd binutils coreutils \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /opt/cura/extract \
    && curl -sL --retry 3 -o /tmp/Cura.AppImage \
       https://github.com/Ultimaker/Cura/releases/download/5.11.0/UltiMaker-Cura-5.11.0-linux-X64.AppImage \
    && dd if=/tmp/Cura.AppImage of=/tmp/payload.bin bs=1 skip=16 status=none \
    && zstdcat /tmp/payload.bin > /tmp/squash.img 2>/tmp/zstd.log || true \
    && if [ ! -s /tmp/squash.img ]; then echo "zstdcat failed:"; cat /tmp/zstd.log; exit 1; fi \
    && bsdtar -xvf /tmp/squash.img -C /opt/cura/extract squashfs-root/usr/bin/CuraEngine > /dev/null 2>&1 \
    && cp /opt/cura/extract/squashfs-root/usr/bin/CuraEngine /opt/cura/CuraEngine \
    && chmod 755 /opt/cura/CuraEngine \
    && rm -rf /tmp/Cura.AppImage /tmp/payload.bin /tmp/squash.img /tmp/zstd.log /opt/cura/extract

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
