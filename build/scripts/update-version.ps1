param(
    [Parameter(Mandatory=$true)]
    [string]$VersionFile,

    [Parameter(Mandatory=$true)]
    [string]$SourceRoot,

    # Matches MSBuild properties
    [string]$MajorType = "None",
    [string]$MinorType = "None",
    [string]$BuildType = "Increment",
    [string]$RevisionType = "Reset",

    # Optional: add suffix/tag between core version and closing group
    [string]$VersionTag = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Read-Version([string]$path) {
    if (!(Test-Path -LiteralPath $path)) { return @(1,0,0,0) }  # sane default
    $line = (Get-Content -LiteralPath $path -TotalCount 1).Trim()
    if ([string]::IsNullOrWhiteSpace($line)) { return @(1,0,0,0) }

    $m = [regex]::Match($line, '^\s*(\d+)\.(\d+)\.(\d+)\.(\d+)\s*$')
    if (!$m.Success) { throw "VersionFile '$path' does not contain a 4-part version (e.g. 1.0.161.0). Found: '$line'" }

    return @(
        [int]$m.Groups[1].Value,
        [int]$m.Groups[2].Value,
        [int]$m.Groups[3].Value,
        [int]$m.Groups[4].Value
    )
}

function Apply-Change([int]$value, [string]$changeType) {
    switch ($changeType) {
        "None"      { return $value }
        "Increment" { return ($value + 1) }
        "Reset"     { return 0 }
        default     { throw "Unknown change type '$changeType' (expected None|Increment|Reset)" }
    }
}

# Read current version
$major, $minor, $build, $rev = Read-Version $VersionFile

# Compute next version with simple cascading rules:
# - If Major increments/resets, reset Minor/Build/Rev to 0 unless explicitly overridden.
# - If Minor increments/resets, reset Build/Rev to 0 unless explicitly overridden.
# - If Build increments/resets, reset Rev to 0 unless explicitly overridden.
#
# This matches typical behavior of these build scripts.
$origMajor, $origMinor, $origBuild, $origRev = $major, $minor, $build, $rev

$major2 = Apply-Change $major $MajorType
$majorChanged = ($major2 -ne $origMajor)
if ($majorChanged) {
    $minor = 0; $build = 0; $rev = 0
}

$minor2 = Apply-Change $minor $MinorType
$minorChanged = ($minor2 -ne $origMinor) -or $majorChanged
if ($minorChanged -and $MinorType -ne "None") {
    $build = 0; $rev = 0
}

$build2 = Apply-Change $build $BuildType
$buildChanged = ($build2 -ne $origBuild) -or ($MinorType -ne "None") -or $majorChanged
if ($BuildType -ne "None" -and $build2 -ne $origBuild) {
    $rev = 0
}

$rev2 = Apply-Change $rev $RevisionType

# Final values
$major = $major2
$minor = $minor2
$build = $build2
$rev   = $rev2

# Write updated version file (currently always 4-part)
$newVersion = "$major.$minor.$build.$rev"
$dir = Split-Path -Parent $VersionFile
if ($dir -and !(Test-Path -LiteralPath $dir)) { New-Item -ItemType Directory -Path $dir | Out-Null }
Set-Content -LiteralPath $VersionFile -Value $newVersion -Encoding ASCII

Write-Host "Updated version: $newVersion (wrote $VersionFile)"

# Update AssemblyInfo.* files under SourceRoot
$files = Get-ChildItem -LiteralPath $SourceRoot -Recurse -File -Include "AssemblyInfo.*" -ErrorAction Stop

$regexes = @(
    @{ Precision = 4; Pattern = "(?'BeforeVersion'AssemblyVersion\(%22)(?'CoreVersion'(\*|\d+)\.)+(\*|\d+)(?'AfterVersion'%22\))" },
    @{ Precision = 4; Pattern = "(?'BeforeVersion'AssemblyFileVersion\(%22)(?'CoreVersion'(\*|\d+)\.)+(\*|\d+)(?'AfterVersion'%22\))" }
)

foreach ($f in $files) {
    $text = Get-Content -LiteralPath $f.FullName -Raw
    $updated = $false

    foreach ($rx in $regexes) {
        $pattern = $rx.Pattern
        # Replace with same wrapping text but new version
        $repl = '${BeforeVersion}' + $newVersion + $VersionTag + '${AfterVersion}'
        $newText = [regex]::Replace($text, $pattern, $repl)

        if ($newText -ne $text) {
            $text = $newText
            $updated = $true
        }
    }

    if ($updated) {
        Set-Content -LiteralPath $f.FullName -Value $text -Encoding UTF8
        Write-Host "Updated: $($f.FullName)"
    }
}
