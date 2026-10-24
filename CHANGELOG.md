# WindowsTimer Release History

## v1.1.3 — 2026-10-24

### 한국어
- .NET Framework에서 GitHub TLS 연결이 실패해 자동 업데이트가 멈추던 문제를 수정했습니다. GitHub CLI를 통해 릴리스 정보를 확인하고 실행 파일을 내려받습니다.
- 파일 크기, GitHub 릴리스 SHA-256, 실행 파일 제품 버전을 확인한 뒤에만 교체·재실행합니다.
- v1.1.2 사용자는 v1.1.3을 한 번 직접 설치해야 합니다. 이후 버전부터 업데이트가 자동 적용됩니다.

### English
- Fixed automatic updates failing on .NET Framework TLS connections. Release metadata and the executable are now fetched through GitHub CLI.
- Replacement and restart happen only after checking the asset size, GitHub release SHA-256, and executable product version.
- Users on v1.1.2 must install v1.1.3 manually once; later updates apply automatically.

### 日本語
- .NET FrameworkのGitHub TLS接続が失敗して自動更新できない問題を修正しました。GitHub CLIでリリース情報と実行ファイルを取得します。
- ファイルサイズ、GitHubリリースのSHA-256、実行ファイルの製品バージョンを確認してから置き換え、再起動します。
- v1.1.2のユーザーはv1.1.3を一度手動でインストールしてください。以降の更新は自動で適用されます。

### 简体中文
- 修复了 .NET Framework 的 GitHub TLS 连接失败导致自动更新无法运行的问题。现在通过 GitHub CLI 获取发布信息和程序文件。
- 只有在检查文件大小、GitHub 发布 SHA-256 和程序产品版本后，才会替换并重新启动。
- v1.1.2 用户需要手动安装一次 v1.1.3；之后的更新会自动应用。

## v1.1.2 — 2026-10-24

### 한국어
- 앱 시작 후와 6시간마다 GitHub Releases의 버전을 확인하고, 새 실행 파일을 자동으로 다운로드해 SHA-256과 제품 버전을 검증한 뒤 교체·재실행합니다.
- 활성 일회 예약이나 2분 이내의 반복 예약이 있으면 파일은 미리 받고 예약이 안전해진 뒤 적용합니다.
- 비공개 저장소 업데이트에 GitHub CLI 로그인을 사용하며, 업데이트 상태와 오류를 한국어·영어·일본어·중국어 간체로 표시합니다.
- 버전 비교, 릴리스 파일 이름과 업데이트 상태 번역 검사를 추가했습니다. 안전 검사에서 실제 전원 명령은 실행하지 않습니다.

### English
- Checks GitHub Releases after launch and every six hours, then downloads, verifies the SHA-256 digest and product version, replaces the executable, and restarts the app.
- Downloads can finish during a schedule; installation waits until a one-time schedule is inactive and no recurring action is due within two minutes.
- Uses GitHub CLI authentication for the private repository and localizes update status and errors in Korean, English, Japanese, and Simplified Chinese.
- Added safe checks for version comparison, release asset naming, and update status translations. Tests issue no Windows power commands.

### 日本語
- 起動後と6時間ごとにGitHub Releasesを確認し、新しい実行ファイルを自動でダウンロードします。SHA-256と製品バージョンを検証してから置き換え、アプリを再起動します。
- 1回予約が有効な間、または2分以内に繰り返し実行がある場合は、ダウンロード後に安全な時点まで適用を待ちます。
- 非公開リポジトリへのアクセスにGitHub CLI認証を使用し、更新状態とエラーを韓国語・英語・日本語・簡体字中国語で表示します。
- バージョン比較、リリースファイル名、更新状態の翻訳を検査する安全なテストを追加しました。テストではWindowsの電源命令を実行しません。

### 简体中文
- 启动后及每六小时检查 GitHub Releases；发现新版本后自动下载，并验证 SHA-256 和产品版本，再替换程序并重新启动。
- 可在预约期间完成下载；单次预约仍有效或重复操作将在两分钟内执行时，会等到安全时机再安装。
- 使用 GitHub CLI 认证访问私有仓库，并以韩语、英语、日语和简体中文显示更新状态与错误。
- 新增版本比较、发布文件名和更新状态翻译的安全检查。测试不会执行 Windows 电源命令。

## v1.1.1 — 2026-10-24

### 한국어
- 고급 설정을 **일정**, **보호 · 시스템**, **프로필 · 기록**, **원격 · 언어** 네 개 탭으로 나눠 한 번에 한 종류만 표시합니다.
- 영문 등 번역된 버튼 문구가 두 줄로 갈라지지 않도록 한 줄 표시와 버튼 너비에 맞춘 글자 크기를 적용했습니다.
- 같은 탭의 설정 카드 높이를 가장 큰 카드에 맞춰 아래쪽 선이 가지런히 정렬되도록 했습니다.

### English
- Organized Advanced settings into four tabs: **Schedule**, **Safety · system**, **Profiles · history**, and **Remote · language**. Each tab shows one category at a time.
- Kept translated button labels on one line and fit their text to the available button width.
- Matched card heights within each tab so their bottom edges line up.

### 日本語
- 詳細設定を **スケジュール**、**保護・システム**、**プロファイル・履歴**、**リモート・言語** の4つのタブに整理し、一度に1種類の設定を表示します。
- 翻訳後のボタンラベルが折り返されないようにし、ボタン幅に合わせて文字サイズを調整します。
- 同じタブ内のカードの高さをそろえ、下端がきれいに並ぶようにしました。

### 简体中文
- 将高级设置整理为 **日程**、**保护 · 系统**、**配置 · 记录**、**远程 · 语言** 四个标签，每次只显示一类设置。
- 翻译后的按钮文字保持单行，并根据按钮宽度调整字号。
- 统一每个标签页内卡片的高度，使卡片底边对齐。
## v1.1.0 — 2026-10-24

### 한국어
- 기존 메인 예약 화면은 유지하고 왼쪽 메뉴에 **고급** 설정 페이지를 추가했습니다.
- 매일·요일별 반복 예약, 사용자 지정 빠른 시간 4개, 예약 기록, 저장·적용 가능한 프로필을 추가했습니다.
- 배터리 잔량, AC 전원, 사용자 유휴 시간을 보호 조건으로 설정할 수 있으며, 조건이 맞지 않으면 보류하고 제한 시간이 지나면 취소합니다.
- Windows 로그인 자동 실행, 절전 예약 시 PC 깨우기, 트레이 남은 시간 표시를 추가했습니다.
- 종료·재부팅 외에 절전·최대 절전 예약을 지원합니다.
- 한국어·영어·일본어·중국어 간체 UI와 휴대폰/다른 PC 브라우저용 원격 예약 화면을 추가했습니다.
- 원격 연결은 같은 사설망 또는 두 기기가 연결된 VPN에서만 사용할 수 있습니다. LAN HTTP는 암호화되지 않음을 앱과 문서에 안내하고, 저장된 연결 토큰은 Windows DPAPI로 암호화합니다.
- 새 버전 확인을 위해 비공개 GitHub Releases 페이지를 여는 기능을 추가했습니다.
- 보호 조건 대기 시간이 5분 연기 후에도 계속 누적되도록 고치고, 프로필에 날짜·시간도 저장하도록 수정했습니다.
- 안전 검사에 반복·보호 조건·다국어·토큰 저장 검증을 포함했습니다. 검사에서는 실제 전원 명령을 실행하지 않습니다.

### English
- Kept the existing main scheduling screen and added a separate **Advanced** page in the left navigation.
- Added daily or weekday recurrence, four custom quick-time values, schedule history, and reusable profiles.
- Added battery, AC-power, and idle-time safeguards. A schedule waits when a condition fails and is canceled after the configured limit.
- Added start-with-Windows, wake-timer scheduling, and a system-tray countdown.
- Added sleep and hibernate alongside shutdown and restart.
- Added Korean, English, Japanese, and Simplified Chinese UI plus a remote browser page for phones and other PCs.
- Remote access works on the same private network or through a VPN shared by both devices. The app explains that LAN HTTP is unencrypted and protects saved pairing tokens with Windows DPAPI.
- Added a link to the private GitHub Releases page for manual version checks.
- Fixed safeguard wait time so it continues across five-minute deferrals, and saved the date/time in profiles.
- Expanded safe checks for recurrence, safeguards, localization, and protected token storage. Tests issue no Windows power commands.

### 日本語
- メインの予約画面は維持し、左側のナビゲーションに独立した **詳細** ページを追加しました。
- 毎日・曜日別の繰り返し予約、4つのカスタム時間、予約履歴、再利用できるプロファイルを追加しました。
- バッテリー残量、AC電源、アイドル時間の保護条件を追加しました。条件が合わない場合は保留し、設定時間後に予約を取り消します。
- Windowsログイン時の起動、予約時のスリープ解除、システムトレイのカウントダウンを追加しました。
- シャットダウンと再起動に加え、スリープと休止状態を予約できます。
- 韓国語・英語・日本語・簡体字中国語のUIと、スマートフォン・別PC向けのリモート画面を追加しました。
- 同じプライベートネットワーク、または両端末を接続したVPNでリモート利用できます。LAN上のHTTP通信は暗号化されないことを明記し、保存した接続トークンはWindows DPAPIで暗号化します。
- 非公開 GitHub Releases ページを開く手動バージョン確認機能を追加しました。
- 5分延長後も保護条件の待機時間が累積するよう修正し、プロファイルに日時を保存します。
- 繰り返し・保護条件・多言語・トークン保存の安全テストを追加しました。テストは電源命令を実行しません。

### 简体中文
- 保持现有主预约界面不变，并在左侧导航中新增独立的 **高级** 页面。
- 新增每日或按星期重复计划、四个自定义快捷时长、预约记录和可复用配置。
- 新增电量、交流电源和空闲时间保护条件。条件不满足时会等待，超过设定上限后取消预约。
- 新增 Windows 登录时启动、预约时唤醒电脑和系统托盘倒计时。
- 除关机和重启外，新增睡眠与休眠预约。
- 新增韩语、英语、日语和简体中文界面，以及供手机和其他电脑使用的远程网页。
- 远程访问支持同一专用网络，或连接到同一 VPN 的设备。应用会说明局域网 HTTP 未加密，并使用 Windows DPAPI 加密保存的连接令牌。
- 新增打开私有 GitHub Releases 页面以手动查看版本的功能。
- 修复保护条件等待时间在每次延后 5 分钟后被重置的问题，并在配置中保存日期和时间。
- 扩展安全检查，覆盖重复计划、保护条件、多语言和令牌存储。测试不会执行 Windows 电源命令。

## v1.0.1 — 2026-10-24

### 한국어
- 종료·재부팅, 분 단위/날짜·시간, 빠른 시간 선택 버튼을 누른 직후 선택 색상이 바로 갱신되도록 수정했습니다.

### English
- Fixed selected colors so action, time-mode, and quick-duration buttons update immediately after a click.

### 日本語
- 終了・再起動、時間モード、時間プリセットのボタンをクリックした直後に選択色が更新されるよう修正しました。

### 简体中文
- 修复了点击关机/重启、时间模式和快捷时长按钮后，选中颜色不能立即更新的问题。

## v1.0.0 — 2026-10-24

### 한국어
- 제품명을 **윈도우 타이머**로 통일하고 Windows용 새 앱 아이콘과 바로가기를 적용했습니다.
- 3D 이머시브 참고 이미지에서 영감을 받은 크림·브라운·테라코타 UI를 적용했습니다.
- Google Fonts Noto Sans KR을 앱에 포함하고 라이선스 안내를 추가했습니다.
- 종료·재부팅 예약, 날짜/시간 지정, 빠른 시간 선택, 60초 전 알림, 취소 및 10분 연장을 제공합니다.
- 절전 복귀 후 추가 대기와 시스템 트레이 유지 동작을 다듬었습니다.
- README, 실행 화면, 빌드 안내, 안전 데모와 예약 로직 자가 검사를 추가했습니다.

### English
- Renamed the product to **WindowsTimer** and updated its Windows icon and shortcuts.
- Added a cream, brown, and terracotta interface inspired by the supplied immersive 3D reference.
- Bundled Noto Sans KR from Google Fonts and included its license notice.
- Added shutdown/restart scheduling, date/time selection, presets, a 60-second warning, cancellation, and a ten-minute extension.
- Improved sleep-resume grace time and system-tray behavior.
- Added the multilingual README, app screenshot, build guide, safe demo, and schedule self-test.

### 日本語
- 製品名を **WindowsTimer** に統一し、Windows アイコンとショートカットを更新しました。
- 共有された 3D イメージを参考に、クリーム・ブラウン・テラコッタの UI を追加しました。
- Google Fonts の Noto Sans KR とライセンス表記を同梱しました。
- 終了・再起動予約、日時指定、プリセット、60 秒前通知、キャンセル、10 分延長を追加しました。
- スリープ復帰後の待機時間とシステムトレイの動作を改善しました。
- 多言語 README、画面画像、ビルド手順、安全デモ、予約ロジックのセルフテストを追加しました。

### 简体中文
- 将产品名称统一为 **WindowsTimer**，并更新 Windows 图标和快捷方式。
- 根据提供的 3D 沉浸式参考图，设计了奶油色、棕色和陶土色界面。
- 内置 Google Fonts 的 Noto Sans KR，并附带字体许可证。
- 支持预约关机/重启、日期时间、快捷时长、提前 60 秒提醒、取消和延长 10 分钟。
- 改进从睡眠恢复后的等待时间和系统托盘行为。
- 新增多语言 README、应用截图、构建指南、安全演示和预约逻辑自测。
