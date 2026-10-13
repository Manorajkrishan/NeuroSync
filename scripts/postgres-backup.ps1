param(
    [string]$Output = ""
)

$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($Output)) {
    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $Output = Join-Path "backups" "neurosync-$stamp.sql"
}

$dir = Split-Path -Parent $Output
if (-not [string]::IsNullOrWhiteSpace($dir)) {
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
}

docker compose exec -T db pg_dump -U neurosync -d neurosync --clean --if-exists | Set-Content -Path $Output -Encoding utf8
Write-Output "Wrote $Output"
