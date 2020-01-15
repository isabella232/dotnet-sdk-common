param(
    [string]$step
)

# Performs a delegated release step in a CircleCI Windows container using PowerShell. This
# mechanism is described in scripts/circleci/README.md. All of the necessary environment
# variables should already be in the generated CircleCI configuration.

$ErrorActionPreference = "Stop"

$repoDir = Get-Location
$circleCIWindowsDir = split-path -parent $MyInvocation.MyCommand.Definition
$circleCIDir = split-path -parent $circleCIWindowsDir
$ldReleaseDir = split-path -parent $circleCIDir

New-Item -Path "$repoDir/artifacts" -ItemType "directory" -Force | Out-Null

Write-Host
Write-Host "[$step] " -NoNewline
$scriptName = "$step.ps1"
$projectHostSpecificScriptPath = "$ldReleaseDir\windows-$scriptName"
$projectScriptPath = "$ldReleaseDir\$scriptName"
$templateScriptPath = "$circleCIDir\template\$scriptName"
if (Test-Path -Path $projectHostSpecificScriptPath) {
    Write-Host "executing $projectHostSpecificScriptPath"
    & $projectHostSpecificScriptPath
} elseif (Test-Path -Path $projectScriptPath) {
    Write-Host "executing $projectScriptPath"
    & $projectScriptPath
} elseif (Test-Path -Path $templateScriptPath) {
    Write-Host "executing $templateScriptPath"
    & $templateScriptPath
} else {
    Write-Host "script $scriptName is undefined, skipping"
}
