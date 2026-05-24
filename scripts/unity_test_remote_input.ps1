param(
    [string]$UnityPath = "",
    [string]$ProjectPath = "",
    [string]$LogPath = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}

if ([string]::IsNullOrWhiteSpace($LogPath)) {
    $LogDirectory = Join-Path $ProjectPath "output/logs"
    New-Item -ItemType Directory -Force -Path $LogDirectory | Out-Null
    $LogPath = Join-Path $LogDirectory ("unity_remote_input_{0}.log" -f (Get-Date -Format "yyyyMMdd_HHmmss"))
}

if ([string]::IsNullOrWhiteSpace($UnityPath)) {
    $ProjectVersionPath = Join-Path $ProjectPath "ProjectSettings/ProjectVersion.txt"
    $EditorVersion = ""
    if (Test-Path $ProjectVersionPath) {
        $VersionLine = Select-String -Path $ProjectVersionPath -Pattern "^m_EditorVersion:\s*(.+)$" | Select-Object -First 1
        if ($VersionLine) {
            $EditorVersion = $VersionLine.Matches[0].Groups[1].Value.Trim()
        }
    }

    $Candidates = @()
    if ($env:UNITY_EXE) {
        $Candidates += $env:UNITY_EXE
    }

    if ($EditorVersion) {
        $Candidates += "C:\Program Files\Unity\Hub\Editor\$EditorVersion\Editor\Unity.exe"
    }

    $Candidates += "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe"

    foreach ($Candidate in $Candidates) {
        if (![string]::IsNullOrWhiteSpace($Candidate) -and (Test-Path $Candidate)) {
            $UnityPath = $Candidate
            break
        }
    }
}

if ([string]::IsNullOrWhiteSpace($UnityPath) -or !(Test-Path $UnityPath)) {
    throw "Unity.exe not found. Pass -UnityPath or set UNITY_EXE."
}

$LogDirectoryPath = Split-Path -Parent $LogPath
if (![string]::IsNullOrWhiteSpace($LogDirectoryPath)) {
    New-Item -ItemType Directory -Force -Path $LogDirectoryPath | Out-Null
}

Write-Host "Unity: $UnityPath"
Write-Host "Project: $ProjectPath"
Write-Host "Log: $LogPath"

& $UnityPath -batchmode -quit -projectPath $ProjectPath -executeMethod RemoteInputSelfTests.RunAll -logFile $LogPath
$ExitCode = $LASTEXITCODE
if ($ExitCode -ne 0) {
    throw "Unity remote input tests failed with exit code $ExitCode. See log: $LogPath"
}

$LogText = ""
$Deadline = (Get-Date).AddSeconds(20)
while ((Get-Date) -lt $Deadline) {
    if (Test-Path $LogPath) {
        $LogText = Get-Content -Path $LogPath -Raw
        if ($LogText -match "REMOTE_INPUT_TEST_PASSED") {
            break
        }
    }

    Start-Sleep -Milliseconds 250
}

if ($LogText -notmatch "REMOTE_INPUT_TEST_PASSED") {
    throw "Unity remote input tests completed without REMOTE_INPUT_TEST_PASSED. See log: $LogPath"
}

Write-Host "REMOTE_INPUT_TEST_PASSED"
Write-Host "Log: $LogPath"
