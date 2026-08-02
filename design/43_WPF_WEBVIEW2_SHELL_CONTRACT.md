# WPF WebView2 Shell契約

版: 0.1-p1-020
状態: P1-020実装済み
基準日: 2026-08-03

## 起動と接続

- WPFは`--server-url=<localhost URL>`で接続先を受け取る。
- 既定値は`http://127.0.0.1:5181/`とする。
- 起動時に`/health/ready`を確認し、Server起動前は一定時間再試行する。
- 接続できない場合はServer URLと「再試行」を表示する。
- WPF終了時にServerを停止しない。

## WebView2 Runtime

WebView2 Evergreen Runtimeがない、または初期化できない場合は、Runtime導入と再試行を日本語で案内する。user data folderは`%LOCALAPPDATA%\AI Development Manager\WebView2`とし、業務データや秘密情報は保存しない。

## Navigation境界

許可するNavigationは設定Serverと同じscheme、host、portのoriginだけとする。外部URLはWebView2内で開かず拒否する。業務データBridge、Explorer起動、認証Cookieの実運用はP1-021以降へ分離する。
