param(
    [string]$Version,
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
$Root = Split-Path $PSScriptRoot -Parent

if (-not $Version) {
    Write-Error "Informe a versao. Exemplo: .\scripts\build-update-package.ps1 -Version 1.0.1"
}

$ReleaseRoot = "$Root\artifacts\releases"
$PublishDir = "$ReleaseRoot\sparkcore-$Version\publish"
$ZipPath = "$ReleaseRoot\sparkcore-$Version.zip"
$ManifestPath = "$ReleaseRoot\sparkcore-$Version.manifest.json"

New-Item -ItemType Directory -Force -Path $ReleaseRoot | Out-Null

if (-not $NoBuild) {
    if (Test-Path $PublishDir) {
        Remove-Item -LiteralPath $PublishDir -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null

    Write-Host "[1/4] Publicando SparkCore $Version..."
    & dotnet publish "$Root\AgendamentoWpfApp.csproj" `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:Version=$Version `
        -o $PublishDir
    if ($LASTEXITCODE -ne 0) { Write-Error "dotnet publish falhou." }
} else {
    Write-Host "[1/4] Build ignorado. Usando $PublishDir"
    if (-not (Test-Path $PublishDir)) {
        Write-Error "PublishDir nao encontrado: $PublishDir"
    }
}

if (Test-Path $ZipPath) {
    Remove-Item -LiteralPath $ZipPath -Force
}

Write-Host "[2/4] Criando ZIP de atualizacao..."
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::Open($ZipPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    $files = Get-ChildItem -LiteralPath $PublishDir -Recurse -File |
        Where-Object {
            $_.Extension -ne ".pdb" -and
            $_.Name -ne "SparkCore.Updater.exe"
        }

    $publishFullPath = (Get-Item -LiteralPath $PublishDir).FullName
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($publishFullPath.Length).TrimStart('\', '/')
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $file.FullName, $relative, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
} finally {
    $zip.Dispose()
}

Write-Host "[3/4] Calculando SHA256..."
$sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $ZipPath).Hash
$packageUri = (New-Object System.Uri($ZipPath)).AbsoluteUri

$manifest = [ordered]@{
    Enabled = $true
    Version = $Version
    PackageUrl = $packageUri
    Sha256 = $sha256
    Required = $false
    ReleaseNotes = "Versao $Version"
}

$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $ManifestPath -Encoding UTF8

Write-Host "[4/4] Pacote pronto."
Write-Host "ZIP: $ZipPath"
Write-Host "Manifesto local: $ManifestPath"
Write-Host "PackageUrl local: $packageUri"
Write-Host "SHA256: $sha256"
Write-Host ""
Write-Host "Para teste local, coloque estes valores em RetaguardaAgendamentoAPI\appsettings.json:"
Write-Host ""
Write-Host '"Updates": {'
Write-Host '  "SparkCore": {'
Write-Host '    "Enabled": true,'
Write-Host "    `"Version`": `"$Version`","
Write-Host "    `"PackageUrl`": `"$packageUri`","
Write-Host "    `"Sha256`": `"$sha256`","
Write-Host '    "Required": false,'
Write-Host "    `"ReleaseNotes`": `"Versao $Version`""
Write-Host '  }'
Write-Host '}'
