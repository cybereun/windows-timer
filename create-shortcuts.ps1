$ErrorActionPreference = 'Stop'
$appPath = Join-Path $PSScriptRoot 'WindowsTimer.exe'
if (-not (Test-Path -LiteralPath $appPath)) { throw "앱을 찾을 수 없습니다: $appPath" }

$desktop = [Environment]::GetFolderPath('Desktop')
$programs = [Environment]::GetFolderPath('Programs')
$desktopLink = Join-Path $desktop '윈도우 타이머.lnk'
$startLink = Join-Path $programs '윈도우 타이머.lnk'
$wsh = New-Object -ComObject WScript.Shell
foreach ($linkPath in @($desktopLink, $startLink)) {
    $link = $wsh.CreateShortcut($linkPath)
    $link.TargetPath = $appPath
    $link.WorkingDirectory = $PSScriptRoot
    $link.Description = '원하는 시간에 Windows를 종료하거나 재부팅합니다.'
    $link.IconLocation = "$appPath,0"
    $link.Save()
}

# Windows shell controls taskbar pinning. Request its native pin verb when available.
$shell = New-Object -ComObject Shell.Application
$item = $shell.Namespace($programs).ParseName('윈도우 타이머.lnk')
$pinVerb = @($item.Verbs()) | Where-Object { ($_.Name -replace '&','') -match '작업 표시줄에 고정|Pin to taskbar' } | Select-Object -First 1
if ($pinVerb) {
    $pinVerb.DoIt()
    Write-Output '작업 표시줄 고정 명령을 Windows에 전달했습니다.'
} else {
    Write-Output '바탕화면 및 시작 메뉴 바로가기를 만들었습니다. 이 Windows 버전은 자동 작업표시줄 고정을 허용하지 않아 수동으로 고정해야 합니다.'
}
Write-Output "바탕화면: $desktopLink"
Write-Output "시작 메뉴: $startLink"
