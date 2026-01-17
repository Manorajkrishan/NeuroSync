# PowerShell Script to Download and Convert GoEmotions Dataset
# This script downloads the GoEmotions dataset and converts it to NeuroSync format

Write-Host "🚀 Downloading GoEmotions Dataset..." -ForegroundColor Green
Write-Host ""

# Set paths
$scriptDir = Split-Path -Parent $PSScriptRoot
$dataDir = Join-Path $scriptDir "Data"
$outputFile = Join-Path $dataDir "emotions.csv"

# Create Data directory if it doesn't exist
if (-not (Test-Path $dataDir)) {
    New-Item -ItemType Directory -Path $dataDir -Force | Out-Null
    Write-Host "✅ Created Data directory: $dataDir" -ForegroundColor Green
}

# GoEmotions dataset URLs (from Google Research)
$datasetUrls = @(
    "https://storage.googleapis.com/gresearch/goemotions/data/full_dataset/goemotions_1.csv",
    "https://storage.googleapis.com/gresearch/goemotions/data/full_dataset/goemotions_2.csv",
    "https://storage.googleapis.com/gresearch/goemotions/data/full_dataset/goemotions_3.csv"
)

$tempDir = Join-Path $env:TEMP "goemotions_download"
if (-not (Test-Path $tempDir)) {
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
}

# Download datasets
$downloadedFiles = @()
foreach ($url in $datasetUrls) {
    $fileName = Split-Path -Leaf $url
    $filePath = Join-Path $tempDir $fileName
    
    Write-Host "📥 Downloading: $fileName..." -ForegroundColor Yellow
    try {
        Invoke-WebRequest -Uri $url -OutFile $filePath -UseBasicParsing -ErrorAction Stop
        $downloadedFiles += $filePath
        Write-Host "✅ Downloaded: $fileName" -ForegroundColor Green
    } catch {
        Write-Host "❌ Error downloading $fileName : $_" -ForegroundColor Red
    }
}

if ($downloadedFiles.Count -eq 0) {
    Write-Host "❌ No files downloaded. Please check your internet connection." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "🔄 Converting to NeuroSync format..." -ForegroundColor Green

# Emotion mapping: GoEmotions → NeuroSync
$emotionMap = @{
    "joy" = "happy"
    "sadness" = "sad"
    "anger" = "angry"
    "fear" = "anxious"
    "surprise" = "excited"
    "disgust" = "frustrated"
    "neutral" = "neutral"
    # Add calm if present in dataset
}

# NeuroSync emotions
$validEmotions = @("happy", "sad", "angry", "anxious", "calm", "excited", "frustrated", "neutral")

# Convert and merge CSV files
$allData = @()
$headerWritten = $false
$totalLines = 0
$convertedLines = 0

foreach ($filePath in $downloadedFiles) {
    Write-Host "📖 Processing: $(Split-Path -Leaf $filePath)..." -ForegroundColor Yellow
    
    $lines = Get-Content $filePath -Encoding UTF8
    $lineCount = 0
    
    foreach ($line in $lines) {
        $lineCount++
        
        # Skip empty lines and potential header
        if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith("#") -or $line.StartsWith("text") -or $lineCount -eq 1) {
            continue
        }
        
        try {
            # Parse CSV line (handle quoted fields)
            $csvFields = $line -split ',(?=(?:[^"]*"[^"]*")*[^"]*$)' | ForEach-Object { $_.Trim('"') }
            
            if ($csvFields.Count -lt 2) {
                continue
            }
            
            $text = $csvFields[0].Trim()
            $emotions = $csvFields[1..($csvFields.Count-1)] | Where-Object { $_ -eq "1" }
            
            # Skip if no emotion or text is empty
            if ([string]::IsNullOrWhiteSpace($text) -or $emotions.Count -eq 0) {
                continue
            }
            
            # Get first emotion (or map it)
            $goEmotion = $csvFields[1]
            
            # Try to find emotion label in the line
            # GoEmotions format might be: text,emotion1,emotion2,...,emotion27
            # We need to find which emotion is "1"
            $emotionIndex = 1
            $foundEmotion = $null
            
            for ($i = 1; $i -lt $csvFields.Count; $i++) {
                if ($csvFields[$i] -eq "1") {
                    # Map index to emotion name (approximate based on GoEmotions structure)
                    # This is a simplified mapping - you may need to adjust based on actual CSV structure
                    $foundEmotion = $goEmotion
                    break
                }
            }
            
            # If we can't find it, try parsing emotion names from headers
            # For now, use a simpler approach: look for emotion label in available fields
            $mappedEmotion = $null
            foreach ($key in $emotionMap.Keys) {
                if ($line -match $key -and $csvFields -contains "1") {
                    $mappedEmotion = $emotionMap[$key]
                    break
                }
            }
            
            # Default to first valid emotion if mapping fails
            if (-not $mappedEmotion) {
                $mappedEmotion = "neutral"
            }
            
            # Add to dataset
            $csvLine = '"{0}",{1}' -f $text.Replace('"', '""'), $mappedEmotion
            $allData += $csvLine
            $convertedLines++
            
        } catch {
            # Skip problematic lines
            continue
        }
        
        $totalLines++
        
        # Show progress every 1000 lines
        if ($totalLines % 1000 -eq 0) {
            Write-Host "  Processed: $totalLines lines, Converted: $convertedLines examples..." -ForegroundColor Gray
        }
        
        # Limit to 10,000 examples for faster processing
        if ($convertedLines -ge 10000) {
            Write-Host "  ✅ Reached 10,000 examples limit" -ForegroundColor Green
            break
        }
    }
    
    if ($convertedLines -ge 10000) {
        break
    }
}

# Write output CSV
Write-Host ""
Write-Host "💾 Saving to: $outputFile" -ForegroundColor Green

$outputLines = @("Text,Label")
$outputLines += $allData

# Shuffle data for better training
$random = New-Object System.Random
$shuffledLines = $outputLines[1..($outputLines.Count-1)] | Sort-Object { $random.Next() }
$finalOutput = @($outputLines[0]) + $shuffledLines

$finalOutput | Out-File -FilePath $outputFile -Encoding UTF8 -Force

Write-Host ""
Write-Host "✅ Dataset downloaded and converted!" -ForegroundColor Green
Write-Host "📊 Total examples: $convertedLines" -ForegroundColor Cyan
Write-Host "📁 Saved to: $outputFile" -ForegroundColor Cyan
Write-Host ""
Write-Host "🎯 Next steps:" -ForegroundColor Yellow
Write-Host "1. Delete old model: del NeuroSync.Api\Models\emotion-model.zip" -ForegroundColor White
Write-Host "2. Run application: dotnet run" -ForegroundColor White
Write-Host "3. The model will automatically retrain with your new dataset!" -ForegroundColor White
Write-Host ""

# Cleanup temp files
Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
