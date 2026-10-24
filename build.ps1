param([string]$OutputPath)
$ErrorActionPreference = 'Stop'
$appDir = $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if ([string]::IsNullOrWhiteSpace($OutputPath)) { $OutputPath = Join-Path $appDir 'WindowsTimer.exe' }
$OutputPath = [System.IO.Path]::GetFullPath($OutputPath)
if (-not (Test-Path -LiteralPath (Join-Path $appDir 'WindowsTimer.ico'))) { throw 'WindowsTimer.ico is missing' }
$resources = @(
    "/resource:$(Join-Path $appDir 'assets\hero-timer.png'),WindowsTimer.assets.hero-timer.png",
    "/resource:$(Join-Path $appDir 'fonts\NotoSansKR-VF.ttf'),WindowsTimer.fonts.NotoSansKR-VF.ttf",
    "/resource:$(Join-Path $appDir 'fonts\OFL.txt'),WindowsTimer.fonts.OFL.txt"
)
& $compiler /nologo /target:winexe /optimize+ /win32icon:"$appDir\WindowsTimer.ico" /out:"$OutputPath" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /reference:System.Security.dll $resources "$appDir\AssemblyInfo.cs" "$appDir\Program.cs" "$appDir\RemoteServer.cs" "$appDir\RemoteTunnel.cs" "$appDir\UpdateManager.cs"
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
