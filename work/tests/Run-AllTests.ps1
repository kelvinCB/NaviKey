$ErrorActionPreference = 'Stop'
$tests = @(
    (Join-Path $PSScriptRoot 'UiDesignContract.ps1'),
    (Join-Path $PSScriptRoot 'ModeAudioTests.ps1'),
    (Join-Path $PSScriptRoot 'SystemContract.ps1')
)
foreach ($test in $tests) {
    Write-Output "RUN $([IO.Path]::GetFileName($test))"
    & $test
    if (-not $?) { throw "FAILED $test" }
}
Write-Output 'ALL TESTS PASSED'
