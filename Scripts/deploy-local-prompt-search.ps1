param(
    [string]$BuildOutput = "D:\Flow.Launcher\Output\Debug\Plugins\Flow.Launcher.Plugin.LocalPromptSearch",
    [string]$FlowPluginRoot = "$env:LOCALAPPDATA\FlowLauncher\app-2.1.3\Plugins",
    [switch]$Force
)

$targetDir = Join-Path $FlowPluginRoot "Flow.Launcher.Plugin.LocalPromptSearch"

Write-Host "Build output: $BuildOutput"
Write-Host "Deploy target: $targetDir"

if (-not (Test-Path -LiteralPath $BuildOutput)) {
    Write-Error "构建输出目录不存在，请先成功编译插件。"
    exit 1
}

if (-not (Test-Path -LiteralPath $FlowPluginRoot)) {
    Write-Error "Flow Launcher 插件根目录不存在：$FlowPluginRoot"
    exit 1
}

if ((Test-Path -LiteralPath $targetDir) -and -not $Force) {
    Write-Error "目标目录已存在。若要覆盖，请加 -Force。"
    exit 1
}

if (Test-Path -LiteralPath $targetDir) {
    Remove-Item -LiteralPath $targetDir -Recurse -Force
}

Copy-Item -LiteralPath $BuildOutput -Destination $targetDir -Recurse -Force

Write-Host "部署完成。请重启 Flow Launcher 后测试：pt / pt 周报 / pt reload"
