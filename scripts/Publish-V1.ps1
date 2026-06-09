param(
    [ValidateSet("win-x64")]
    [string]$Runtime = "win-x64",

    [ValidatePattern("^\d+\.\d+\.\d+$")]
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$project = Join-Path $repoRoot "src\FileUtilityHub.WinUI\FileUtilityHub.WinUI.csproj"
$ffmpeg = Join-Path $repoRoot "src\FileUtilityHub.WinUI\Tools\ffmpeg\bin\ffmpeg.exe"
$artifactsRoot = Join-Path $repoRoot "artifacts"
$publishDir = Join-Path $artifactsRoot "release\$Runtime"
$zipPath = Join-Path $artifactsRoot "FileUtilityHub-v$Version-$Runtime.zip"

if (-not (Test-Path -LiteralPath $ffmpeg)) {
    throw "Thiếu FFmpeg bundled tại: $ffmpeg"
}

if (Test-Path -LiteralPath $publishDir) {
    $resolvedArtifacts = (Resolve-Path -LiteralPath $artifactsRoot).Path
    $resolvedPublish = (Resolve-Path -LiteralPath $publishDir).Path
    if (-not $resolvedPublish.StartsWith($resolvedArtifacts, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Từ chối xóa thư mục ngoài artifacts: $resolvedPublish"
    }
    Remove-Item -LiteralPath $resolvedPublish -Recurse -Force
}

dotnet clean $project -c Release -r $Runtime
if ($LASTEXITCODE -ne 0) {
    throw "dotnet clean thất bại."
}

dotnet publish $project `
    -c Release `
    -r $Runtime `
    --self-contained false `
    -p:Version=$Version `
    -p:WindowsAppSDKSelfContained=true `
    -p:PublishTrimmed=false `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish thất bại."
}

$appPri = Join-Path $publishDir "FileUtilityHub.WinUI.pri"
if (-not (Test-Path -LiteralPath $appPri)) {
    throw "Thiếu FileUtilityHub.WinUI.pri trong thư mục publish. App sẽ crash khi khởi động."
}

& (Join-Path $publishDir "Tools\ffmpeg\bin\ffmpeg.exe") -version | Select-Object -First 1
if ($LASTEXITCODE -ne 0) {
    throw "FFmpeg bundled không chạy được trong thư mục publish."
}

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -CompressionLevel Optimal
Write-Host "Release ready: $zipPath"
