# Downloads Steam Audio + FMOD for Unity source archives into _Downloads for manual import.
param(
    [string]$RepoRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = "Stop"
$downloadDir = Join-Path $RepoRoot "_Downloads"
New-Item -ItemType Directory -Force -Path $downloadDir | Out-Null

$steamZip = Join-Path $downloadDir "steamaudio_unity.zip"
$fmodZip = Join-Path $downloadDir "fmod-unity-integration.zip"

Write-Host "Downloading Steam Audio Unity integration..."
Invoke-WebRequest -Uri "https://github.com/ValveSoftware/steam-audio/releases/download/v4.5.3/steamaudio_unity.zip" -OutFile $steamZip -UseBasicParsing

Write-Host "Done. Import in Unity:"
Write-Host "  1. Extract steamaudio_unity.zip"
Write-Host "  2. Import unitypackage from unity/ folder"
Write-Host "  3. Import SteamAudioFMODStudio.unitypackage"
Write-Host ""
Write-Host "FMOD for Unity must be downloaded from https://www.fmod.com/download (login required)."
Write-Host "Place the .unitypackage in $downloadDir and import via Unity."
