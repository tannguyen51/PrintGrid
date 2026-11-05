param([string]$Api = 'http://localhost:5000/api/v1')
$ErrorActionPreference = 'Stop'
if (([uri]$Api).Host -notin @('localhost', '127.0.0.1', '::1')) { throw 'Local API only.' }
# Accounts are seeded solely by tools/LocalSlicerTest in the isolated local database.
$login = Invoke-RestMethod -Method Post -Uri "$Api/auth/login" -ContentType 'application/json' -Body (@{
    email = $env:PRINTGRID_PROFILE_TEST_EMAIL; password = $env:PRINTGRID_PROFILE_TEST_PASSWORD
} | ConvertTo-Json)
$headers = @{ Authorization = 'Bearer ' + $login.accessToken }
function Send-Json($Method, $Path, $Body) {
    Invoke-RestMethod -Method $Method -Uri "$Api$Path" -Headers $headers -ContentType 'application/json' -Body ($Body | ConvertTo-Json -Depth 20)
}
$profiles = @(Invoke-RestMethod -Uri "$Api/cura-profiles" -Headers $headers | ForEach-Object { $_ })
if ($profiles.Count -lt 1) { throw 'Import local profiles first.' }
$labs = @(Invoke-RestMethod -Uri "$Api/labs?includeInactive=true" -Headers $headers | ForEach-Object { $_ })
$lab = $labs | Where-Object { $_.id -eq $env:PRINTGRID_PROFILE_TEST_LAB } | Select-Object -First 1
if (!$lab) { throw 'Seed a local lab first.' }
$profile = $profiles[0]
$body = @{
    name = 'TEMP registration API smoke'; model = $profile.machineModel; technology = 0
    buildWidthMm = $profile.widthMm; buildDepthMm = $profile.depthMm; buildHeightMm = $profile.heightMm
    minLayerHeightMm = 0.1; achievableToleranceMm = 0.2; supportedMaterials = @($profile.materialCode)
    printerConfiguration = @{
        manufacturer = $profile.manufacturer; extruderCount = $profile.extruderCount
        maximumColorSlots = $profile.maximumColorSlots; nozzleDiameterMm = $profile.nozzleDiameterMm
        filamentDiameterMm = $profile.filamentDiameterMm; heatedBed = $profile.heatedBed
        maxNozzleTemperatureC = 260; maxBedTemperatureC = 100; pricePerHour = 6000; failureRate = 0.08
    }
}
$machine = $null
try {
    $machine = Send-Json Post "/labs/$($lab.id)/machines" $body
    if ($machine.profileStatus -ne 'PendingReview') { throw 'Matching machine did not become PendingReview.' }
    Write-Output 'PASS registration automatically attaches compatible profile and waits for review'
    $review = Send-Json Post "/cura-profiles/machines/$($machine.id)/review" @{ approve = $true }
    if ($review.profileStatus -ne 'Approved') { throw 'Admin review failed.' }
    Write-Output 'PASS admin approves matched machine'
    $body.model = 'UNKNOWN MODEL - SMOKE'
    $updated = Send-Json Put "/labs/$($lab.id)/machines/$($machine.id)" $body
    if ($updated.profileStatus -ne 'MissingProfile' -or $updated.curaProfileIds.Count -ne 0) { throw 'Changed machine kept stale approval/profile.' }
    Write-Output 'PASS unknown hardware remains saved and loses quote eligibility'
    $blocked = Invoke-WebRequest -Method Post -Uri "$Api/cura-profiles/machines/$($machine.id)/review" -Headers $headers `
        -ContentType 'application/json' -Body '{"approve":true}' -SkipHttpErrorCheck
    if ([int]$blocked.StatusCode -ne 400) { throw 'Machine without profile could be approved.' }
    Write-Output 'PASS machine without profile cannot be approved'
    $source = Invoke-RestMethod -Uri "$Api/cura-profiles/$($profile.id)" -Headers $headers
    $null = Send-Json Post '/cura-profiles' @{
        name = 'TEMP registry API smoke profile'; manufacturer = $profile.manufacturer
        machineModel = $body.model; materialCode = $profile.materialCode; version = 'smoke'
        engineVersion = $profile.engineVersion; extruderCount = $profile.extruderCount; settingsJson = $source.settingsJson
    }
    $reloaded = Invoke-RestMethod -Uri "$Api/labs/$($lab.id)" -Headers $headers
    $rematched = $reloaded.machines | Where-Object { $_.id -eq $machine.id }
    if ($rematched.profileStatus -ne 'PendingReview') { throw 'Import did not automatically match the missing machine.' }
    Write-Output 'PASS admin imports resolved JSON and the missing machine rematches automatically'
    $body.model = $profile.machineModel
    $updated = Send-Json Put "/labs/$($lab.id)/machines/$($machine.id)" $body
    if ($updated.profileStatus -ne 'PendingReview') { throw 'Corrected machine did not rematch.' }
    $rejected = Send-Json Post "/cura-profiles/machines/$($machine.id)/review" @{ approve = $false; reason = 'Smoke test rejection' }
    if ($rejected.profileStatus -ne 'Rejected') { throw 'Rejection failed.' }
    Write-Output 'PASS corrected configuration rematches; rejection records reason'
}
finally {
    if ($machine) {
        Invoke-RestMethod -Method Delete -Uri "$Api/labs/$($lab.id)/machines/$($machine.id)" -Headers $headers | Out-Null
        Write-Output 'Removed temporary smoke-test machine.'
    }
}
