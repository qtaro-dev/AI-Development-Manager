# P1-032 WPF組み込みWeb UIローダー契約

## 実行モード

| 起動 | モード | 動作 |
| --- | --- | --- |
| 引数なし | Local | WPF出力内の`WebAssets/index.html`を固定仮想HTTPS originへ割り当てて表示する。Server readiness、Kestrel、HTTP API、localhost待受は行わない |
| `--server-url=<loopback URL>` | Server | 既存のreadiness確認とloopback origin制限を利用してServer配信UIを表示する |

起動モードは引数から明示的に決定し、ブラウザ環境やネットワーク状態から自動判定しない。

## Web資産境界

`Adm.Web`のproduction buildは`Build-WebAssets.mjs`の排他ロック下で一度に一つだけ生成する。WPFは`WebAssets`へ、Serverは`wwwroot`へ同じ`dist`内容をコピーする。WPFとServerのプロジェクトが並列にBuildされても、Web build自体は共有ロックで直列化される。資産は`index.html`を必須とし、欠落時は起動時に安全な修復・再インストール案内を表示する。

## Local WebView2境界

- 仮想originは`https://app.ai-development-manager.local/`に固定する。
- `SetVirtualHostNameToFolderMapping`へWPF出力の`WebAssets`だけを`DenyCors`で割り当てる。
- Navigationは固定originのみ許可する。
- WebResourceは固定origin以外を403で拒否する。外部サイト、`file://`、localhost、割当外パスを許可しない。
- 新規Windowは常にHandledとする。
- UserDataFolderは`%LOCALAPPDATA%\AI Development Manager\WebView2\Local`に固定し、他アプリと共有しない。
- `CreateAsync`と`EnsureCoreWebView2Async`は非同期で実行し、10秒のタイムアウトを設ける。

## Server mode互換

`--server-url`指定時だけServer modeとなり、既存のloopback scheme／host／port一致検査、readiness確認、Server origin Navigation制限を維持する。ServerはWPF終了時に停止しない。

## 失敗時の利用者案内

WebView2 Runtime不足、Web資産欠落、初期化失敗、読み込み失敗をそれぞれ安全な日本語案内へ変換する。例外本文、パス、Stack Trace、秘密情報は画面へ表示しない。再試行と終了（ウィンドウを閉じる）の導線を維持する。

P1-032ではLocal Application Channel、DataAccess PortへのLocal Adapter接続、業務機能、初回ウィザード、Server設定UI、Bridge操作追加は行わない。
