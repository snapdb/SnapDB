#*******************************************************************************************************
#  update-gemstone-packages.ps1 - Gbtc
#
#  Copyright © 2026, Grid Protection Alliance.  All Rights Reserved.
#
#  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
#  the NOTICE file distributed with this work for additional information regarding copyright ownership.
#  The GPA licenses this file to you under the Eclipse Public License -v 1.0 (the "License"); you may
#  not use this file except in compliance with the License. You may obtain a copy of the License at:
#
#      http://www.opensource.org/licenses/eclipse-1.0.php
#
#  Unless agreed to in writing, the subject software distributed under the License is distributed on an
#  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
#  License for the specific language governing permissions and limitations.
#
#  Code Modification History:
#  -----------------------------------------------------------------------------------------------------
#  02/16/2026 - J. Ritchie Carroll with GitHub Copilot Agent using Claude Opus 4.6
#       Generated original version of source code.
#
#*******************************************************************************************************

param(
    [Parameter(Mandatory=$true)]
    [string]$SourceCsprojPath,

    [Parameter(Mandatory=$true)]
    [string]$TargetCsprojPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Validate that source csproj exists
if (!(Test-Path -LiteralPath $SourceCsprojPath)) {
    Write-Error "Source csproj not found: $SourceCsprojPath"
    exit 1
}

# Validate that target csproj exists
if (!(Test-Path -LiteralPath $TargetCsprojPath)) {
    Write-Error "Target csproj not found: $TargetCsprojPath"
    exit 1
}

# Extract version from source Gemstone.Common.csproj using XPath to avoid
# strict mode errors when multiple PropertyGroup elements exist without Version
[xml]$sourceXml = Get-Content -LiteralPath $SourceCsprojPath -Raw
$versionNode = $sourceXml.SelectSingleNode("//PropertyGroup/Version")

if ($null -eq $versionNode -or [string]::IsNullOrWhiteSpace($versionNode.InnerText)) {
    Write-Error "Could not extract Version from $SourceCsprojPath"
    exit 1
}

$version = $versionNode.InnerText.Trim()
Write-Host "Gemstone version extracted: $version"

# Read target csproj and update Gemstone PackageReference versions
$content = Get-Content -LiteralPath $TargetCsprojPath -Raw

# Match PackageReference lines for Gemstone.* packages and update the Version attribute
$pattern = '(<PackageReference\s+Include="Gemstone\.[^"]*"\s+Version=")([^"]+)(")'
$newContent = [regex]::Replace($content, $pattern, "`${1}$version`${3}")

if ($newContent -eq $content) {
    Write-Host "No Gemstone PackageReference updates needed in $TargetCsprojPath"
} else {
    Set-Content -LiteralPath $TargetCsprojPath -Value $newContent -Encoding UTF8 -NoNewline
    Write-Host "Updated Gemstone PackageReference versions to $version in $TargetCsprojPath"
}
