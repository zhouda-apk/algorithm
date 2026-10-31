param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [ValidateSet('sln', 'slnx')]
    [string]$SolutionFormat = 'slnx'
)

$ErrorActionPreference = 'Stop'
$originalPathExt = $env:PATHEXT
$buildExitCode = 1
try {
    $env:PATHEXT = [Environment]::GetEnvironmentVariable('PATHEXT', 'Machine')
    $repoRoot = Split-Path -Parent $PSScriptRoot
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path -LiteralPath $vswhere)) {
        throw 'Visual Studio Installer/vswhere.exe was not found.'
    }
    $candidates = @(& $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe')
    if ($LASTEXITCODE -ne 0 -or $candidates.Count -eq 0) {
        throw 'MSBuild was not found. Install the .NET desktop development workload.'
    }
    $msbuild = $candidates[0]
    $solution = Join-Path $repoRoot ('ConsoleApp1.' + $SolutionFormat)
    & $msbuild $solution -nologo -verbosity:minimal -nodeReuse:false -target:Build "-property:Configuration=$Configuration" '-property:Platform=x64'
    $buildExitCode = $LASTEXITCODE
}
catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    $buildExitCode = 1
}
finally {
    $env:PATHEXT = $originalPathExt
}
exit $buildExitCode
