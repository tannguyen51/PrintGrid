param()
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$api = (docker inspect printgrid-api | ConvertFrom-Json)[0]
if ($LASTEXITCODE -ne 0) { throw 'Start the local printgrid-api Docker container first.' }
$settings = @{}
foreach ($entry in $api.Config.Env) {
    $split = $entry.IndexOf('=')
    if ($split -gt 0) { $settings[$entry.Substring(0, $split)] = $entry.Substring($split + 1) }
}
$connection = $settings['ConnectionStrings__DefaultConnection']
if ($connection -notmatch '(?i)(^|;)\s*Host\s*=\s*postgres\s*(;|$)' -or
    $connection -notmatch '(?i)(^|;)\s*Database\s*=\s*printgrid_db\s*(;|$)') {
    throw 'Expected the local Docker PostgreSQL service and database printgrid_db.'
}
$oldDb = $env:PRINTGRID_UI_TEST_DB
$oldAccess = $env:PRINTGRID_UI_TEST_MINIO_ACCESS
$oldSecret = $env:PRINTGRID_UI_TEST_MINIO_SECRET
try {
    $env:PRINTGRID_UI_TEST_DB = $connection -replace '(?i)Host\s*=\s*postgres', 'Host=127.0.0.1'
    $env:PRINTGRID_UI_TEST_MINIO_ACCESS = $settings['MinIO__AccessKey']
    $env:PRINTGRID_UI_TEST_MINIO_SECRET = $settings['MinIO__SecretKey']
    & dotnet run --project (Join-Path $repoRoot 'tools/SeedMachineSelection') -- $repoRoot
    if ($LASTEXITCODE -ne 0) { throw 'Local machine selection UI seed failed.' }
} finally {
    $env:PRINTGRID_UI_TEST_DB = $oldDb
    $env:PRINTGRID_UI_TEST_MINIO_ACCESS = $oldAccess
    $env:PRINTGRID_UI_TEST_MINIO_SECRET = $oldSecret
}
