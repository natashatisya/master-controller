# ============================================
# MinIO Installation Script for Windows
# Net Orchestrator Deployment Script
# ============================================

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  MinIO Installation Starting..." -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Configuration
$MinioDir = "C:\minio"
$MinioDataDir = "C:\minio\data"
$MinioPort = 9000
$MinioConsolePort = 9001
$MinioRootUser = "minioadmin"
$MinioRootPassword = "minioadmin123"
$MinioDownloadUrl = "https://dl.min.io/server/minio/release/windows-amd64/minio.exe"

# Step 1: Create directories
Write-Host "`n[Step 1] Creating MinIO directories..." -ForegroundColor Yellow
try {
    New-Item -ItemType Directory -Force -Path $MinioDir | Out-Null
    New-Item -ItemType Directory -Force -Path $MinioDataDir | Out-Null
    Write-Host "  Directories created: $MinioDir" -ForegroundColor Green
} catch {
    Write-Host "  Error creating directories: $_" -ForegroundColor Red
    exit 1
}

# Step 2: Download MinIO
Write-Host "`n[Step 2] Downloading MinIO executable..." -ForegroundColor Yellow
$MinioExe = "$MinioDir\minio.exe"
try {
    if (Test-Path $MinioExe) {
        Write-Host "  MinIO already downloaded, skipping..." -ForegroundColor Green
    } else {
        Invoke-WebRequest -Uri $MinioDownloadUrl -OutFile $MinioExe -UseBasicParsing
        Write-Host "  MinIO downloaded successfully!" -ForegroundColor Green
    }
} catch {
    Write-Host "  Error downloading MinIO: $_" -ForegroundColor Red
    exit 1
}

# Step 3: Set environment variables
Write-Host "`n[Step 3] Configuring MinIO environment..." -ForegroundColor Yellow
try {
    [System.Environment]::SetEnvironmentVariable("MINIO_ROOT_USER", $MinioRootUser, "Machine")
    [System.Environment]::SetEnvironmentVariable("MINIO_ROOT_PASSWORD", $MinioRootPassword, "Machine")
    $env:MINIO_ROOT_USER = $MinioRootUser
    $env:MINIO_ROOT_PASSWORD = $MinioRootPassword
    Write-Host "  Environment variables set!" -ForegroundColor Green
} catch {
    Write-Host "  Error setting environment variables: $_" -ForegroundColor Red
    exit 1
}

# Step 4: Configure Windows Firewall
Write-Host "`n[Step 4] Configuring firewall rules..." -ForegroundColor Yellow
try {
    netsh advfirewall firewall add rule name="MinIO Server" dir=in action=allow protocol=TCP localport=$MinioPort | Out-Null
    netsh advfirewall firewall add rule name="MinIO Console" dir=in action=allow protocol=TCP localport=$MinioConsolePort | Out-Null
    Write-Host "  Firewall rules added!" -ForegroundColor Green
} catch {
    Write-Host "  Warning: Could not add firewall rules: $_" -ForegroundColor Yellow
}

# Step 5: Start MinIO
Write-Host "`n[Step 5] Starting MinIO server..." -ForegroundColor Yellow
try {
    $existingProcess = Get-Process -Name "minio" -ErrorAction SilentlyContinue
    if ($existingProcess) {
        Write-Host "  MinIO is already running!" -ForegroundColor Green
    } else {
        Start-Process -FilePath $MinioExe `
            -ArgumentList "server $MinioDataDir --console-address :$MinioConsolePort --address :$MinioPort" `
            -WindowStyle Hidden
        Start-Sleep -Seconds 3
        Write-Host "  MinIO server started!" -ForegroundColor Green
    }
} catch {
    Write-Host "  Error starting MinIO: $_" -ForegroundColor Red
    exit 1
}

# Step 6: Verify
Write-Host "`n[Step 6] Verifying MinIO installation..." -ForegroundColor Yellow
Start-Sleep -Seconds 2
try {
    $response = Invoke-WebRequest -Uri "http://localhost:$MinioPort/minio/health/live" -UseBasicParsing -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Host "  MinIO health check passed!" -ForegroundColor Green
    }
} catch {
    Write-Host "  Warning: Health check failed (MinIO may still be starting)" -ForegroundColor Yellow
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  MinIO Installation Complete!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Server URL  : http://localhost:$MinioPort" -ForegroundColor White
Write-Host "  Console URL : http://localhost:$MinioConsolePort" -ForegroundColor White
Write-Host "  Username    : $MinioRootUser" -ForegroundColor White
Write-Host "  Password    : $MinioRootPassword" -ForegroundColor White
Write-Host ""