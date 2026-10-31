param(
    [Parameter(Mandatory = $true)][string]$ZipPath,
    [Parameter(Mandatory = $true)][string]$EvidenceDirectory
)

$ErrorActionPreference = 'Stop'
$ZipPath = (Resolve-Path -LiteralPath $ZipPath).Path
$EvidenceDirectory = [IO.Path]::GetFullPath($EvidenceDirectory)
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('AlgorithmLab ' + [char]0x671F + [char]0x4E2D + ' RC ' + [Guid]::NewGuid().ToString('N'))
$extractRoot = Join-Path $tempRoot 'extracted package'
New-Item -ItemType Directory -Path $extractRoot, $EvidenceDirectory -Force | Out-Null
try {
    $expectedZipHash = (Get-Content -LiteralPath ($ZipPath + '.sha256.txt') -Raw).Split(' ')[0].Trim()
    $actualZipHash = (Get-FileHash -LiteralPath $ZipPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($expectedZipHash -ne $actualZipHash) { throw 'ZIP SHA256 mismatch.' }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::ExtractToDirectory($ZipPath, $extractRoot)
    $manifest = Get-Content -LiteralPath (Join-Path $extractRoot 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($entry in $manifest.files) {
        $filePath = [IO.Path]::GetFullPath((Join-Path $extractRoot $entry.path))
        if (-not $filePath.StartsWith($extractRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Manifest path escaped package.' }
        $file = Get-Item -LiteralPath $filePath
        if ($file.Length -ne $entry.bytes -or
            (Get-FileHash -LiteralPath $filePath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $entry.sha256) {
            throw "Packaged file mismatch: $($entry.path)"
        }
    }
    $actualFiles = @(Get-ChildItem -LiteralPath $extractRoot -File -Recurse -Force)
    if ($actualFiles.Count -ne $manifest.files.Count + 1) { throw 'Unexpected or missing files in package.' }
    foreach ($required in @('runtime/Start-Demo.cmd', 'runtime/ConsoleApp1.exe', 'source/.gitignore', 'source/ConsoleApp1.sln',
        'source/ConsoleApp1.slnx', 'source/scripts/build.ps1', 'source/docs/acceptance-checklist.md',
        'source/docs/wireframes/winforms-wireframes.pdf', 'source/docs/presentation/midterm-briefing.pptx',
        'source/docs/presentation/midterm-briefing.pdf')) {
        if (-not (Test-Path -LiteralPath (Join-Path $extractRoot $required) -PathType Leaf)) { throw "Missing delivery: $required" }
    }
    if (@($actualFiles | Where-Object { $_.FullName -match '[\\/](\.git|\.vs|bin|obj|logs)[\\/]' }).Count -gt 0) { throw 'Excluded working files found.' }
    $dataPath = Join-Path $tempRoot 'new demo data\store.json'
    function Invoke-Smoke([string]$InputText) {
        $info = New-Object Diagnostics.ProcessStartInfo
        $info.FileName = Join-Path $extractRoot 'runtime\ConsoleApp1.exe'
        $info.Arguments = '--data-path "' + $dataPath + '"'
        $info.WorkingDirectory = $tempRoot
        $info.UseShellExecute = $false
        $info.RedirectStandardInput = $true
        $info.RedirectStandardOutput = $true
        $info.RedirectStandardError = $true
        $info.StandardOutputEncoding = [Text.UTF8Encoding]::new($false)
        $info.StandardErrorEncoding = [Text.UTF8Encoding]::new($false)
        $process = [Diagnostics.Process]::new()
        $process.StartInfo = $info
        try {
            [void]$process.Start()
            $stdoutTask = $process.StandardOutput.ReadToEndAsync()
            $stderrTask = $process.StandardError.ReadToEndAsync()
            $process.StandardInput.Write($InputText)
            $process.StandardInput.Close()
            if (-not $process.WaitForExit(45000)) { $process.Kill(); throw 'Extracted application smoke timed out.' }
            $stdout = $stdoutTask.GetAwaiter().GetResult()
            $stderr = $stderrTask.GetAwaiter().GetResult()
            if ($process.ExitCode -ne 0 -or $stderr.Length -gt 0) { throw "Extracted application failed: $stderr" }
            return $stdout
        }
        finally { $process.Dispose() }
    }
    $transcript = Invoke-Smoke "4`nY`n2`n1`n1`nY`n0`n"
    $saved = Get-Content -LiteralPath $dataPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($saved.testCases.Count -ne 3 -or $saved.benchmarkRuns.Count -ne 1 -or
        $saved.benchmarkRuns[0].status -ne 'Completed' -or $saved.benchmarkRuns[0].samples.Count -ne 5) {
        throw 'Extracted package did not save the expected cases and completed run.'
    }
    $beforeReopen = (Get-FileHash -LiteralPath $dataPath -Algorithm SHA256).Hash
    $transcript += Invoke-Smoke "3`n`n`n`n1`n0`n0`n"
    if ((Get-FileHash -LiteralPath $dataPath -Algorithm SHA256).Hash -ne $beforeReopen) { throw 'Read-only reopening changed store contents.' }
    if (-not (Test-Path -LiteralPath ($dataPath + '.bak'))) { throw 'Expected JSON backup was not produced.' }
    [IO.File]::WriteAllText((Join-Path $EvidenceDirectory 'package-smoke-transcript.txt'), $transcript, [Text.UTF8Encoding]::new($false))
    $summary = @(
        'PASS ZIP SHA256; every manifest file length/hash; exact packaged file count.',
        'PASS required runtime/source/documents and exclusions.',
        'PASS extracted Release in fresh Chinese/space path; three cases; one Completed run with five samples; backup.',
        'PASS reopened extracted application; history readable; store bytes unchanged.',
        'Same Windows host/account only; second-machine and classroom rehearsal remain pending.',
        ('Release: ' + $manifest.releaseId), ('Manifest files: ' + $manifest.files.Count), ('ZIP SHA256: ' + $actualZipHash)
    ) -join "`n"
    [IO.File]::WriteAllText((Join-Path $EvidenceDirectory 'package-verification.txt'), $summary, [Text.UTF8Encoding]::new($false))
    Write-Output $summary
}
finally {
    if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
}
