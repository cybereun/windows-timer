# WindowsTimer v1.1.5

### 한국어

- **휴대폰 연결 QR:** 원격 설정 카드에 연결 QR을 표시합니다. 같은 Wi-Fi에서는 LAN QR을, 5G 인터넷 연결을 시작한 뒤에는 새 `5G` QR을 스캔하세요.
- **코드 자동 입력:** QR을 스캔하면 휴대폰에서 연결 페이지가 열리고, 6자리 연결 코드가 자동으로 제출되어 바로 페어링됩니다.
- **짧은 수명의 코드:** 연결 코드는 5분 동안 한 번만 유효하며 사용 후 또는 만료 후 자동으로 다시 발급됩니다. QR을 다른 사람과 공유하지 마세요.
- QR 생성 라이브러리 QRCoder 1.8.1을 실행 파일에 포함하고 MIT 라이선스를 배포 ZIP에 넣었습니다.

### English

- **Phone pairing QR:** Remote settings show a QR for the current connection. Scan the LAN QR on the same Wi-Fi, or scan the refreshed QR marked `5G` after starting internet access.
- **Automatic pairing:** Scanning opens the phone page and submits the six-digit pairing code automatically.
- **Short-lived code:** The code works once for five minutes and renews after use or expiry. Do not share the QR with others.
- Bundles QRCoder 1.8.1 in the executable and includes its MIT license in the release ZIP.

### 日本語

- **スマートフォン接続用QR:** リモート設定に接続先のQRを表示します。同じWi-FiではLAN用を、5G接続を開始した後は新しく表示される `5G` 用QRをスキャンしてください。
- **自動ペアリング:** スキャンするとスマートフォンで接続ページが開き、6桁コードが自動送信されて接続します。
- **短時間のみ有効:** コードは5分間の1回限りで、使用後または期限切れ後に自動更新されます。QRを他人と共有しないでください。
- QRCoder 1.8.1を実行ファイルに同梱し、MITライセンスを配布ZIPに含めました。

### 简体中文

- **手机连接二维码：** 远程设置中会显示当前连接二维码。同一 Wi-Fi 下扫描 LAN 二维码；启动 5G 网络连接后，扫描更新并标有 `5G` 的二维码。
- **自动配对：** 扫描后会在手机上打开连接页面，并自动提交 6 位配对码。
- **短时有效代码：** 配对码仅在 5 分钟内有效一次，使用后或过期后会自动更新。请勿与他人分享二维码。
- 将 QRCoder 1.8.1 嵌入可执行文件，并在发布 ZIP 中附上 MIT 许可证。

### Verification

- `--self-test`: QR payload fragment, QR bitmap rendering, pairing-page auto-submit, and existing schedule safeguards.
- QR image independently decoded to the exact URL and one-time code with OpenCV.
- No shutdown or restart command was run during testing.
