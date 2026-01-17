# PowerShell Script to Remove All .md Files (except README.md) from Git Repository
# This script removes .md files from both local and remote repository

Write-Host "Removing all .md files (except README.md) from Git repository..." -ForegroundColor Yellow
Write-Host ""

# Change to project root
$projectRoot = Split-Path -Parent $PSScriptRoot
$projectRoot = Split-Path -Parent $projectRoot

Set-Location $projectRoot

# Check if we're in a git repository
if (-not (Test-Path ".git")) {
    Write-Host "ERROR: Not in a git repository!" -ForegroundColor Red
    exit 1
}

# Stage all deleted .md files (except README.md)
Write-Host "Staging deleted .md files..." -ForegroundColor Yellow
git add -u

# Check what will be committed
$stagedFiles = git diff --cached --name-only | Select-String "\.md$"

if ($stagedFiles) {
    Write-Host ""
    Write-Host "Files that will be removed from repository:" -ForegroundColor Cyan
    $stagedFiles | ForEach-Object { Write-Host "  - $_" -ForegroundColor Gray }
    Write-Host ""
    
    # Commit the deletions
    Write-Host "Committing deletions..." -ForegroundColor Yellow
    git commit -m "Remove all .md documentation files except README.md"
    
    Write-Host ""
    Write-Host "SUCCESS! Deleted .md files have been committed." -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Push to remote: git push" -ForegroundColor White
    Write-Host "   Or: git push origin main" -ForegroundColor White
    Write-Host "   Or: git push origin master" -ForegroundColor White
    Write-Host ""
    Write-Host "Note: The files will be removed from your remote repository after push." -ForegroundColor Gray
} else {
    Write-Host "No .md files to remove. They may already be committed or not in the repository." -ForegroundColor Gray
}
