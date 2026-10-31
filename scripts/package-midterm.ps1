param(
    [ValidatePattern('^[A-Za-z0-9._-]+$')]
    [string]$ReleaseId = 'AlgorithmLab-Midterm-2026-10-30-rc1',
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$outputRoot = Join-Path $repoRoot 'artifacts'
$releaseRoot = Join-Path $outputRoot $ReleaseId
$zipPath = $releaseRoot + '.zip'
if ((Test-Path -LiteralPath $releaseRoot) -or (Test-Path -LiteralPath $zipPath)) {
    throw 'Release output already exists. Use a new ReleaseId; existing deliveries are not overwritten.'
}
if (-not $SkipBuild) {
    & (Join-Path $PSHOME 'powershell.exe') -NoProfile -NonInteractive -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'build.ps1') -Configuration Release
    if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }
}
$runtimeInput = Join-Path $repoRoot 'ConsoleApp1\bin\x64\Release'
$runtimeFiles = @('ConsoleApp1.exe', 'ConsoleApp1.exe.config', 'AlgorithmLab.Core.dll', 'AlgorithmLab.Data.dll')
foreach ($name in $runtimeFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $runtimeInput $name))) { throw "Missing runtime file: $name" }
}
$evidenceInput = Join-Path $outputRoot 'validation'
foreach ($name in @('automated-tests.txt', 'console-cancel-transcript.txt', 'console-cancel-store.json', 'fresh-directory-summary.txt')) {
    if (-not (Test-Path -LiteralPath (Join-Path $evidenceInput $name))) { throw "Missing validation evidence: $name" }
}

$runtimeRoot = Join-Path $releaseRoot 'runtime'
$sourceRoot = Join-Path $releaseRoot 'source'
$validationRoot = Join-Path $releaseRoot 'validation'
New-Item -ItemType Directory -Path $runtimeRoot, $sourceRoot, $validationRoot -Force | Out-Null
foreach ($name in $runtimeFiles) { Copy-Item -LiteralPath (Join-Path $runtimeInput $name) -Destination $runtimeRoot }
$launcher = @'
@echo off
setlocal
"%~dp0ConsoleApp1.exe" --data-path "%LOCALAPPDATA%\AlgorithmLab-MidtermDemo\store.json"
set "demoExit=%ERRORLEVEL%"
echo.
echo Application exit code: %demoExit%
pause
exit /b %demoExit%
'@
[IO.File]::WriteAllText((Join-Path $runtimeRoot 'Start-Demo.cmd'), ($launcher -replace '\r?\n', "`r`n"), [Text.Encoding]::ASCII)

# Allowlisted source trees only. Never traverse artifacts, .git, .vs or user data.
$extensions = @('.cs', '.csproj', '.config', '.md', '.ps1', '.py', '.png', '.pdf', '.pptx', '.svg', '.txt', '.json')
$sourceFolders = @('ConsoleApp1', 'AlgorithmLab.Core', 'AlgorithmLab.Data', 'AlgorithmLab.Tests', 'docs', 'scripts')
foreach ($folder in $sourceFolders) {
    $base = Join-Path $repoRoot $folder
    foreach ($file in Get-ChildItem -LiteralPath $base -File -Recurse) {
        $relative = $file.FullName.Substring($repoRoot.Length + 1)
        if ($relative -match '(^|[\\/])(bin|obj|\.vs|\.git|logs|__pycache__)([\\/]|$)') { continue }
        if ($extensions -notcontains $file.Extension.ToLowerInvariant()) { continue }
        if ($file.Extension -eq '.json' -and
            $relative -ne 'docs\evidence\demo-store.json' -and
            -not $relative.StartsWith('AlgorithmLab.Tests\Fixtures\')) { continue }
        $destination = Join-Path $sourceRoot $relative
        New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $destination
    }
}
foreach ($file in Get-ChildItem -LiteralPath $repoRoot -File -Force) {
    if ($file.Extension -in @('.md', '.sln', '.slnx') -or $file.Name -eq '.gitignore') {
        Copy-Item -LiteralPath $file.FullName -Destination $sourceRoot
    }
}
foreach ($name in @('automated-tests.txt', 'console-cancel-transcript.txt', 'console-cancel-store.json', 'fresh-directory-summary.txt')) {
    Copy-Item -LiteralPath (Join-Path $evidenceInput $name) -Destination $validationRoot
}
$readme = @'
Algorithm Lab - Midterm RC1 (2026-10-30)

This candidate passed local validation. A second Windows computer/account and
the classroom keyboard/projector rehearsal still require human sign-off.
See source/docs/acceptance-checklist.md. Fill in team names/class/student IDs.

RUN: extract the entire ZIP, then open runtime/Start-Demo.cmd.
Requires Windows x64 and .NET Framework 4.7.2 or a compatible later 4.x runtime.
No Visual Studio, SQLite or NuGet installation is required to run this build.
The launcher uses %LOCALAPPDATA%\AlgorithmLab-MidtermDemo\store.json.
It preserves existing demonstration data. It never resets personal data.
Direct ConsoleApp1.exe uses the regular AlgorithmLab data folder instead.

BUILD: source/README.md and source/scripts/build.ps1 (Windows Visual Studio).
SLIDES: source/docs/presentation/midterm-briefing.pptx or .pdf (8 pages).
Most slide content is image-based; the identity textbox on slide 1 is editable.
WIREFRAMES: source/docs/wireframes/winforms-wireframes.pdf (3 pages).
DEMO: source/docs/demo-script.md. REPORT: source/docs/midterm-report.md.
DATA/RECOVERY: source/docs/data-format-and-recovery.md.
VALIDATION: validation/ (local tests and actual OS Ctrl+C event evidence).

source/docs/evidence/demo-store.json is a deliberate, generated example,
not the runtime data file. No personal store or logs are included.
manifest.json records every other packaged file, size and SHA-256.
The ZIP SHA-256 sidecar verifies transport, not an external code signature.
Source is a working-tree snapshot; no Git commit or publication is implied.
'@
[IO.File]::WriteAllText((Join-Path $releaseRoot 'README.txt'), $readme, [Text.UTF8Encoding]::new($false))

$files = @(Get-ChildItem -LiteralPath $releaseRoot -File -Recurse -Force | Sort-Object FullName | ForEach-Object {
    [ordered]@{
        path = $_.FullName.Substring($releaseRoot.Length + 1).Replace('\', '/')
        bytes = $_.Length
        sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }
})
$manifest = [ordered]@{
    releaseId = $ReleaseId
    status = 'RC1-local-validation-complete-external-signoff-pending'
    createdUtc = [DateTime]::UtcNow.ToString('o')
    appVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $runtimeRoot 'ConsoleApp1.exe')).FileVersion
    platform = 'Windows x64 / .NET Framework 4.7.2'
    files = $files
}
[IO.File]::WriteAllText((Join-Path $releaseRoot 'manifest.json'), ($manifest | ConvertTo-Json -Depth 6), [Text.UTF8Encoding]::new($false))
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.IO.Compression
# Framework's CreateFromDirectory can emit backslashes. Use standard ZIP '/' paths.
$archive = [IO.Compression.ZipFile]::Open($zipPath, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in Get-ChildItem -LiteralPath $releaseRoot -File -Recurse -Force) {
        $entryName = $file.FullName.Substring($releaseRoot.Length + 1).Replace('\', '/')
        [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $entryName, [IO.Compression.CompressionLevel]::Optimal)
    }
}
finally { $archive.Dispose() }
$zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText(($zipPath + '.sha256.txt'), ($zipHash + '  ' + [IO.Path]::GetFileName($zipPath) + "`n"), [Text.Encoding]::ASCII)
Write-Output "Created: $zipPath"
Write-Output "Files: $($files.Count); SHA256: $zipHash"
