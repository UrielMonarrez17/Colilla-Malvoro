param(
    [string]$UnityExecutable = 'C:\Program Files\Unity\Hub\Editor\6000.5.9f1\Editor\Unity.exe',
    [switch]$Build
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$logs = Join-Path $projectRoot 'Logs'
New-Item -ItemType Directory -Path $logs -Force | Out-Null

function Run-ValidationProcess([string]$executable, [string]$arguments) {
    $validationProcess = Start-Process -FilePath $executable -ArgumentList $arguments -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru
    $validationProcess.WaitForExit()
    if ($validationProcess.ExitCode -ne 0) {
        throw "Validation exited with code $($validationProcess.ExitCode). See $logs"
    }
}

# Unity needs the project closed in other editor instances. No scene is saved by these tests.
$editorArgs = '-batchmode -projectPath "{0}" -executeMethod Prototipo3.Editor.MaterialValidation.Run -materialReport="{1}" -logFile "{2}"' -f $projectRoot, (Join-Path $logs 'material-editor.txt'), (Join-Path $logs 'material-editor-unity.log')
Run-ValidationProcess $UnityExecutable $editorArgs
Get-Content -LiteralPath (Join-Path $logs 'material-editor.txt') | Select-Object -Last 1

if ($Build) {
    $buildArgs = '-batchmode -projectPath "{0}" -executeMethod Prototipo3.Editor.MaterialValidation.Build -logFile "{1}"' -f $projectRoot, (Join-Path $logs 'material-build-unity.log')
    Run-ValidationProcess $UnityExecutable $buildArgs
    $playerArgs = '-batchmode -screen-width 1280 -screen-height 720 -materialReport="{0}" -logFile "{1}"' -f (Join-Path $logs 'material-player.txt'), (Join-Path $logs 'material-player-unity.log')
    Run-ValidationProcess (Join-Path $logs 'MaterialValidationBuild\FeralMaterials.exe') $playerArgs
    Get-Content -LiteralPath (Join-Path $logs 'material-player.txt') | Select-Object -Last 1
}
