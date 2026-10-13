param(
    [Parameter(Mandatory = $true)]
    [string]$Backup
)

$ErrorActionPreference = "Stop"
if (-not (Test-Path $Backup)) {
    throw "Backup not found: $Backup"
}

$dump = Get-Content -Raw -Path $Backup
docker compose exec -T db psql -U neurosync -d postgres -c "DROP DATABASE IF EXISTS neurosync_restore_test;"
docker compose exec -T db psql -U neurosync -d postgres -c "CREATE DATABASE neurosync_restore_test;"
$dump | docker compose exec -T db psql -U neurosync -d neurosync_restore_test -v ON_ERROR_STOP=1 | Out-Null
$table = docker compose exec -T db psql -U neurosync -d neurosync_restore_test -tAc "SELECT to_regclass('public.""AspNetUsers""');"
if ($table.Trim() -ne "AspNetUsers") {
    throw "Restore test failed: AspNetUsers was not restored (got '$table')."
}
docker compose exec -T db psql -U neurosync -d postgres -c "DROP DATABASE IF EXISTS neurosync_restore_test;"
Write-Output "Restore test passed and the scratch database was dropped."
