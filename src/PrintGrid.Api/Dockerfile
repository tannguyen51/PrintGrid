FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# ---------------------------------------------------------------------------
# Build CuraEngine + Arcus from source — matches design doc 18-Slicer-Quotation.md
# Arcus is Ultimaker's message-bus library; CuraEngine depends on it.
# Both use CMake; build order must be: Arcus → CuraEngine.
# Use tarball downloads (not git clone) to avoid auth/proxy issues on CI/CD.
# ---------------------------------------------------------------------------
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        git cmake g++ make pkg-config wget \
        libboost-all-dev libssl-dev zlib1g-dev libprotobuf-dev protobuf-compiler \
        && rm -rf /var/lib/apt/lists/* \
    # --- Step 1: download & build Arcus (Ultimaker's message bus) --- \
    && mkdir -p /opt/build && cd /opt/build \
    && echo "[1/7] Downloading Arcus..." \
    && wget -q -O arcus.tar.gz https://github.com/Ultimaker/Arcus/archive/refs/tags/release-4.1.0.tar.gz \
    && tar xzf arcus.tar.gz && mv Arcus-release-4.1.0 arcus-src \
    && mkdir -p arcus-build && cd arcus-build \
    && echo "[2/7] Building Arcus..." \
    && cmake ../arcus-src -DCMAKE_BUILD_TYPE=Release \
    && make -j$(nproc) \
    && make install PREFIX=/usr/local \
    && ldconfig \
    # --- Step 2: download & build CuraEngine --- \
    && echo "[3/7] Downloading CuraEngine..." \
    && cd /opt/build \
    && wget -q -O cura.tar.gz https://github.com/Ultimaker/CuraEngine/archive/refs/heads/main.tar.gz \
    && tar xzf cura.tar.gz && mv CuraEngine-main cura-src \
    && mkdir -p cura-build && cd cura-build \
    && echo "[4/7] Configuring CuraEngine..." \
    && cmake ../cura-src -DCMAKE_BUILD_TYPE=Release \
    && echo "[5/7] Building CuraEngine..." \
    && make -j$(nproc) \
    && echo "[6/7] Copying binary..." \
    && cp CuraEngine /opt/cura/CuraEngine \
    && chmod 755 /opt/cura/CuraEngine \
    && echo "[7/7] Cleaning up..." \
    && rm -rf /opt/build \
    && echo "CuraEngine build complete."

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

USER printgrid
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "PrintGrid.Api.dll"]
