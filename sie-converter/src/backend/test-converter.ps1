# SIE Converter Test Script
# Tests the SIE parser with the example file

$ErrorActionPreference = "Stop"

Write-Host "=== SIE Converter Test ===" -ForegroundColor Cyan
Write-Host ""

# Check if example file exists
$exampleFile = "..\..\SIE4 Exempelfil.SE"
if (-not (Test-Path $exampleFile)) {
    Write-Error "Example file not found: $exampleFile"
    exit 1
}

Write-Host "Found example file: $exampleFile" -ForegroundColor Green
Write-Host "File size: $([math]::Round((Get-Item $exampleFile).Length / 1KB, 2)) KB" -ForegroundColor Gray
Write-Host ""

# Build the project
Write-Host "Building project..." -ForegroundColor Yellow
dotnet build --configuration Release 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed"
    exit 1
}
Write-Host "Build successful!" -ForegroundColor Green
Write-Host ""

# Start the API in background
Write-Host "Starting API server..." -ForegroundColor Yellow
$apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--configuration", "Release", "--urls", "http://localhost:5101" -PassThru -WindowStyle Hidden

# Wait for API to start
Start-Sleep -Seconds 5

try {
    # Test validation endpoint
    Write-Host "Testing validation endpoint..." -ForegroundColor Yellow
    $form = @{
        file = Get-Item $exampleFile
    }
    
    $response = Invoke-RestMethod -Uri "http://localhost:5101/api/conversion/validate" -Method Post -Form $form -TimeoutSec 30
    
    if ($response.valid) {
        Write-Host "✓ Validation passed!" -ForegroundColor Green
        Write-Host "  Company: $($response.company)" -ForegroundColor Gray
        Write-Host "  Accounts: $($response.accounts)" -ForegroundColor Gray
        Write-Host "  Verifications: $($response.verifications)" -ForegroundColor Gray
        Write-Host "  Version: $($response.version)" -ForegroundColor Gray
    } else {
        Write-Host "✗ Validation failed: $($response.error)" -ForegroundColor Red
    }
    Write-Host ""
    
    # Test conversion endpoint
    Write-Host "Testing conversion endpoint..." -ForegroundColor Yellow
    $outputFile = "test-output.xlsx"
    
    Invoke-RestMethod -Uri "http://localhost:5101/api/conversion/convert" -Method Post -Form $form -OutFile $outputFile -TimeoutSec 60
    
    if (Test-Path $outputFile) {
        $fileSize = [math]::Round((Get-Item $outputFile).Length / 1KB, 2)
        Write-Host "✓ Conversion successful!" -ForegroundColor Green
        Write-Host "  Output: $outputFile ($fileSize KB)" -ForegroundColor Gray
        
        # Cleanup
        Remove-Item $outputFile -Force
        Write-Host "  Cleaned up test output" -ForegroundColor Gray
    } else {
        Write-Host "✗ Conversion failed - no output file" -ForegroundColor Red
    }
    
} catch {
    Write-Host "✗ Test failed: $_" -ForegroundColor Red
} finally {
    # Cleanup API process
    if ($apiProcess -and -not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -Force
        Write-Host ""
        Write-Host "Stopped API server" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "=== Test Complete ===" -ForegroundColor Cyan
