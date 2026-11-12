param(
    [string]$UnityExecutable = 'C:\Program Files\Unity\Hub\Editor\6000.5.9f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$validationRoot = Join-Path $projectRoot 'Logs\TrapValidationProject'
New-Item -ItemType Directory -Path $validationRoot -Force | Out-Null

function Copy-ValidationFile([string]$relativePath) {
    $source = Join-Path $projectRoot $relativePath
    $destination = Join-Path $validationRoot $relativePath
    New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
    Copy-Item -LiteralPath $source -Destination $destination -Force
    if (Test-Path -LiteralPath "$source.meta") {
        Copy-Item -LiteralPath "$source.meta" -Destination "$destination.meta" -Force
    }
}

# Copiar código de producción; sólo incluir el validador entre las herramientas del editor.
Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets') -Recurse -Filter '*.cs' -File |
    Where-Object { $_.FullName -notmatch '\\Editor\\' -or $_.Name -eq 'TrapEffectValidation.cs' } |
    ForEach-Object { Copy-ValidationFile $_.FullName.Substring($projectRoot.Length + 1) }

Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets\Animals\Animals_FREE') -Recurse -File |
    Where-Object { $_.Extension -ne '.meta' -and $_.Extension -ne '.unity' } |
    ForEach-Object { Copy-ValidationFile $_.FullName.Substring($projectRoot.Length + 1) }
Copy-ValidationFile 'Assets\Prototipo3\Prefabs\Tiger_Prototipo3.prefab'
New-Item -ItemType Directory -Path (Join-Path $validationRoot 'ProjectSettings') -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $projectRoot 'ProjectSettings') -File |
    ForEach-Object { Copy-ValidationFile ('ProjectSettings\' + $_.Name) }

# Reutilizar paquetes locales. La copia de pruebas no necesita la conexión MCP del editor abierto.
$manifest = Get-Content -LiteralPath (Join-Path $projectRoot 'Packages\manifest.json') -Raw | ConvertFrom-Json
$manifest.dependencies.PSObject.Properties.Remove('com.coplaydev.unity-mcp')
Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Library\PackageCache') -Directory | ForEach-Object {
    $packageJson = Join-Path $_.FullName 'package.json'
    if (Test-Path -LiteralPath $packageJson) {
        $package = Get-Content -LiteralPath $packageJson -Raw | ConvertFrom-Json
        if ($package.name -like 'com.unity.*') {
            $manifest.dependencies | Add-Member -NotePropertyName $package.name -NotePropertyValue ('file:' + $_.FullName.Replace('\', '/')) -Force
        }
    }
}
New-Item -ItemType Directory -Path (Join-Path $validationRoot 'Packages') -Force | Out-Null
$manifest | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $validationRoot 'Packages\manifest.json') -Encoding UTF8
$logPath = Join-Path $projectRoot 'Logs\prototipo3-trap-validation-unity.log'
$arguments = '-batchmode -nographics -projectPath "{0}" -executeMethod Prototipo3.Editor.TrapEffectValidation.Run -logFile "{1}"' -f $validationRoot, $logPath
$process = Start-Process -FilePath $UnityExecutable -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "Unity validation PID: $($process.Id)"
$process.WaitForExit()
$report = Join-Path $validationRoot 'Logs\prototipo3-trap-validation.txt'
if (Test-Path -LiteralPath $report) {
    Copy-Item -LiteralPath $report -Destination (Join-Path $projectRoot 'Logs\prototipo3-trap-validation.txt') -Force
    Get-Content -LiteralPath $report
}
Write-Output "Unity exit: $($process.ExitCode)"
exit $process.ExitCode
