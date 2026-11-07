FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# ---------------------------------------------------------------------------
# Build CuraEngine + Arcus from source — matches design doc 18-Slicer-Quotation.md
# Arcus is Ultimaker's message-bus library; CuraEngine depends on it.
# Both use CMake; build order must be: Arcus → CuraEngine.
# Pin versions known to work together for Cura 5.11.0 compatibility.
# ---------------------------------------------------------------------------
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        git cmake g++ make pkg-config \
        libboost-all-dev libssl-dev zlib1g-dev libprotobuf-dev protobuf-compiler \
        && rm -rf /var/lib/apt/lists/* \
    # --- Step 1: build & install Arcus (Ultimaker's message bus) --- \
    && mkdir -p /opt/build && cd /opt/build \
    && echo "[1/6] Cloning Arcus..." \
    && git clone --depth 1 --branch release-4.1.0 https://github.com/Ultimaker/Arcus.git arcus-src \
    && mkdir -p arcus-build && cd arcus-build \
    && echo "[2/6] Building Arcus..." \
    && cmake ../arcus-src -DCMAKE_BUILD_TYPE=Release \
    && make -j$(nproc) \
    && make install PREFIX=/usr/local \
    && ldconfig \
    # --- Step 2: build CuraEngine --- \
    && echo "[3/6] Cloning CuraEngine..." \
    && git clone --depth 1 --branch main https://github.com/Ultimaker/CuraEngine.git cura-src \
    && mkdir -p cura-build && cd cura-build \
    && echo "[4/6] Configuring CuraEngine..." \
    && cmake ../cura-src -DCMAKE_BUILD_TYPE=Release \
    && echo "[5/6] Building CuraEngine..." \
    && make -j$(nproc) \
    && echo "[6/6] Copying binary..." \
    && cp CuraEngine /opt/cura/CuraEngine \
    && chmod 755 /opt/cura/CuraEngine \
    && rm -rf /opt/build

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
