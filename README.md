<div align="center">

# 윈도우 타이머 · WindowsTimer

[![Windows](https://img.shields.io/badge/Windows-10%2F11-557A95?style=for-the-badge&logo=windows)](#)
[![Version](https://img.shields.io/badge/version-1.0.1-B25B3A?style=for-the-badge)](https://github.com/cybereun/windows-timer/releases)
[![Developer: Lebi](https://img.shields.io/badge/Developer-Lebi-4C3B32?style=for-the-badge&logo=github&logoColor=white)](https://github.com/cybereun)

[한국어](#한국어) · [English](#english) · [日本語](#日本語) · [简体中文](#简体中文)

<img src="docs/screenshot.jpg" alt="윈도우 타이머 앱 화면" width="920">

</div>

## 한국어

원하는 시간에 Windows 종료 또는 재부팅을 예약하는 가벼운 데스크톱 앱입니다. 따뜻한 3D 스타일 UI와 Google Fonts의 Noto Sans KR을 사용하며, 글꼴과 화면 이미지를 실행 파일에 포함해 별도 다운로드 없이 실행할 수 있습니다.

### 주요 기능

- 분 단위 타이머와 날짜·시간 지정 예약(최대 7일)
- Windows 종료 또는 재부팅 예약
- 실행 60초 전 알림, 예약 취소, 10분 연장
- 절전 후 복귀하면 안전하게 60초 대기
- 창을 닫아도 시스템 트레이에서 예약 유지
- 한글 UI, 빠른 시간 선택, 바탕화면·시작 메뉴 바로가기와 작업 표시줄 고정 지원

### 다운로드 및 실행

1. [Releases](https://github.com/cybereun/windows-timer/releases)에서 `WindowsTimer.exe`를 받습니다. 비공개 저장소이므로 저장소에 접근 권한이 있는 GitHub 계정으로 로그인해야 합니다.
2. 받은 파일을 실행하고 종료 또는 재부팅, 시간을 선택합니다.
3. **예약 시작**을 누릅니다. 실행 전에 알림이 표시되며, 예약 카드에서 언제든 취소하거나 10분 연장할 수 있습니다.

예약은 앱이 실행되는 동안 유지됩니다. 창의 닫기 버튼은 앱을 트레이로 숨깁니다. 트레이 메뉴에서 **앱 종료**를 선택하면 진행 중인 예약도 취소됩니다. 저장하지 않은 파일은 미리 저장하세요.

바탕화면과 시작 메뉴 아이콘은 배포 ZIP에 포함된 `create-shortcuts.ps1`로 만들 수 있습니다. 작업 표시줄에는 시작 메뉴의 **윈도우 타이머** 바로가기를 마우스 오른쪽 버튼으로 눌러 **추가 옵션 표시 → 작업 표시줄에 고정**을 선택합니다.

### 개발 및 안전 확인

Windows와 .NET Framework 4.x 컴파일러가 있는 환경에서 PowerShell을 열어 저장소 폴더에서 실행합니다.

```powershell
.\build.ps1
.\WindowsTimer.exe --demo       # 종료 명령을 실행하지 않는 화면 데모
.\WindowsTimer.exe --self-test  # 예약 로직 검사, 종료 명령 미실행
```

일반 실행은 실제 종료·재부팅 예약을 수행합니다. 테스트는 `--demo` 또는 `--self-test`로 실행하세요.

### 글꼴 및 제작자

포함된 Noto Sans KR은 [Google Fonts](https://fonts.google.com/specimen/Noto+Sans+KR)의 글꼴이며, SIL Open Font License는 [`fonts/OFL.txt`](fonts/OFL.txt)에 있습니다. 제작자: **Lebi** · [@cybereun](https://github.com/cybereun).

### 릴리스 기록

전체 4개 언어의 변경 내역은 [`CHANGELOG.md`](CHANGELOG.md)를 확인하세요.

## English

WindowsTimer is a lightweight Windows desktop app for scheduling shutdowns and restarts. It features a warm, sculptural 3D-inspired interface and embeds Noto Sans KR from Google Fonts so the app can run without downloading its font separately.

### Features

- Schedule by minutes or choose a date and time up to seven days ahead
- Choose Windows shutdown or restart
- Get a 60-second warning, cancel a schedule, or extend it by ten minutes
- Wait an extra 60 seconds after waking from sleep
- Keep a schedule while the app is minimized to the system tray
- Korean interface, quick presets, desktop/Start menu shortcuts, and Windows taskbar pin support

### Download and run

1. Download `WindowsTimer.exe` from [Releases](https://github.com/cybereun/windows-timer/releases). This private repository requires an authorized GitHub account.
2. Run the file, choose shutdown or restart, and set a time.
3. Select **예약 시작** (Start schedule). You can cancel or extend the active schedule at any time before it runs.

The app must remain running for the schedule to stay active. Closing its window hides it in the system tray. Choosing **앱 종료** (Exit app) from the tray also cancels an active schedule. Save open work before scheduling.

The release ZIP includes `create-shortcuts.ps1` to create desktop and Start menu shortcuts. To pin the app, right-click the **WindowsTimer** Start menu shortcut and choose **Show more options → Pin to taskbar**.

### Build and safe checks

Build on Windows with the .NET Framework 4.x compiler:

```powershell
.\build.ps1
.\WindowsTimer.exe --demo       # UI demo; never issues a shutdown command
.\WindowsTimer.exe --self-test  # schedule logic checks; no shutdown command
```

Normal mode can shut down or restart Windows when its timer expires. Use the demo or self-test for validation.

### Font and developer

Noto Sans KR is from [Google Fonts](https://fonts.google.com/specimen/Noto+Sans+KR). Its SIL Open Font License is included at [`fonts/OFL.txt`](fonts/OFL.txt). Developer: **Lebi** · [@cybereun](https://github.com/cybereun).

See [`CHANGELOG.md`](CHANGELOG.md) for release history in Korean, English, Japanese, and Simplified Chinese.

## 日本語

WindowsTimer は、Windows のシャットダウンまたは再起動を予約する軽量なデスクトップアプリです。あたたかみのある 3D イメージの UI を採用し、Google Fonts の Noto Sans KR をアプリに同梱しています。

### 主な機能

- 分単位のタイマー、または最大 7 日先までの日付と時刻を指定
- シャットダウンと再起動を選択
- 実行 60 秒前の通知、予約のキャンセル、10 分延長
- スリープから復帰した後は安全のため 60 秒待機
- ウィンドウを閉じても、システムトレイで予約を維持
- 韓国語 UI、時間プリセット、デスクトップ・スタートメニュー・タスクバーのショートカット

### ダウンロードと実行

1. [Releases](https://github.com/cybereun/windows-timer/releases) から `WindowsTimer.exe` をダウンロードします。この非公開リポジトリへのアクセス権が必要です。
2. ファイルを起動し、シャットダウンまたは再起動と時間を選択します。
3. **예약 시작**（予約開始）を押します。実行前に通知が表示され、予約はいつでもキャンセルまたは 10 分延長できます。

予約中はアプリを起動したままにしてください。ウィンドウを閉じるとシステムトレイに格納されます。トレイの **앱 종료**（アプリ終了）を選ぶと、予約もキャンセルされます。作業中のファイルは事前に保存してください。

リリース ZIP に含まれる `create-shortcuts.ps1` でデスクトップとスタートメニューのショートカットを作成できます。タスクバーに固定するには、スタートメニューの **WindowsTimer** を右クリックし、**その他のオプションを表示 → タスクバーにピン留め**を選択します。

### ビルドと安全なテスト

Windows と .NET Framework 4.x コンパイラーの環境で実行します。

```powershell
.\build.ps1
.\WindowsTimer.exe --demo       # シャットダウン命令を実行しない UI デモ
.\WindowsTimer.exe --self-test  # 予約ロジック検査。シャットダウン命令なし
```

通常モードは予約時刻に実際の終了・再起動を行います。検証にはデモまたはセルフテストを使用してください。

### フォントと開発者

Noto Sans KR は [Google Fonts](https://fonts.google.com/specimen/Noto+Sans+KR) のフォントです。SIL Open Font License は [`fonts/OFL.txt`](fonts/OFL.txt) に収録しています。開発者: **Lebi** · [@cybereun](https://github.com/cybereun)。

韓国語・英語・日本語・簡体字中国語の更新履歴は [`CHANGELOG.md`](CHANGELOG.md) をご覧ください。

## 简体中文

WindowsTimer 是一款用于预约 Windows 关机或重启的轻量桌面应用。界面采用温暖的 3D 风格，并内置来自 Google Fonts 的 Noto Sans KR 字体，无需单独下载字体。

### 主要功能

- 按分钟预约，或选择最多 7 天内的日期和时间
- 选择关机或重启
- 执行前 60 秒提醒、取消预约、延长 10 分钟
- 从睡眠恢复后额外等待 60 秒
- 窗口关闭后可在系统托盘中继续保持预约
- 韩语界面、快捷时长、桌面/开始菜单快捷方式和任务栏固定支持

### 下载和运行

1. 从 [Releases](https://github.com/cybereun/windows-timer/releases) 下载 `WindowsTimer.exe`。这是私有仓库，需要使用已获授权的 GitHub 账号访问。
2. 运行程序，选择关机或重启并设置时间。
3. 点击 **예약 시작**（开始预约）。执行前会提醒；预约执行前可以随时取消或延长 10 分钟。

预约期间应用必须保持运行。关闭窗口会将应用隐藏到系统托盘。从托盘菜单选择 **앱 종료**（退出应用）也会取消当前预约。请提前保存未保存的文件。

发布 ZIP 中包含 `create-shortcuts.ps1`，可创建桌面和开始菜单快捷方式。若要固定到任务栏，请右键开始菜单中的 **WindowsTimer** 快捷方式，选择 **显示更多选项 → 固定到任务栏**。

### 构建和安全测试

在安装 .NET Framework 4.x 编译器的 Windows 环境中运行：

```powershell
.\build.ps1
.\WindowsTimer.exe --demo       # 仅演示界面，不会执行关机命令
.\WindowsTimer.exe --self-test  # 检查预约逻辑，不会执行关机命令
```

普通模式会在计时结束时实际关机或重启。验证时请使用演示模式或自测模式。

### 字体和开发者

Noto Sans KR 来自 [Google Fonts](https://fonts.google.com/specimen/Noto+Sans+KR)，SIL Open Font License 位于 [`fonts/OFL.txt`](fonts/OFL.txt)。开发者：**Lebi** · [@cybereun](https://github.com/cybereun)。

韩语、英语、日语和简体中文的更新记录请参阅 [`CHANGELOG.md`](CHANGELOG.md)。
