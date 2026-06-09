param(
    [ValidatePattern("^\d+\.\d+\.\d+$")]
    [string]$Version = "1.0.0",

    [string]$InnoCompiler = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

& (Join-Path $PSScriptRoot "Publish-V1.ps1") -Version $Version
if ($LASTEXITCODE -ne 0) {
    throw "Publish release thất bại."
}

if (-not (Test-Path -LiteralPath $InnoCompiler)) {
    throw "Không tìm thấy Inno Setup Compiler tại: $InnoCompiler"
}

$installerScript = Join-Path $repoRoot "installer\FileUtilityHub.iss"
& $InnoCompiler "/DAppVersion=$Version" $installerScript
if ($LASTEXITCODE -ne 0) {
    throw "Build installer thất bại."
}

$installer = Join-Path $repoRoot "artifacts\installer\FileUtilityHub-Setup-v$Version-win-x64.exe"
if (-not (Test-Path -LiteralPath $installer)) {
    throw "Không tìm thấy file installer sau khi build: $installer"
}

Write-Host "Installer ready: $installer"
