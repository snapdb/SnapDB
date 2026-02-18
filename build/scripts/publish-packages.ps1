# Call the script with the path to the project directory as an argument:
#     .\publish-packages.ps1 "C:\Projects\SnapDB\gsfapi"

param(
    [string]$projectDir,
    [string]$buildConfig = "Release"
)

# Script Constants
Set-Variable versionFile -Option Constant -Scope Script -Value "build\scripts\SnapDB.version"
Set-Variable buildFolder  -Option Constant -Scope Script -Value "build\$buildConfig"

# Script Functions

function Publish-Package([string]$package) {
    Invoke-Command -ScriptBlock {
        # Sign NuGet package
        if ($env:NuGetCertFingerprint -ne $null) {
            # Prime the certificate store to avoid issues with signing
            certutil -scinfo | Out-Null     
            & dotnet nuget sign $package --certificate-fingerprint $env:NuGetCertFingerprint --timestamper http://timestamp.digicert.com
            if ($LASTEXITCODE -ne 0) { throw "dotnet nuget sign failed ($LASTEXITCODE) for: $package" }
        }

        # Push package to NuGet
        if ($env:GemstoneNuGetApiKey) {
            Write-Host "Pushing package to NuGet..."
            & dotnet nuget push $package -k $env:GemstoneNuGetApiKey --skip-duplicate -s "https://api.nuget.org/v3/index.json"
            if ($LASTEXITCODE -ne 0) { Write-Warning "NuGet push failed ($LASTEXITCODE) for: $package" }
        }
    } | Write-Host
}

# --------- Start Script ---------

$versionPath = Join-Path $projectDir $versionFile
$buildPath   = Join-Path $projectDir $buildFolder

# Get current SnapDB version (expects a.b.c.d)
$fullVersion = (Get-Content -Path $versionPath -TotalCount 1).Trim()

$parts = $fullVersion.Split('.')

if ($parts.Length -lt 3) {
    throw "Invalid version format '$fullVersion' (expected at least 3 parts)"
}

$packageVersion = ($parts[0..2] -join '.')

Write-Host "Current SnapDB Library version = $fullVersion"
Write-Host "NuGet package version        = $packageVersion"

# Query file system for package files to get proper casing
$packages = [IO.Directory]::GetFiles("$buildPath", "*.$packageVersion.nupkg")

if ($packages.Length -eq 0) {
    Write-Host "WARNING: No SnapDB v$packageVersion package found in $buildPath, build failure? No packages pushed."
}

foreach ($package in $packages) {
    Publish-Package $package
}
