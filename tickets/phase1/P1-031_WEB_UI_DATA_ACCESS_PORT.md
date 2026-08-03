# P1-031 Web UI DataAccess Port

## 目的

共通React UIがHTTP API、WebView2、Local Application Channel等の具体的な実行方式へ直接依存しないよう、型付きの`DataAccess Port`境界を製品Web基盤へ追加する。

Local modeとServer modeが同じUIコンポーネントを利用できる差替境界を作り、後続チケットが通信方式と業務画面を混在させず実装できる状態にする。

## 背景

P1-028で、React UIはDataAccess Portだけを参照し、Local Application ChannelとHTTP API Adapterを切り替える方針を承認した。P1-030では、WPFとServerを独立Composition Rootとし、両者が同じApplication／Coreを利用する参照境界を確定した。

現在の`src/Adm.Web/src/api/client.ts`はHTTP `fetch`を直接扱う。業務画面から同様の直接依存が増える前に、Web UI内部のPort、Adapter、Composition境界を最小構成で確立する必要がある。

## 前提・依存関係

- P1-028承認済み
- P1-029条件付き採用として承認済み
- P1-030承認済み
- P1-013～P1-018のReact、テスト、文言、Theme、AppShell基盤
- `design/50_ADR_019_LOCAL_FIRST_EXECUTION_MODEL.md`
- `src/Adm.Web/src/api/client.ts`の既存HTTP基盤

## 対象範囲

- Web UI内のDataAccess Port配置・命名・責務
- Local／Server等の実行モードを外部から注入できるComposition境界
- 型付き要求・結果・利用者向け失敗のWeb側共通表現
- 既存HTTP ClientをHTTP Adapter境界へ隔離する最小整理
- テスト用Fake AdapterによるPort差替試験
- UIコンポーネントから`fetch`やWebView2 APIを直接使用しない検査
- Vitest／Testing Libraryによる単体テスト
- 既存React UIのBuild、Test、Playwright回帰
- DataAccess Port契約の設計記録

## 対象外

- Local Application ChannelのRequest／Response／Error Envelope実装
- `window.chrome.webview`、WebMessage、WPFコードの利用
- 製品WPFへの組み込みWeb UI読込
- Server URL設定、接続試行、認証、HTTPS、Cookie
- プロジェクト、Markdown、チケット、添付、テスト、検索等の業務メソッド
- 汎用RPC、汎用Repository、プラグイン可能なTransport基盤
- Progress、Cancel、Streaming、イベント購読
- 画面レイアウト、ナビゲーション、文言、Themeの変更
- `src/Adm.Wpf`、`src/Adm.Server.Host`、`installer/`の変更
- P1-032／P1-033の実装
- P1-034以降のチケット作成または着手

## 対象ファイルまたは対象モジュール

- `src/Adm.Web/src/data-access/`
- `src/Adm.Web/src/api/client.ts`
- `src/Adm.Web/src/env.ts`
- `src/Adm.Web/src/test/`
- `src/Adm.Web/src/**/*.test.ts`
- `design/30_PHASE1_IMPLEMENTATION_PLAN.md`
- P1-031のWeb UI DataAccess Port契約資料
- `tickets/phase1/P1-031_WEB_UI_DATA_ACCESS_PORT.md`
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

UIコンポーネントの見た目を変更しない。既存HTTP ServerのAPI契約とOpenAPI正本を変更しない。

## 設計方針

- UIコンポーネントはDataAccess Portの型付きメソッドだけを呼び出す。
- PortはHTTP、WebView2、WPF、Server、ファイルパス等の技術用語を公開しない。
- HTTP `fetch`はHTTP Adapter内だけで使用する。
- WebView2 APIは本チケットで使用せず、後続Local Adapterだけへ隔離する。
- 実行モードはグローバル自動判別せず、Composition境界から明示的に注入する。
- Phase 1では基盤確認に必要な最小型だけを置き、将来業務操作を予測した巨大Interfaceを作らない。
- 業務機能追加時は、ユースケース単位の型付きメソッドを独立チケットで追加する。

## 具体的な実装内容

1. `data-access`配下へPort、結果型、Adapter生成境界、テストFakeを配置する。
2. Portが公開するPhase 1の基盤確認操作を1件に限定し、Local／HTTP差替可能性を検証する。操作名と結果型はServer固有のURLやHTTP Statusを公開しない名称にする。
3. 既存`ApiClient`はHTTP Adapterの内部依存として扱い、Reactコンポーネントから直接importしない構成へ整理する。
4. Composition関数へ明示的な実行モードとAdapterを渡し、テストがFake Adapterへ差し替えられるようにする。
5. Local AdapterとWebMessage実装は作らず、後続P1-033で実装できるPort接続点だけを定義する。
6. 成功結果と利用者向け失敗を型で区別し、例外本文やStack TraceをUIへ渡す型を作らない。
7. UI層からの直接`fetch`、`window.chrome.webview`、Server URL直書きを検出するテストまたは静的検査を追加する。
8. Port、HTTP Adapter、Fake Adapter、Compositionの単体テストを追加する。
9. Web Build、Web Test、Playwrightスモークを実行し、表示差分がないことを確認する。
10. Portへのメソッド追加ルールと対象外事項を設計資料へ記録する。

## テスト内容

- Portの型付き成功結果
- Portの型付き失敗結果
- Fake Adapter差替
- HTTP Adapterが既存API Clientを使用すること
- UIコンポーネントがHTTP Adapter実装を直接参照しないこと
- UIコンポーネント内に直接`fetch`がないこと
- WebView2 APIを本チケットで追加していないこと
- 未知の実行モードまたはAdapter未指定時に安全に失敗すること
- `npm run build`
- `npm run test`
- `npm run typecheck`相当の型検査
- 既存Playwrightスモーク
- 既存画面の主要配置・Theme・狭幅表示に意図しない差分がないこと
- `git diff --check`

## 完了条件

- DataAccess Port、HTTP Adapter、Composition、Fake Adapterの責務が分離されている。
- UIコンポーネントがHTTP `fetch`、WebView2、WPFへ直接依存していない。
- 既存HTTP ClientがPortの外側ではなくHTTP Adapter内部へ隔離されている。
- PortのPhase 1操作が基盤確認用の最小1件に限定されている。
- 将来の業務機能を推測した巨大Interfaceを作っていない。
- 成功と利用者向け失敗を型で判別でき、内部例外を露出しない。
- Web Build、Test、型検査、Playwrightが合格する。
- 既存UIの見た目とServer modeの基盤動作を壊していない。
- WPF、Local Application Channel、業務画面を変更していない。
- P1-032／P1-033を実装していない。
- P1-034以降を作成・着手していない。

## ユーザーが目視確認する内容

- DataAccess Port、HTTP Adapter、Local Adapter予定位置、UIの依存図
- Portが公開する最小操作
- UIから直接HTTP／WebView2依存がなくなった検査結果
- Web Build、Test、Playwright結果
- 既存画面に意図しない視覚差分がないこと

## 想定されるリスク

- Portを汎用RPCや巨大Repositoryとして過剰設計する。
- HTTPのRequest／Response型をそのままUI共通型にする。
- 実行モードをブラウザ環境から暗黙推測し、テスト不能にする。
- UIコンポーネント内へ`fetch`やWebView2分岐が残る。
- P1-033より先にLocal Channelを仮実装する。
- 基盤変更と画面変更を混在させる。
- Server modeの既存API Clientを破壊する。

## 完了後に更新すべき設計資料

- `design/00_INDEX.md`
- `design/30_PHASE1_IMPLEMENTATION_PLAN.md`
- P1-031 Web UI DataAccess Port契約資料
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`
- `tickets/phase1/P1-031_WEB_UI_DATA_ACCESS_PORT.md`

## 完了時に残す証拠

- 変更ファイル一覧
- Port／Adapter依存図
- 公開する最小型と操作一覧
- Web Build、型検査、Test、Playwright結果
- 直接`fetch`／WebView2依存検査結果
- UI目視確認結果
- `git diff --check`結果

## 実施結果

- `src/Adm.Web/src/data-access/port.ts`へ、実行モード、最小1操作、型付き成功／安全な失敗結果を定義した。
- `http-adapter.ts`へ既存`api/client.ts`とHTTP `fetch`を隔離し、例外本文やStack TraceをUIへ返さないようにした。
- `composition.ts`へ明示的なmode／Adapter注入境界を追加した。Local Adapter、WebMessage、WPF、業務APIは実装していない。
- UIのAppはDataAccess PortをComposition Rootから受け取り、HTTP AdapterやWebView2を直接参照しない構成へ整理した。既存画面のレイアウト、文言、Themeは変更していない。
- Fake Adapter差替、HTTP成功／失敗、無効mode／Adapter未指定のテストを追加した。
- UIソースの直接`fetch`、WebView2 API、`api/client`直接importを検出する境界検査を追加した。
- P1-031の契約を`design/31_WEB_UI_DATA_ACCESS_PORT.md`へ記録した。

## 検証結果

- `npm.cmd --prefix .\src\Adm.Web run typecheck`: 成功
- `npm.cmd --prefix .\src\Adm.Web run data-access:check`: 成功
- `npm.cmd --prefix .\src\Adm.Web run test`: 11 files / 25 tests 成功
- `npm.cmd --prefix .\src\Adm.Web run build`: 成功
- `npm.cmd --prefix .\tests\Adm.Web.E2E run test`: 9 tests 成功。既存Playwrightランナーの終了処理が戻らず、専用のServer／Nodeプロセスを停止して環境を復旧した。
- P1-031ではWPF、Server、installer、P1-032以降を変更していない。

## 状態

実施済み、レビュー待ち。

P1-032へ進む前に、DataAccess Port契約とComposition境界のレビューを完了する。
