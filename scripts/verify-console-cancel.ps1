param([Parameter(Mandatory = $true)][string]$Executable,
      [Parameter(Mandatory = $true)][string]$EvidenceDirectory)
$ErrorActionPreference = 'Stop'
$originalPathExt = $env:PATHEXT
$temporary = Join-Path ([IO.Path]::GetTempPath()) ('AlgorithmLab-CancelProbe-' + [Guid]::NewGuid().ToString('N'))
try {
    $env:PATHEXT = [Environment]::GetEnvironmentVariable('PATHEXT', 'Machine')
    $null = New-Item -ItemType Directory -Path $temporary
    $null = New-Item -ItemType Directory -Path $EvidenceDirectory -Force
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    $compiler = @(& $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.Roslyn.Compiler -find 'MSBuild\**\Bin\Roslyn\csc.exe')[0]
    if (-not $compiler) { throw 'C# compiler was not found.' }
    $probe = Join-Path $temporary 'ConsoleCancelProbe.exe'
    & $compiler -nologo -target:exe -platform:x64 -langversion:7.3 "-out:$probe" (Join-Path $PSScriptRoot 'ConsoleCancelProbe.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Console probe compilation failed.' }
    $data = Join-Path $temporary 'store.json'
    $transcript = Join-Path $EvidenceDirectory 'console-cancel-transcript.txt'
    & $probe $Executable $data $transcript
    if ($LASTEXITCODE -ne 0) { throw 'Isolated console event probe failed. See transcript.' }
    $document = Get-Content -LiteralPath $data -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($document.benchmarkRuns.Count -ne 1 -or $document.benchmarkRuns[0].status -ne 'Cancelled' -or $document.benchmarkRuns[0].errorCode -ne 'USER_CANCELLED' -or $null -ne $document.benchmarkRuns[0].medianMs) {
        throw 'CTRL_C_EVENT did not persist a Cancelled/USER_CANCELLED result without aggregate score.'
    }
    Copy-Item -LiteralPath $data -Destination (Join-Path $EvidenceDirectory 'console-cancel-store.json')
    Write-Output 'PASS: isolated Windows CTRL_C_EVENT -> Cancelled / USER_CANCELLED; no aggregate score.'
}
finally {
    $env:PATHEXT = $originalPathExt
    if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Recurse -Force }
}
