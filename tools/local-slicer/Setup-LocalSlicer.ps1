param([switch]$SeedLocalDatabase, [switch]$SmokeTest)
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$runtime = Join-Path $repoRoot '.local'
$engine = Join-Path $runtime 'cura-5.11.0/CuraEngine.exe'
$python = Join-Path $runtime 'python312/python.exe'
if (!(Test-Path -LiteralPath $engine) -or !(Test-Path -LiteralPath $python)) {
    throw 'Portable CuraEngine/Python are missing. See infra/slicer/README.md for pinned official downloads and extraction.'
}
& $python (Join-Path $PSScriptRoot 'generate_profiles.py') --cura (Join-Path $runtime 'cura-5.11.0') --output (Join-Path $repoRoot 'infra/slicer/profiles') --fixtures (Join-Path $repoRoot 'tests/manual/slicer')
if ($LASTEXITCODE -ne 0) { throw 'Profile generation failed.' }
# Read only the Docker-local credentials needed for loopback services; never print them.
$dotenv = Join-Path $repoRoot '.env'
$values = @{}
if (Test-Path -LiteralPath $dotenv) {
    foreach ($line in Get-Content -LiteralPath $dotenv) {
        if ($line -match '^\s*([A-Z_]+)\s*=\s*(.*?)\s*$' -and !$line.TrimStart().StartsWith('#')) {
            $values[$Matches[1]] = $Matches[2].Trim('"').Trim("'")
        }
    }
}
$previousDb = $env:PRINTGRID_SLICER_LOCAL_DB
$previousKey = $env:PRINTGRID_LOCAL_MINIO_ACCESS
$previousSecret = $env:PRINTGRID_LOCAL_MINIO_SECRET
try {
    if (!$previousDb -and $values.ContainsKey('POSTGRES_PASSWORD')) {
        $localUser = if ($values.ContainsKey('POSTGRES_USER')) { $values['POSTGRES_USER'] } else { 'postgres' }
        # Quote values as Npgsql connection-string literals, not as shell syntax.
        $escapedUser = $localUser.Replace('"', '""')
        $escapedPassword = $values['POSTGRES_PASSWORD'].Replace('"', '""')
        $env:PRINTGRID_SLICER_LOCAL_DB = 'Host=localhost;Port=5432;Database=printgrid_slicer_local;Username="' + $escapedUser + '";Password="' + $escapedPassword + '";Timeout=3'
    }
    if ($values.ContainsKey('MINIO_ROOT_PASSWORD')) {
        $env:PRINTGRID_LOCAL_MINIO_ACCESS = if ($values.ContainsKey('MINIO_ROOT_USER')) { $values['MINIO_ROOT_USER'] } else { 'minioadmin' }
        $env:PRINTGRID_LOCAL_MINIO_SECRET = $values['MINIO_ROOT_PASSWORD']
    }
    $mode = if ($SeedLocalDatabase) { '--seed-local-db' } else { '--configure' }
    & dotnet run --project (Join-Path $repoRoot 'tools/LocalSlicerTest') -- $repoRoot $mode
    if ($LASTEXITCODE -ne 0) { throw 'Local configuration/seed failed. Start local PostgreSQL/Docker before seeding.' }
    if ($SmokeTest) {
        & dotnet run --project (Join-Path $repoRoot 'tools/LocalSlicerTest') --no-build -- $repoRoot
        if ($LASTEXITCODE -ne 0) { throw 'Real CuraEngine smoke test failed.' }
    }
} finally {
    $env:PRINTGRID_SLICER_LOCAL_DB = $previousDb
    $env:PRINTGRID_LOCAL_MINIO_ACCESS = $previousKey
    $env:PRINTGRID_LOCAL_MINIO_SECRET = $previousSecret
}
