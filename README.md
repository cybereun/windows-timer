<div align="center">

# 윈도우 타이머 · WindowsTimer

[![Windows](https://img.shields.io/badge/Windows-10%2F11-557A95?style=for-the-badge&logo=windows)](#)
[![Version](https://img.shields.io/badge/version-1.1.1-B25B3A?style=for-the-badge)](https://github.com/cybereun/windows-timer/releases)
[![Developer: Lebi](https://img.shields.io/badge/Developer-Lebi-4C3B32?style=for-the-badge&logo=github&logoColor=white)](https://github.com/cybereun)

[한국어](#한국어) · [English](#english) · [日本語](#日本語) · [简体中文](#简体中文)

[릴리스 기록 · Release history · 更新履歴 · 更新记录](CHANGELOG.md)

<img src="docs/screenshot.jpg" alt="윈도우 타이머 앱 실행 화면" width="920">

</div>

## 한국어

윈도우 타이머는 Windows 종료, 재부팅, 절전, 최대 절전을 예약하는 가벼운 Windows 데스크톱 앱입니다. 기존 메인 예약 화면은 유지하고, **고급** 메뉴의 일정·보호·시스템·프로필·기록·원격 설정을 네 개 탭으로 나눴습니다. 한 번에 한 종류의 설정만 표시합니다. Google Fonts의 Noto Sans KR 글꼴과 앱 아이콘을 실행 파일에 포함해 별도 설치 없이 실행할 수 있습니다.

### 기능

- 분 단위 또는 날짜·시간으로 예약하고 실행 60초 전에 알림을 표시합니다. 예약은 언제든 취소하거나 10분 연장할 수 있습니다.
- 매일 또는 선택한 요일마다 반복 예약을 실행합니다.
- 메인 화면의 빠른 시간 버튼 4개를 원하는 분 값으로 바꿉니다.
- 배터리 잔량, AC 전원 연결, 키보드·마우스 유휴 조건을 설정합니다. 조건이 맞지 않으면 5분씩 보류하고 정한 제한 시간 후 예약을 취소합니다.
- 예약 기록과 종료 방식·시간 프로필을 저장합니다.
- Windows 로그인 시 자동 실행, 절전 타이머로 PC 깨우기, 시스템 트레이 남은 시간 표시를 설정합니다.
- 한국어, 영어, 일본어, 중국어 간체 UI를 제공합니다.
- 휴대폰이나 다른 PC의 브라우저에서 현재 PC의 예약을 만들고 취소합니다.
- 새 버전은 비공개 GitHub Releases 페이지에서 확인합니다. 자동 업데이트는 하지 않습니다.

### 다운로드와 실행

1. [Releases](https://github.com/cybereun/windows-timer/releases)에서 `WindowsTimer-v1.1.1.exe` 또는 배포 ZIP을 받습니다. 저장소가 비공개이므로 접근 권한이 있는 GitHub 계정으로 로그인해야 합니다.
2. 실행 파일을 열고 메인 화면에서 종료·재부팅과 시간을 선택한 뒤 **예약 시작**을 누릅니다.
3. 예약 중에는 앱을 실행해 두세요. 창을 닫으면 시스템 트레이로 숨겨지고, 트레이의 **앱 종료**는 예약도 취소합니다. 저장하지 않은 파일은 미리 저장하세요.

절전·최대 절전은 **고급 → 한 번 예약 · 다른 동작**에서 설정합니다. 반복 일정과 보호 조건도 고급 화면에서 따로 저장할 수 있습니다. 설정과 예약 기록은 현재 Windows 사용자 프로필 아래 `%LocalAppData%\WindowsTimer`에 보관됩니다.

### 휴대폰·다른 PC에서 원격 예약

1. PC 앱의 **고급 → 휴대폰 · 다른 PC에서 원격 예약**에서 원격 접속을 켭니다.
2. 같은 사설망의 휴대폰에서 표시된 `http://...` 주소를 열고, PC에 표시된 6자리 코드를 입력합니다. 연결 코드는 5분 동안 한 번만 사용할 수 있습니다.
3. 인터넷을 통해 접속하려면 휴대폰과 PC를 같은 VPN에 연결한 뒤 PC에 표시된 VPN 주소를 사용합니다. 앱은 공용 인터넷에 포트를 열거나 라우터 포트 전달을 설정하지 않습니다.

LAN 연결은 HTTP라 네트워크에서 암호화되지 않습니다. 신뢰할 수 있는 사설 Wi-Fi에서만 사용하세요. 외부 접속은 양쪽 기기를 VPN에 연결하세요. 연결 토큰은 PC에 저장할 때 Windows 사용자 계정의 DPAPI로 암호화됩니다. 예약은 2분에서 7일 사이로 만들며, 실행 전 PC에 60초 취소 알림이 표시됩니다.

### 빌드 및 안전 검사

Windows와 .NET Framework 4.x 컴파일러가 있는 환경에서 저장소 폴더에서 실행합니다.

```powershell
.\build.ps1 -OutputPath .\dist\WindowsTimer.exe
.\dist\WindowsTimer.exe --demo       # UI 데모, 실제 종료·재부팅 명령은 실행하지 않음
.\dist\WindowsTimer.exe --self-test  # 예약·반복·보호·저장 검사, 전원 명령 미실행
```

일반 실행은 설정한 동작을 Windows에 실제 요청합니다. 테스트에는 `--demo` 또는 `--self-test`를 사용하세요. PC 깨우기는 하드웨어·BIOS와 Windows의 절전 타이머 지원이 필요합니다.

### 글꼴 및 제작자

Noto Sans KR은 [Google Fonts](https://fonts.google.com/specimen/Noto+Sans+KR)에서 제공하며 SIL Open Font License를 `fonts/OFL.txt`에 포함했습니다. 제작자: **Lebi** · [@cybereun](https://github.com/cybereun).

## English

WindowsTimer is a lightweight Windows desktop app for scheduling shutdown, restart, sleep, and hibernate. The main scheduling screen stays familiar. The **Advanced** page groups settings into four tabs, showing one category at a time. Noto Sans KR and the app icon are bundled in the executable, so no separate installation is required.

### Features

- Schedule by minutes or a date and time, with a 60-second warning, cancellation, and a ten-minute extension.
- Repeat a schedule daily or on selected weekdays.
- Set the four quick-time buttons to custom minute values.
- Add battery, AC-power, and idle-time safeguards. When a condition is not met, the app waits in five-minute increments and cancels after the configured limit.
- Keep schedule history and save reusable action/time profiles.
- Start with Windows, wake the PC for a scheduled time, and show the remaining time in the system tray.
- Use the interface in Korean, English, Japanese, or Simplified Chinese.
- Create or cancel a schedule from a phone or another PC browser.
- Check for new versions on the private GitHub Releases page; the app does not auto-update.

### Download and run

1. Download `WindowsTimer-v1.1.1.exe` or the release ZIP from [Releases](https://github.com/cybereun/windows-timer/releases). The repository is private, so sign in with an account that has access.
2. Run the executable, choose shutdown or restart and a time on the main screen, then select **예약 시작** (Start schedule).
3. Keep the app running while a schedule is active. Closing the window hides it in the system tray. **앱 종료** (Exit app) in the tray cancels active schedules too. Save open files first.

Sleep and hibernate are available in **Advanced → One-time schedule · other actions**. Configure recurring schedules and safeguards separately in Advanced. Settings and history are stored for the current Windows user under `%LocalAppData%\WindowsTimer`.

### Remote scheduling from a phone or another PC

1. In **Advanced → Remote scheduling from phone or PC**, enable remote access.
2. On the same private network, open the displayed `http://...` address on the phone and enter the six-digit code shown on the PC. The code works once and expires after five minutes.
3. For access over the internet, connect the phone and PC to the same VPN and use the PC's VPN address. The app does not expose a public internet port or configure router port forwarding.

LAN traffic uses unencrypted HTTP, so use a trusted private Wi-Fi network only. Connect both devices through a VPN for internet access. The PC encrypts saved pairing tokens with the current Windows user's DPAPI. Remote schedules are limited to two minutes through seven days and still show a 60-second warning on the PC.

### Build and safe checks

Build on Windows with the .NET Framework 4.x compiler:

```powershell
.\build.ps1 -OutputPath .\dist\WindowsTimer.exe
.\dist\WindowsTimer.exe --demo       # UI demo; issues no shutdown or restart command
.\dist\WindowsTimer.exe --self-test  # schedule, guard, recurrence, and storage checks
```

Normal mode sends the selected power request to Windows when the schedule expires. Use `--demo` or `--self-test` for validation. Waking a sleeping PC depends on hardware, BIOS, and Windows wake-timer support.

### Font and developer

Noto Sans KR is from [Google Fonts](https://fonts.google.com/specimen/Noto+Sans+KR), with the SIL Open Font License in `fonts/OFL.txt`. Developer: **Lebi** · [@cybereun](https://github.com/cybereun).

## 日本語

WindowsTimer は、Windows のシャットダウン、再起動、スリープ、休止状態を予約する軽量デスクトップアプリです。メインの予約画面はそのままにし、左側の **詳細** メニューを4つのタブに分け、設定を種類ごとに表示します。Noto Sans KR とアプリアイコンを実行ファイルに同梱しています。

### 主な機能

- 分単位または日時で予約し、実行60秒前に通知。予約はいつでも取り消し、10分延長できます。
- 毎日または選択した曜日に繰り返し実行します。
- メイン画面の4つの時間ボタンを任意の分数に変更できます。
- バッテリー残量、AC電源、ユーザーのアイドル時間を条件に設定できます。条件を満たさない場合は5分ずつ保留し、設定した上限時間後に予約を取り消します。
- 予約履歴と、操作・時間を再利用するプロファイルを保存します。
- Windowsログイン時の自動起動、スリープ解除タイマー、システムトレイの残り時間表示に対応します。
- 韓国語、英語、日本語、簡体字中国語のUIを選べます。
- スマートフォンや別のPCのブラウザーから予約を作成・取り消せます。
- 非公開 GitHub Releases ページで新しいバージョンを確認できます。自動更新は行いません。

### ダウンロードと実行

1. [Releases](https://github.com/cybereun/windows-timer/releases) から `WindowsTimer-v1.1.1.exe` または ZIP をダウンロードします。非公開リポジトリのため、アクセス権のある GitHub アカウントが必要です。
2. 実行ファイルを開き、メイン画面でシャットダウンまたは再起動と時間を選び、**예약 시작**（予約開始）を押します。
3. 予約中はアプリを起動したままにしてください。ウィンドウを閉じるとシステムトレイに格納されます。トレイの **앱 종료**（アプリ終了）を選ぶと予約も取り消されます。開いているファイルは先に保存してください。

スリープと休止状態は **詳細 → 1回予約・その他の操作** で設定できます。繰り返し予約と保護条件も詳細画面で設定します。設定と履歴は現在のWindowsユーザーの `%LocalAppData%\WindowsTimer` に保存されます。

### スマートフォン・別PCからのリモート予約

1. **詳細 → スマホ・別のPCから遠隔予約** でリモート接続を有効にします。
2. 同じプライベートネットワーク上のスマートフォンで表示された `http://...` を開き、PC画面の6桁コードを入力します。コードは1回限りで5分後に失効します。
3. インターネット経由の場合は、スマートフォンとPCを同じVPNに接続し、PCに表示されたVPNアドレスを使います。アプリは公開インターネットにポートを開かず、ルーターのポート転送も設定しません。

LAN通信は暗号化されないHTTPです。信頼できるプライベートWi-Fiでのみ使ってください。外部接続では両方の端末をVPNに接続してください。保存した接続トークンはWindowsユーザーのDPAPIで暗号化します。予約範囲は2分から7日で、実行60秒前にはPCに通知が表示されます。

### ビルドと安全な確認

Windows と .NET Framework 4.x コンパイラーで実行します。

```powershell
.\build.ps1 -OutputPath .\dist\WindowsTimer.exe
.\dist\WindowsTimer.exe --demo       # UIデモ。終了・再起動命令は実行しません
.\dist\WindowsTimer.exe --self-test  # 予約・保護・繰り返し・保存を検査
```

通常モードでは予約時刻にWindowsへ電源操作を要求します。検証には `--demo` または `--self-test` を使ってください。PCの起動はハードウェア、BIOS、Windowsのスリープ解除タイマー対応に依存します。

### フォントと開発者

Noto Sans KR は [Google Fonts](https://fonts.google.com/specimen/Noto+Sans+KR) のフォントです。SIL Open Font License は `fonts/OFL.txt` に収録しています。開発者: **Lebi** · [@cybereun](https://github.com/cybereun)。

## 简体中文

WindowsTimer 是一款用于预约 Windows 关机、重启、睡眠和休眠的轻量桌面应用。主预约界面保持原样，左侧 **高级** 页面按用途分为四个标签，每次只显示一类设置。Noto Sans KR 字体和应用图标已包含在可执行文件中，无需单独安装。

### 主要功能

- 按分钟或日期时间预约，并在执行前60秒提醒；可随时取消或延长10分钟。
- 每天或在选定星期重复执行。
- 自定义主界面的四个快捷时长按钮。
- 设置电量、交流电源和空闲时间保护条件。条件不满足时每隔5分钟等待一次，超过设定上限后取消预约。
- 保存预约历史以及可复用的操作和时间配置。
- 设置 Windows 登录时启动、预约时唤醒电脑，以及在系统托盘显示剩余时间。
- 支持韩语、英语、日语和简体中文界面。
- 从手机或其他电脑的浏览器创建或取消预约。
- 在私有 GitHub Releases 页面查看新版本；应用不会自动更新。

### 下载和运行

1. 从 [Releases](https://github.com/cybereun/windows-timer/releases) 下载 `WindowsTimer-v1.1.1.exe` 或发布 ZIP。仓库为私有，需要使用有访问权限的 GitHub 账号登录。
2. 运行程序，在主界面选择关机或重启和时间，然后点击 **예약 시작**（开始预约）。
3. 预约期间请保持应用运行。关闭窗口会将其隐藏到系统托盘；从托盘选择 **앱 종료**（退出应用）也会取消预约。请先保存文件。

睡眠和休眠设置位于 **高级 → 单次预约 · 其他操作**。重复计划和保护条件也在高级页面中单独设置。设置和记录保存在当前 Windows 用户的 `%LocalAppData%\WindowsTimer` 下。

### 使用手机或其他电脑远程预约

1. 在 **高级 → 通过手机或其他电脑远程预约** 中启用远程访问。
2. 手机连接同一专用网络后，打开应用显示的 `http://...` 地址，并输入电脑上显示的6位代码。代码只能使用一次，5分钟后失效。
3. 通过互联网访问时，让手机和电脑连接同一 VPN，再使用电脑显示的 VPN 地址。应用不会向公共互联网开放端口，也不会设置路由器端口转发。

局域网通信使用未加密的 HTTP，请仅在可信的专用 Wi-Fi 网络中使用。通过互联网访问时，请让两台设备连接 VPN。连接令牌在电脑上使用当前 Windows 用户的 DPAPI 加密保存。远程预约时间为2分钟至7天，执行前电脑仍会显示60秒提醒。

### 构建和安全检查

在安装 .NET Framework 4.x 编译器的 Windows 环境中运行：

```powershell
.\build.ps1 -OutputPath .\dist\WindowsTimer.exe
.\dist\WindowsTimer.exe --demo       # 界面演示，不执行关机或重启命令
.\dist\WindowsTimer.exe --self-test  # 检查预约、保护、重复计划和存储
```

普通模式会在预约时间向 Windows 发送所选电源操作。验证时使用 `--demo` 或 `--self-test`。唤醒电脑取决于硬件、BIOS 和 Windows 的唤醒定时器支持。

### 字体和开发者

Noto Sans KR 来自 [Google Fonts](https://fonts.google.com/specimen/Noto+Sans+KR)，SIL Open Font License 位于 `fonts/OFL.txt`。开发者：**Lebi** · [@cybereun](https://github.com/cybereun)。