FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/ src/
RUN dotnet restore src/PrintGrid.Api/PrintGrid.Api.csproj
RUN dotnet publish src/PrintGrid.Api/PrintGrid.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN groupadd --system --gid 2000 printgrid \
    && useradd --system --uid 2000 --gid printgrid printgrid \
    && mkdir -p /app/logs && chown -R printgrid:printgrid /app

COPY --from=build --chown=printgrid:printgrid /app/publish ./

USER printgrid
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "PrintGrid.Api.dll"]
