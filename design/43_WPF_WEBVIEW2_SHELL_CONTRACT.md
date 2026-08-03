# WPF WebView2 Shell契約

版: 0.1-p1-020
状態: P1-032実装済み
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

## P1-028 Local mode補足

本契約のServer URL、readiness、origin制約はServer modeに適用する。ADR-019により、Windowsアプリの既定起動はLocal modeとし、Server未導入・停止・障害・接続不能でもWebView2またはLocal UIのホームへ進める設計へ置換する。Local modeではServer URLのreadiness確認、Kestrel、localhostポート、HTTP APIを起動しない。

Server接続失敗時には、Local mode、Server設定、再試行、終了の利用者向け導線を持たせる。具体的な画面とLocal Application Channelは後続チケットで実装し、本契約ではNavigation安全境界を変更しない。

## P1-032 Local mode実装

- 引数なし起動はLocal modeとし、`WebAssets/index.html`を`https://app.ai-development-manager.local/index.html`へ表示する。
- `--server-url=<loopback URL>`指定時だけServer modeとし、既存のreadiness確認とloopback origin制限を使用する。
- Local modeのWeb資産はWPF出力の`WebAssets`に固定配置し、資産欠落時は起動時に修復・再インストール案内を表示する。
- Web資産Buildは`Build-WebAssets.mjs`の排他ロック下で実行し、WPFとServerの出力へ同一内容を段階配置する。
- Local modeは固定仮想origin以外のNavigation／Resourceを拒否し、新規Windowを開かない。
- Local modeのUserDataFolderは`%LOCALAPPDATA%\AI Development Manager\WebView2\Local`に固定する。
