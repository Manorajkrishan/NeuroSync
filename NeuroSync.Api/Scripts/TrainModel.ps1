# PowerShell Script to Manually Train the ML Model
# This ensures training happens with visible output

Write-Host "Training ML Model with 10,000 examples..." -ForegroundColor Green
Write-Host ""

$projectRoot = Split-Path -Parent $PSScriptRoot
$projectRoot = Split-Path -Parent $projectRoot

# Check dataset
$datasetPath = Join-Path $projectRoot "NeuroSync.Api\Data\emotions.csv"
if (-not (Test-Path $datasetPath)) {
    Write-Host "ERROR: Dataset not found at: $datasetPath" -ForegroundColor Red
    exit 1
}

$lineCount = (Get-Content $datasetPath | Measure-Object -Line).Lines
Write-Host "Dataset: $datasetPath ($lineCount lines)" -ForegroundColor Cyan

# Delete old model to force retraining
$modelPath = Join-Path $projectRoot "NeuroSync.Api\Models\emotion-model.zip"
if (Test-Path $modelPath) {
    Remove-Item $modelPath -Force
    Write-Host "Deleted old model file" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Starting application - training will happen automatically..." -ForegroundColor Yellow
Write-Host "You should see training output like:" -ForegroundColor Cyan
Write-Host "  - Training model with 10,000 examples..." -ForegroundColor Gray
Write-Host "  - Training set: 8,000 examples" -ForegroundColor Gray
Write-Host "  - Test set: 2,000 examples" -ForegroundColor Gray
Write-Host "  - Training started..." -ForegroundColor Gray
Write-Host "  - Model Accuracy: XX.XX%" -ForegroundColor Gray
Write-Host ""
Write-Host "Press Ctrl+C after training completes to stop the server." -ForegroundColor Yellow
Write-Host ""

Set-Location (Join-Path $projectRoot "NeuroSync.Api")
dotnet run
