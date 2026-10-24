# WindowsTimer v1.1.4

### 한국어

- 휴대폰이 5G이고 PC가 유선 인터넷에 연결된 경우에도 원격 예약을 할 수 있도록 고급 설정에 **5G 인터넷 연결 시작/중지** 버튼을 추가했습니다.
- PC에 표시되는 임시 `https://...trycloudflare.com` 주소를 휴대폰에서 열고, PC에 표시된 6자리 코드를 입력해 연결합니다. VPN과 라우터 포트 전달은 필요하지 않습니다.
- Cloudflare `cloudflared` 실행 파일은 사용자가 버튼을 눌렀을 때 공식 GitHub 릴리스에서 내려받고 SHA-256을 검증합니다. 터널은 앱을 종료하면 중지되고, 다시 시작할 때 임시 주소가 달라집니다.
- DNS가 준비되고 HTTPS 연결 화면이 실제로 열리는지 확인한 뒤 주소를 표시합니다.
- HTTPS 터널의 Origin 검증과 청크 방식 요청 본문 처리를 지원합니다.

같은 Wi-Fi에서는 기존 LAN 주소를 계속 사용할 수 있습니다. LAN HTTP는 암호화되지 않으므로 신뢰할 수 있는 사설망에서만 사용하세요. 5G 요청은 Cloudflare를 거쳐 전달됩니다. 임시 터널은 상시 서비스용이 아니며, 링크를 아는 사람은 코드 입력 화면에 접근할 수 있습니다. 예약을 연결하려면 PC의 5분 유효 6자리 코드가 필요합니다.

### English

- Added **Start/stop 5G internet access** in Advanced settings so a phone on cellular data can schedule actions on a PC using wired internet.
- Open the temporary `https://...trycloudflare.com` address shown on the PC, then enter its six-digit pairing code. No VPN or router port forwarding is required.
- When requested, the app downloads Cloudflare `cloudflared` from the official GitHub release and verifies SHA-256. The tunnel stops with the app, and its temporary address changes after a new start.
- The app confirms DNS and the HTTPS pairing page are reachable before showing the address.
- Supports HTTPS tunnel origin checks and chunked request bodies.

The existing LAN address remains available on the same Wi-Fi. LAN HTTP is unencrypted and should be used only on a trusted private network. 5G requests are relayed through Cloudflare. The temporary tunnel is not an always-on service; anyone with its link can open the pairing page, while scheduling still requires the six-digit code shown on the PC, valid for five minutes.

### 日本語

- 詳細設定に**5Gインターネット接続の開始/停止**を追加しました。スマートフォンがモバイル通信、PCが有線インターネットでもリモート予約を利用できます。
- PCに表示された一時的な `https://...trycloudflare.com` アドレスをスマートフォンで開き、6桁コードを入力します。VPNやルーターのポート転送は不要です。
- ボタン操作時にCloudflare公式GitHubリリースから `cloudflared` をダウンロードし、SHA-256を検証します。アプリ終了時にトンネルを停止し、再起動後は一時アドレスが変わります。
- DNSとHTTPSの接続画面が開けることを確認してからアドレスを表示します。
- HTTPSトンネルのOrigin検証とチャンク形式のリクエスト本文に対応しました。

同じWi-Fiでは従来のLANアドレスも使えます。LANのHTTP通信は暗号化されないため、信頼できるネットワークでのみ使用してください。5Gの通信はCloudflareを経由します。一時トンネルは常時稼働向けではありません。リンクを知っている人はペアリング画面を開けますが、予約にはPCに表示された5分間有効な6桁コードが必要です。

### 简体中文

- 在高级设置中添加**启动/停止 5G 网络连接**，即使手机使用蜂窝网络、电脑使用有线网络，也能远程预约。
- 在手机上打开电脑显示的临时 `https://...trycloudflare.com` 地址，然后输入电脑上的 6 位配对码。无需 VPN 或路由器端口转发。
- 用户点击后，应用从 Cloudflare 官方 GitHub 发布页下载 `cloudflared` 并验证 SHA-256。关闭应用时隧道会停止；重新启动后临时地址会改变。
- 确认 DNS 和 HTTPS 配对页面可访问后，应用才会显示地址。
- 支持 HTTPS 隧道来源验证和分块请求正文。

同一 Wi-Fi 下仍可使用原有 LAN 地址。LAN 使用未加密的 HTTP，仅限可信网络。5G 请求会通过 Cloudflare 中转。临时隧道不保证持续运行；知道链接的人可以打开配对页面，但远程预约仍需要输入电脑显示的 6 位代码，代码 5 分钟后失效。
