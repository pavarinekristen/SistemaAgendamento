param(
    [string]$Version = ""
)

$ErrorActionPreference = "Stop"
$Root = Split-Path $PSScriptRoot -Parent
$ISCC = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

if (-not (Test-Path $ISCC)) {
    Write-Error "Inno Setup nao encontrado em '$ISCC'."
}

if (-not $Version) {
    $csproj = [xml](Get-Content "$Root\AgendamentoWpfApp.csproj")
    $Version = $csproj.Project.PropertyGroup.Version | Select-Object -First 1
    if (-not $Version) { $Version = "1.0.0" }
}

$PublishDir = "$Root\artifacts\installer\publish"
$OutputDir = "$Root\artifacts\installer"
$IconPath = "$Root\Assets\SparkCore.ico"

Write-Host "Versao: $Version"

if (-not (Test-Path $IconPath)) {
    Write-Error "Icone nao encontrado: $IconPath"
}

if (Test-Path $PublishDir) {
    Remove-Item -LiteralPath $PublishDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

Write-Host "`n[1/2] Publicando SparkCore com updater..."
& dotnet publish "$Root\AgendamentoWpfApp.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:Version=$Version `
    -o $PublishDir
if ($LASTEXITCODE -ne 0) { Write-Error "dotnet publish falhou." }

Write-Host "`n[2/2] Compilando instalador..."
$IssFile = "$Root\Installer\SparkCore.iss"
$InstallerPath = "$OutputDir\SparkCore-Setup-$Version.exe"
if (Test-Path $InstallerPath) {
    Remove-Item -LiteralPath $InstallerPath -Force
}

& $ISCC $IssFile `
    "/DAppVersion=$Version" `
    "/DAppIcon=$IconPath" `
    "/DSourceDir=$PublishDir" `
    "/DOutputDir=$OutputDir"
if ($LASTEXITCODE -ne 0) { Write-Error "ISCC falhou." }

$installer = Get-Item $InstallerPath -ErrorAction SilentlyContinue
if ($installer) {
    $mb = [math]::Round($installer.Length / 1MB, 1)
    Write-Host "`nInstalador gerado com sucesso."
    Write-Host "Arquivo: $($installer.FullName)"
    Write-Host "Tamanho: $mb MB"
} else {
    Write-Error "Instalador nao encontrado apos compilacao."
}
