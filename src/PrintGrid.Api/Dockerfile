FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Note: CuraEngine multi-color quoting is temporarily disabled until we can
# reliably extract the binary from the Cura 5.x New Gen (compressed) AppImage.
# When enabled, set QuoteSlicer__ExecutablePath=/opt/cura/CuraEngine on Railway.
# See tools/docker/extract_cura.py and infra/slicer/README.md for implementation details.

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
