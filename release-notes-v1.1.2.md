# WindowsTimer v1.1.2

## 한국어

- 실행 후 자동으로 새 버전을 확인하고, 릴리즈 실행 파일을 다운로드·검증·교체한 뒤 앱을 다시 시작합니다. 6시간마다 다시 확인하며 **고급 → 원격 · 언어 → 업데이트 확인**에서 바로 확인할 수도 있습니다.
- 비공개 GitHub 저장소에 접근하려면 `winget install --id GitHub.cli`로 GitHub CLI를 설치하고 `gh auth login --hostname github.com`으로 한 번 로그인하세요. 기존 v1.1.1 사용자는 자동 업데이트 기능을 포함한 v1.1.2를 한 번 직접 설치해야 합니다.
- 일회 예약 중에는 새 버전을 내려받아도 예약이 끝날 때까지 적용을 미룹니다. 업데이트 전 파일의 SHA-256과 제품 버전을 검사합니다.

## English

- Checks for updates after launch and every six hours. A newer release is downloaded, verified, installed, and restarted automatically. Select **Advanced → Remote · language → Check for updates** to check now.
- The repository is private. Install GitHub CLI with `winget install --id GitHub.cli`, then sign in once with `gh auth login --hostname github.com`. Users on v1.1.1 must install v1.1.2 manually once to get auto-updates.
- An active one-time schedule finishes before an update is installed. The app checks the downloaded file's SHA-256 digest and product version.

## 日本語

- 起動後と6時間ごとに更新を確認し、新しいリリースがあればダウンロード、検証、置き換え、再起動を自動で行います。**詳細 → リモート・言語 → 更新を確認**から手動確認もできます。
- リポジトリは非公開です。`winget install --id GitHub.cli`でGitHub CLIをインストールし、`gh auth login --hostname github.com`で一度ログインしてください。v1.1.1のユーザーは、自動更新を有効にするためv1.1.2を一度だけ手動でインストールする必要があります。
- 1回予約の実行中は適用を待ち、ダウンロードしたファイルのSHA-256と製品バージョンを確認します。

## 简体中文

- 启动后及每六小时检查更新。发现新版本后会自动下载、验证、替换并重新启动。也可在**高级 → 远程 · 语言 → 检查更新**中立即检查。
- 仓库为私有。请运行 `winget install --id GitHub.cli` 安装 GitHub CLI，再运行 `gh auth login --hostname github.com` 登录一次。v1.1.1 用户需手动安装一次 v1.1.2，之后才能自动更新。
- 单次预约执行期间会延后安装，并验证下载文件的 SHA-256 和产品版本。
