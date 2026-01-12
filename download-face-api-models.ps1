# PowerShell script to download face-api.js models locally
# This makes the models visible in your project and enables offline use

$baseUrl = "https://raw.githubusercontent.com/justadudewhohacks/face-api.js/master/weights"
$outputDir = "NeuroSync.Api\wwwroot\models\face-api"

# Create output directory
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

Write-Host "Downloading face-api.js models..." -ForegroundColor Green

# List of model files to download
$models = @(
    @{ Name = "tiny_face_detector_model-weights_manifest.json"; Size = "~190KB" },
    @{ Name = "tiny_face_detector_model-shard1"; Size = "~190KB" },
    @{ Name = "face_landmark_68_model-weights_manifest.json"; Size = "~350KB" },
    @{ Name = "face_landmark_68_model-shard1"; Size = "~350KB" },
    @{ Name = "face_recognition_model-weights_manifest.json"; Size = "~6MB" },
    @{ Name = "face_recognition_model-shard1"; Size = "~6MB" },
    @{ Name = "face_expression_model-weights_manifest.json"; Size = "~310KB" },
    @{ Name = "face_expression_model-shard1"; Size = "~310KB" }
)

$total = $models.Count
$current = 0

foreach ($model in $models) {
    $current++
    $url = "$baseUrl/$($model.Name)"
    $outputPath = Join-Path $outputDir $model.Name
    
    Write-Host "[$current/$total] Downloading $($model.Name) ($($model.Size))..." -ForegroundColor Yellow
    
    try {
        Invoke-WebRequest -Uri $url -OutFile $outputPath -UseBasicParsing
        Write-Host "  ✓ Downloaded successfully" -ForegroundColor Green
    } catch {
        Write-Host "  ✗ Failed to download: $_" -ForegroundColor Red
    }
}

Write-Host "`nDownload complete!" -ForegroundColor Green
Write-Host "Models saved to: $outputDir" -ForegroundColor Cyan
Write-Host "Total size: ~7MB" -ForegroundColor Cyan
Write-Host "`nNext step: Update facial-detection.js to use local models" -ForegroundColor Yellow

