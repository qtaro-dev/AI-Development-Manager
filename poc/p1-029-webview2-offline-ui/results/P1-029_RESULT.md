# P1-029 PoC結果

## 判定

**条件付き採用**。LiteTube Dock起動中でも、PoC専用UserDataFolderを使用したWebView2で、Server・Kestrel・HTTP API・localhost待受なしに既存React buildを表示できた。製品実装へはPoCコードをコピーせず、方式と制約だけを引き継ぐ。

条件は、WebView2 Evergreen Runtimeを前提とすること、実装時にUserDataFolderをアプリ単位・プロファイル単位で分離すること、仮想HTTPS originとNavigation／Resource境界を維持すること、配布後のRuntime差とWPF実機回帰を受入条件にすることである。不採用時の代替案（隠れServerへ戻さず、WPFネイティブUIまたは別の同一プロセスUIホストを設計レビューへ戻す）は今回は不要と判断した。

## 実行環境

| 項目 | 実測値 |
| --- | --- |
| OS | Windows 11 (10.0.26200), win-x64 |
| .NET SDK | 10.0.302 (`global.json`固定) |
| TargetFramework | net10.0-windows |
| WebView2 Runtime | 150.0.4078.105 |
| Node.js | v22.18.0 |
| npm | 10.9.3 |
| 共存アプリ | LiteTube Dock 起動中（PID 36792、応答中） |

## 実施内容

- `src/Adm.Web`を`npm ci`後にproduction buildし、成果物をPoC専用assetsへコピー。
- WebView2の`https://p1-029.local/`仮想HTTPS originへassetsフォルダーを割当。
- 実行ごとに`artifacts/.../run-N/webview2-user-data`を使用。同じUserDataFolderをLiteTube Dockや他のPoC実行と共有していない。
- `EnsureCoreWebView2Async`をUIスレッド上で非同期待機し、Environment作成・Core初期化・Console Protocolにタイムアウトを設定。
- 外部Navigation、新規Window、外部Resourceを拒否し、固定origin内のResourceだけを許可。
- サーバー、Kestrel、HTTP Listener、localhostポート、API要求は使用していない。

## 5回の起動時間

| 回 | UI ready | 終了コード |
| ---: | ---: | ---: |
| 1 | 3,478 ms | 0 |
| 2 | 3,250 ms | 0 |
| 3 | 3,348 ms | 0 |
| 4 | 3,209 ms | 0 |
| 5 | 3,211 ms | 0 |

全5回が暫定基準の5秒以内だった。

## オフライン・安全境界結果

ネットワークアダプターは変更せず、PoC側で固定origin以外のResourceを403応答へ置換する同等のネットワーク分離条件を使用した。外部サイトへの依存なしでUI表示・再読込が完了した。

- Navigation: 固定originは許可、`https://example.com/...`は拒否。
- New window: `window.open`を`Handled=true`で拒否。
- Resource: 固定origin以外は拒否。Server／localhost要求なし。
- Console: `Runtime.consoleAPICalled`を記録。表示確認用`info` probeのみで、未処理例外はなし。
- Screenshot: `artifacts/p1-029-webview2-offline-ui/run-1/screenshot-initial.png`。
- Windowsイベントログ: 過去の異常終了は測定パスの空白分割による`UnauthorizedAccessException`であり、WebView2共存障害ではなかった。引数引用修正後の5回は終了コード0。

## 未解決事項・後続受入条件

- WebView2 Runtime未導入時の利用者向け導線は製品実装時に定義する。
- 実機で100～200% DPI、日本語IME、長時間再読込、複数Window、製品Routerを確認する。
- 配布後のUserDataFolder ACL、ロック、更新・アンインストール時の保持方針を確認する。
- Console／Navigation／Resourceの監査保存期間と製品ログ方針は別途確定する。

## 再現コマンド

```powershell
dotnet --version
dotnet restore .\poc\p1-029-webview2-offline-ui\OfflineUiPoc.csproj --runtime win-x64
powershell -NoProfile -ExecutionPolicy Bypass -File .\poc\p1-029-webview2-offline-ui\Run-Poc.ps1 -Configuration Release
dotnet build .\poc\p1-029-webview2-offline-ui\OfflineUiPoc.csproj --configuration Debug --no-restore
```

生成物・UserData・ログ・スクリーンショットは`artifacts/`配下でGit管理対象外とした。
