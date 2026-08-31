[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [ValidatePattern("^win-(x64|arm64)$")]
    [string]$Runtime = "win-x64",

    [ValidatePattern("^\d+\.\d+\.\d+([-.][0-9A-Za-z.-]+)?$")]
    [string]$Version = "1.0.0"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$projectPath = Join-Path $repositoryRoot "src\PulsePing.Native\PulsePing.Native.csproj"
$artifactsRoot = Join-Path $repositoryRoot "artifacts"
$publishDirectory = Join-Path $artifactsRoot "publish-$Runtime"
$releasePath = Join-Path $artifactsRoot "PulsePing1.0.exe"

if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
    throw "Project file not found: $projectPath"
}

if (Test-Path -LiteralPath $publishDirectory) {
    $resolvedPublish = [System.IO.Path]::GetFullPath($publishDirectory)
    $resolvedArtifacts = [System.IO.Path]::GetFullPath($artifactsRoot)
    $allowedPrefix = $resolvedArtifacts.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar

    if (-not $resolvedPublish.StartsWith($allowedPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove a publish directory outside the artifacts folder: $resolvedPublish"
    }

    Remove-Item -LiteralPath $resolvedPublish -Recurse -Force
}

New-Item -ItemType Directory -Path $publishDirectory -Force | Out-Null

dotnet publish $projectPath `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    --output $publishDirectory `
    -p:Version=$Version `
    -p:ContinuousIntegrationBuild=true

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$publishedExecutable = Join-Path $publishDirectory "PulsePing.exe"
if (-not (Test-Path -LiteralPath $publishedExecutable -PathType Leaf)) {
    throw "Published executable not found: $publishedExecutable"
}

Copy-Item -LiteralPath $publishedExecutable -Destination $releasePath -Force
$hash = Get-FileHash -LiteralPath $releasePath -Algorithm SHA256

Write-Host "Built unsigned release: $releasePath"
Write-Host "SHA-256: $($hash.Hash)"
