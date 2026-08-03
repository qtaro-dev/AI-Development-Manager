# P1-029 WebView2オフラインUI PoC

## 目的

Server、Kestrel、localhostポート、HTTP APIを使用せず、Windowsアプリへ同梱した共通React Web UIのビルド成果物をWebView2で安全に表示できることを、独立した技術PoCで確認する。

PoC結果から、WindowsアプリのLocal modeで採用するWeb UI読込方式、その制約、製品実装へ進むための条件を判断できる状態にする。

## 背景

P1-028／ADR-019で、Windowsアプリを主製品、Server/APIを任意導入の追加機能とするローカルファースト実行モデルを確定した。Local modeはServer未導入、停止、障害、接続不能の影響を受けず、隠れたServerプロセスも起動しない。

現在のWPF ShellはServer URLのreadiness確認後にServer配信Web UIへ遷移する構成である。製品実装を変更する前に、WebView2のローカル資産読込、origin、CSP、SPA再読込、Navigation、オフライン動作、配布形態の成立性を確認する必要がある。

## 前提・依存関係

- P1-028承認済み
- `design/50_ADR_019_LOCAL_FIRST_EXECUTION_MODEL.md`
- `design/43_WPF_WEBVIEW2_SHELL_CONTRACT.md`のP1-028 Local mode補足
- `design/44_WPF_BRIDGE_CONTRACT.md`のPlatform Bridge分離方針
- P1-013～P1-018で構築済みのReact／TypeScript／Vite共通UI基盤
- P1-020／P1-023で確認済みのWPF WebView2起動・Runtime互換結果
- ルート`global.json`の.NET SDK 10.0.302固定
- Windows 11 64-bit、WebView2 Evergreen Runtime

## 対象範囲

- 独立したWPF WebView2 PoC Host
- `src/Adm.Web`の既存ビルド成果物を入力とするオフライン表示検証
- WebView2の仮想HTTPS originによるローカルフォルダー割当方式
- `index.html`、JavaScript、CSS、フォント、画像等のローカル資産読込
- ルート画面の再読込と、PoCで定める最小のクライアント側ルート再表示
- 固定origin、トップレベルNavigation、新規Window、外部URLの安全境界
- ネットワーク切断状態、Server未導入状態、Server停止状態での起動
- Serverプロセス、Kestrel、HTTP Listener、localhostポートを使用していないことの確認
- WebView2 Console、Navigation、Resource要求、起動時間の記録
- 採用、条件付き採用、不採用の製品採用判断

## 対象外

- `src/`、`tests/`、`installer/`の製品実装変更
- 現行WPF ShellのLocal mode対応
- DataAccess Portの実装
- Local Application ChannelのRequest、Response、Error実装
- Progress、Cancel、Streaming、分割転送、イベント購読
- Platform Bridgeの変更または新規操作追加
- プロジェクト、Markdown、チケット、添付、テスト、検索等の業務データ表示
- 初回設定、Server設定、再試行、終了等の製品UI実装
- Self-contained配布、MSI、Shortcut、Iconの修正
- Server MSI、Windows Service、HTTPS、認証、LAN公開の修正
- `file://`を製品採用方式とすること
- P1-030以降のチケット作成または着手

## 対象ファイルまたは対象モジュール

- `poc/p1-029-webview2-offline-ui/`
  - PoC専用.NET 10 WPFプロジェクト
  - 実行手順
  - テスト補助
  - 匿名化された結果記録
- `src/Adm.Web/`
  - 読み取りと既存Buildの実行だけを許可する
  - ソース、設定、依存、lockfileを変更しない
- `poc/p1-029-webview2-offline-ui/results/`
  - SDK、Runtime、OS、手順、結果、制約、採否
- `tickets/phase1/P1-029_WEBVIEW2_OFFLINE_UI_POC.md`
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

生成したWeb UI、`bin/`、`obj/`、WebView2 UserData、ログ、一時ファイルはコミットしない。

## 具体的なPoC内容

1. リポジトリ直下で`dotnet --version`を実行し、`global.json`の10.0.302と一致することを確認する。一致しない場合はPoC実装・測定を開始しない。
2. `src/Adm.Web`を固定lockfileからBuildし、生成物をPoC専用の一時配置へコピーする。製品WebソースとBuild設定は変更しない。
3. .NET 10 WPFとWebView2を使用する独立PoC Hostを`poc/`配下へ作成する。製品ソリューションへ追加しない。
4. WebView2の仮想ホスト名とローカルフォルダー割当機能を使用し、製品候補の固定HTTPS originから`index.html`を表示する。
5. `file://`はorigin、CORS、相対資産、Navigation上の差を確認する負例に限定し、正式候補として実装を広げない。
6. HTML、JavaScript、CSS、フォント、画像等がローカル資産だけから読み込まれ、外部CDNやServerへ要求しないことを記録する。
7. ルート画面の初回表示、再読込、戻る／進む、PoC用の最小クライアント側ルートまたはfragment再表示を確認する。製品Router方式は本PoCで確定しない。
8. 固定ローカルorigin以外のトップレベルNavigationと新規WindowをWebView2内で開かず、拒否または外部処理候補として記録する。PoCから外部アプリを実際に起動しない。
9. 相対パス、存在しない資産、`..`を含む要求、任意ローカルパスへのアクセスを確認し、割当フォルダー外を読み取れないことを記録する。
10. ネットワークアダプターを無効化した状態、または同等のオフライン条件でPoCを起動し、UI表示と再読込を確認する。
11. PoC実行中に`Adm.Server.Host`、Kestrel、HTTP Listener、TCP待受ポートが起動していないことを、プロセスと待受状態から確認する。
12. 5回の連続起動で、WebView2初期化開始からUI表示完了までを測定する。各回5秒以内を暫定成功条件とし、最小、中央値、最大、測定方法を記録する。
13. WebView2 Runtime未導入または初期化失敗を実際に再現できない場合は、検出分岐の自動試験と既存P1-020／P1-023証拠を使用し、未実施条件を明記する。
14. 結果を採用、条件付き採用、不採用に分類し、製品実装へコピーせず、採用すべき方式と制約だけを設計へ反映する。

## PoCの最小画面

PoC画面は技術確認に必要な情報だけを表示する。

- 「オフラインUI PoC」等の検証用見出し
- 読み込んだUIのBuild情報
- 現在のorigin
- ローカル資産読込結果
- 再読込操作
- 検証結果を判別できる状態表示

完成版のローカルホーム、設定画面、接続失敗画面、業務ナビゲーションは作成しない。Vol.5ガードレールの製品UI確定作業へ進まず、既存UIを壊さない最小PoC表示に限定する。

## テスト内容

### Build・環境

- `dotnet --version`が10.0.302
- PoCのDebug／Release Build
- `src/Adm.Web`の既存Build
- WebView2 Runtimeの版と検出結果
- Windows 11 64-bitの版、表示倍率、ネットワーク条件

### オフライン表示

- Server未導入
- Server停止
- ネットワーク切断
- 初回表示
- 5回連続起動
- 再読込
- PoC用クライアント側ルートまたはfragmentの再表示
- JavaScript／CSS／フォント／画像の読込

### 安全境界

- 固定仮想HTTPS origin
- 外部HTTP／HTTPS Navigation
- 新規Window
- `file://`
- 存在しない資産
- `..`を含む相対パス
- 割当外の任意ローカルファイル
- WebView2 Consoleの重大エラー
- 外部ネットワークResource要求

### Server非依存

- `Adm.Server.Host`プロセスなし
- Windows Serviceなし
- Kestrelなし
- HTTP Listenerなし
- PoCプロセスによるTCP待受なし
- `localhost`／`127.0.0.1`へのUI要求なし

### 回帰確認

- 製品`src/`、`tests/`、`installer/`に差分がない
- P1-001～P1-028のBuild、Test、Architecture検査を壊していない
- PoC生成物とWebView2 UserDataがGit追跡対象へ混入していない

## 成功条件

- Server未導入、Server停止、ネットワーク切断の各状態で、PoCの組み込みWeb UIを表示できる。
- UI表示にKestrel、HTTP API、HTTP Listener、localhostポート、Serverプロセスを使用していない。
- 固定仮想HTTPS originから、HTML、JavaScript、CSS、フォント、画像等をローカルに読み込める。
- ルート画面の初回表示、再読込、PoC用クライアント側ルートまたはfragmentの再表示が成功する。
- 外部Navigation、新規Window、`file://`、割当外ローカルファイルを安全境界どおり扱える。
- 外部CDN、外部API、Server、localhostへの意図しないResource要求がない。
- 5回の連続起動が各回5秒以内で、測定結果と環境が記録されている。
- LiteTube Dock等、他のWebView2利用アプリが起動中でも、PoC専用UserDataFolderで正常に表示・再読込できる。
- WebView2 ConsoleにUI表示を妨げる未処理例外または重大Resourceエラーがない。
- 固定SDK、WebView2 Runtime、OS、実行コマンド、結果、スクリーンショット、既知の制約が記録されている。
- PoCコードが`poc/`へ隔離され、製品コードへ自動昇格していない。
- Local Application Channel、DataAccess Port、業務画面、Installerを実装していない。

## 製品採用判断基準

### 採用

すべての成功条件を満たし、仮想HTTPS originからの組み込みWeb UI読込をLocal modeの製品候補として採用できる。製品実装ではPoCコードをコピーせず、確認した方式と制約を入力として再実装する。

### 条件付き採用

UI表示、Server非依存、安全境界は成立するが、SPA再読込、起動時間、WebView2 Runtime差、配布時パス等に限定的な課題がある。課題、影響、回避策、再確認先を明記し、製品実装チケットの受け入れ条件へ移す。

### 不採用

Serverなしでは共通UIを安定表示できない、割当外ファイルへアクセスできる、外部Resourceへ暗黙依存する、または重大なNavigation境界違反がある。隠れたlocalhost Serverへ自動的に戻さず、代替方式の設計レビューを別チケットとして提案する。

## 完了条件

- 上記PoC、テスト、目視確認を一度の実装・レビュー・実機確認で完了判定できる。
- 合格・条件付き合格・不合格を、再現可能な証拠とともに記録している。
- 採用方式、制約、製品実装への入力、未解決事項が一意に分かる。
- P1-030以降を作成・実装していない。
- ユーザーがPoC結果を確認し、次のチケット作成可否を判断できる。

## ユーザーが目視確認する内容

- Serverをインストール・起動していない状態でのPoC起動
- WPF WebView2内の組み込みWeb UI
- 現在の固定仮想HTTPS origin
- ネットワーク切断中の表示と再読込
- 外部Navigationと新規Windowの拒否結果
- 5回の起動時間一覧
- WebView2 Console／Resource要求の重大エラーなし
- PoC結果の採用・条件付き採用・不採用判定

## 想定されるリスク

- PoCが製品WPF Shellの改修へ広がる。
- `file://`で一時的に表示できたことを安全な採用根拠にする。
- 開発環境のVite Dev Serverを使用し、オフライン成立と誤認する。
- 外部フォント、CDN、API、localhostへの依存を見落とす。
- 仮想originから割当外ローカルファイルを読める設定にする。
- SPAの深いURL再読込を本PoCだけで過剰設計する。
- Platform BridgeまたはLocal Application Channelの実装を混在させる。
- WebView2 UserData、生成Web資産、ログ、個人情報をコミットする。
- 起動時間の初回／ウォーム差やVM性能を記録せず、測定値だけを比較する。

## 完了後に更新すべき設計資料

- `design/00_INDEX.md`
- `design/30_PHASE1_IMPLEMENTATION_PLAN.md`
- `design/43_WPF_WEBVIEW2_SHELL_CONTRACT.md`
- P1-029のPoC結果契約または技術判断記録
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`
- `tickets/phase1/P1-029_WEBVIEW2_OFFLINE_UI_POC.md`

P1-029ではADR-019のローカルファースト判断を再検討しない。PoC結果が不採用の場合も、Server必須設計へ自動的に戻さず、代替のオフラインUI方式を未決事項として分離する。

## 完了時に残す証拠

- `global.json`の固定SDK値と`dotnet --version`実測値
- OS、WebView2 Runtime、Node.js、npmの実測値
- 再現コマンドと終了コード
- Debug／Release Build結果
- オフライン条件とServer非依存確認結果
- Resource要求・Console・Navigation記録
- 5回の起動時間
- 代表スクリーンショット
- 採用、条件付き採用、不採用の判定と理由
- 既知の制約、後続受け入れ条件、未解決事項

## 状態

実施済み（条件付き採用、レビュー待ち）。

結果は`poc/p1-029-webview2-offline-ui/results/P1-029_RESULT.md`に記録した。LiteTube Dock起動中に5回の表示・再読込を確認し、全回5秒以内・終了コード0だった。P1-030以降の作成・実施は、P1-029のレビュー・承認後にユーザー指示を待つ。
