param(
    [string]$BuildOutput = "D:\Flow.Launcher\Output\Debug",
    [string]$FlowRoot = "$env:LOCALAPPDATA\FlowLauncher\app-2.1.3",
    [string]$PluginName = "Flow.Launcher.Plugin.Note",
    [switch]$Restart
)

$managedFiles = @(
    "Flow.Launcher.dll",
    "Flow.Launcher.pdb",
    "Flow.Launcher.Core.dll",
    "Flow.Launcher.Core.pdb",
    "Flow.Launcher.Infrastructure.dll",
    "Flow.Launcher.Infrastructure.pdb",
    "Flow.Launcher.Plugin.dll",
    "Flow.Launcher.Plugin.pdb"
)

$pluginSource = Join-Path $BuildOutput "Plugins\$PluginName"
$pluginTarget = Join-Path $FlowRoot "Plugins\$PluginName"
$flowExe = Join-Path $FlowRoot "Flow.Launcher.exe"

Write-Host "Build output: $BuildOutput"
Write-Host "Flow root: $FlowRoot"
Write-Host "Plugin source: $pluginSource"
Write-Host "Plugin target: $pluginTarget"

if (-not (Test-Path -LiteralPath $BuildOutput)) {
    Write-Error "Build output directory does not exist. Build the app first."
    exit 1
}

if (-not (Test-Path -LiteralPath $FlowRoot)) {
    Write-Error "Installed Flow Launcher directory does not exist."
    exit 1
}

if (-not (Test-Path -LiteralPath $flowExe)) {
    Write-Error "Flow.Launcher.exe was not found in the installed directory."
    exit 1
}

if (-not (Test-Path -LiteralPath $pluginSource)) {
    Write-Error "Plugin build output directory does not exist."
    exit 1
}

foreach ($file in $managedFiles) {
    $source = Join-Path $BuildOutput $file
    $target = Join-Path $FlowRoot $file

    if (-not (Test-Path -LiteralPath $source)) {
        Write-Error "Missing managed build output: $source"
        exit 1
    }

    Copy-Item -LiteralPath $source -Destination $target -Force
}

if (Test-Path -LiteralPath $pluginTarget) {
    Remove-Item -LiteralPath $pluginTarget -Recurse -Force
}

New-Item -ItemType Directory -Path $pluginTarget | Out-Null
Get-ChildItem -LiteralPath $pluginSource -Force | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $pluginTarget -Recurse -Force
}

$mainHash = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $FlowRoot "Flow.Launcher.dll")).Hash
$pluginHash = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $pluginTarget "$PluginName.dll")).Hash

Write-Host "Deployed Flow.Launcher.dll hash: $mainHash"
Write-Host "Deployed $PluginName.dll hash: $pluginHash"
Write-Host "Safe deploy completed. Startup files were not touched."

if ($Restart) {
    $process = Get-Process -Name Flow.Launcher -ErrorAction SilentlyContinue
    if ($process) {
        Stop-Process -Id $process.Id -Force
        Start-Sleep -Seconds 2
    }

    Start-Process -FilePath $flowExe -WindowStyle Hidden
    Write-Host "Flow Launcher restarted."
}
