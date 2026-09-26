param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$projectDir = $PSScriptRoot
$projectFile = Join-Path $projectDir "KassenDoubleTapScreen.csproj"
$publishDir = Join-Path $projectDir "bin\$Configuration\net9.0-android\publish"
$finalApkPath = Join-Path $projectDir "KassenDoubleTap_XA02Pro.apk"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Membangun APK Double Tap Kassen XA 02 Pro ($Configuration) " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# Jalankan dotnet publish
dotnet publish $projectFile `
    --framework net9.0-android `
    --configuration $Configuration `
    -p:AndroidPackageFormats=apk

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build APK gagal dengan exit code $LASTEXITCODE."
    exit 1
}

# Cari APK yang dihasilkan
$apkFiles = Get-ChildItem (Join-Path $projectDir "bin\$Configuration") -Filter *.apk -File -Recurse | 
            Sort-Object LastWriteTime -Descending

if ($apkFiles.Count -eq 0) {
    Write-Error "File APK tidak ditemukan di folder output."
    exit 1
}

# Ambil APK yang ditandatangani (Signed) jika ada, atau APK pertama
$targetApk = $apkFiles | Where-Object { $_.Name -like "*Signed*.apk" } | Select-Object -First 1
if (-not $targetApk) {
    $targetApk = $apkFiles[0]
}

# Salin ke root folder project dengan nama yang mudah dikenali
Copy-Item $targetApk.FullName -Destination $finalApkPath -Force

$fileSizeMb = [math]::Round((Get-Item $finalApkPath).Length / 1MB, 2)

Write-Host ""
Write-Host "==========================================================" -ForegroundColor Green
Write-Host " APK BERHASIL DIBUAT!" -ForegroundColor Green
Write-Host " Lokasi APK : $finalApkPath" -ForegroundColor Yellow
Write-Host " Ukuran File: $fileSizeMb MB" -ForegroundColor Yellow
Write-Host "==========================================================" -ForegroundColor Green
