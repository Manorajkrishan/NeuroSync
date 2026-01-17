# PowerShell Script to Generate 10,000 Emotion Dataset Examples
# This creates a balanced dataset for training

Write-Host "Generating 10,000 emotion examples..." -ForegroundColor Green
Write-Host ""

# Set paths
$scriptDir = Split-Path -Parent $PSScriptRoot
$dataDir = Join-Path $scriptDir "Data"
$outputFile = Join-Path $dataDir "emotions.csv"

# Create Data directory if it doesn't exist
if (-not (Test-Path $dataDir)) {
    New-Item -ItemType Directory -Path $dataDir -Force | Out-Null
    Write-Host "Created Data directory: $dataDir" -ForegroundColor Green
}

# Emotion templates with variations
$emotionTemplates = @{
    "happy" = @(
        "I'm feeling great!", "I'm so happy today!", "This makes me happy", "I feel joyful", "I'm delighted",
        "What a wonderful day!", "I'm thrilled!", "This is amazing!", "I feel fantastic!", "I'm ecstatic",
        "I'm overjoyed", "This brings me joy", "I'm feeling wonderful", "I'm so pleased", "I'm on cloud nine"
    )
    "sad" = @(
        "I'm feeling sad", "I feel down", "This makes me sad", "I'm upset", "I feel terrible",
        "I'm feeling blue", "I feel miserable", "This is heartbreaking", "I'm depressed", "I feel hopeless",
        "I'm feeling low", "This is so disappointing", "I feel empty inside", "I'm feeling gloomy", "I feel awful"
    )
    "angry" = @(
        "I'm so angry!", "This makes me furious", "I'm mad about this", "I'm frustrated", "This is infuriating",
        "I'm livid", "I'm really upset", "This makes me rage", "I'm annoyed", "I'm irritated",
        "I'm furious about this", "This is so irritating", "I'm seething", "I'm fuming", "This angers me"
    )
    "anxious" = @(
        "I'm really worried", "I feel anxious", "I'm nervous about this", "I'm feeling anxious", "This makes me nervous",
        "I'm stressed out", "I'm feeling worried", "I have anxiety", "I'm concerned", "I'm feeling uneasy",
        "I'm panicking", "I'm really anxious", "I'm feeling tense", "I'm worried sick", "This gives me anxiety"
    )
    "calm" = @(
        "I feel calm", "I'm feeling peaceful", "I'm relaxed", "I feel at ease", "I'm feeling serene",
        "I feel tranquil", "I'm at peace", "I'm feeling zen", "I feel composed", "I'm feeling mellow",
        "I'm feeling placid", "I feel relaxed and calm", "I'm feeling quiet", "I feel balanced", "I'm feeling steady"
    )
    "excited" = @(
        "I'm so excited!", "This is exciting!", "I'm thrilled about this", "I can't wait!", "I'm pumped!",
        "I'm exhilarated", "This is awesome!", "I'm so hyped", "I'm ecstatic", "I'm really excited",
        "This is thrilling!", "I'm buzzing with excitement", "I'm so stoked", "I'm amped up", "I'm elated"
    )
    "frustrated" = @(
        "This is so frustrating!", "I'm frustrated", "This is annoying", "I'm feeling frustrated", "This irritates me",
        "I'm really frustrated", "This is exasperating", "I'm fed up", "This is so irritating", "I'm getting frustrated",
        "This is maddening", "I'm really annoyed", "This is so annoying", "I'm losing patience", "This is so irritating"
    )
    "neutral" = @(
        "I'm okay", "I feel fine", "Nothing special", "I'm doing alright", "Everything is normal",
        "I'm okay with this", "I feel neutral", "I'm indifferent", "I feel average", "I'm doing okay",
        "Nothing much", "I'm feeling okay", "Everything is fine", "I'm alright", "I feel normal"
    )
}

# Generate 10,000 examples (balanced distribution)
$allExamples = @()
$examplesPerEmotion = [Math]::Floor(10000 / $emotionTemplates.Count)
$random = New-Object System.Random

Write-Host "Generating examples for each emotion..." -ForegroundColor Yellow

foreach ($emotion in $emotionTemplates.Keys) {
    $templates = $emotionTemplates[$emotion]
    
    for ($i = 0; $i -lt $examplesPerEmotion; $i++) {
        # Pick a random template
        $baseText = $templates[$random.Next($templates.Count)]
        
        # Add variations
        $variations = @("", " today", " right now", " about this", " lately")
        $variation = $variations[$random.Next($variations.Length)]
        $text = $baseText + $variation
        
        # Add to examples
        $allExamples += [PSCustomObject]@{
            Text = $text
            Label = $emotion
        }
    }
    
    Write-Host "  Generated $examplesPerEmotion examples for: $emotion" -ForegroundColor Gray
}

# Shuffle examples
Write-Host ""
Write-Host "Shuffling examples..." -ForegroundColor Yellow
$shuffled = $allExamples | Sort-Object { $random.Next() }

# Write to CSV
Write-Host "Writing to CSV..." -ForegroundColor Yellow

$outputLines = @("Text,Label")
foreach ($example in $shuffled) {
    $escapedText = $example.Text.Replace('"', '""')
    $outputLines += '"' + $escapedText + '",' + $example.Label
}

$outputLines | Out-File -FilePath $outputFile -Encoding UTF8 -Force

Write-Host ""
Write-Host "Dataset created successfully!" -ForegroundColor Green
Write-Host "Total examples: $($shuffled.Count)" -ForegroundColor Cyan
Write-Host "Saved to: $outputFile" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Delete old model: Remove-Item '$scriptDir\Models\emotion-model.zip' -ErrorAction SilentlyContinue" -ForegroundColor White
Write-Host "2. Run application: dotnet run" -ForegroundColor White
Write-Host "3. The model will automatically retrain with your new dataset!" -ForegroundColor White
Write-Host ""
