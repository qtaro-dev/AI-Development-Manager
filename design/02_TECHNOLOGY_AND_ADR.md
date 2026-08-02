# AI Development Manager 採用技術案・技術ADR

版: 0.7-p0-002-hosting-boundary
基準日: 2026-08-02

## 1. 採用技術の確定案

| 領域 | 採用案 | 状態 |
|---|---|---|
| Server | .NET 10 LTS / ASP.NET Core 10 / Kestrel | 確定案 |
| Windows Client | .NET 10 WPF | 確定案 |
| 埋込Web | Microsoft Edge WebView2 Evergreen Runtime | 確定案 |
| Web UI | React 19 + TypeScript + Vite | Phase 0 PoC後に確定 |
| Web UI代替 | Blazor WebAssembly | React PoC不合格時の比較対象 |
| API | REST / JSON / OpenAPI 3.x | 確定案 |
| Markdown | CommonMark系Markdown + YAML Front Matter | 確定案 |
| .NET Markdown解析 | Markdig | PoCで互換性確認後に確定 |
| YAML解析 | YamlDotNetの安全な型制約付き利用 | PoCで確定 |
| ID | ULID + 種別別連番 | 確定案 |
| 索引 | SQLite FTS5 | PoCで日本語検索品質確認後に確定 |
| 永続キャッシュアクセス | Microsoft.Data.Sqlite + 明示SQL | PoC後に確定 |
| 人の認証 | セッションCookie | 確定案 |
| AI認証 | スコープ付きBearer APIトークン | 確定案 |
| 通信 | Kestrel HTTPS / TLS 1.2以上 | 確定案 |
| バックアップ | NTFS上の世代管理バックアップ | 確定案 |
| Webテスト | Vitest / Testing Library / Playwright | 確定案 |
| Serverテスト | xUnit + ASP.NET Core TestServer | 確定案 |
| UI回帰 | Playwright screenshot + WebView2実機確認 | 確定案 |
| ログ | Microsoft.Extensions.Logging + 構造化JSONログ | 確定案 |

ライブラリの正確なバージョンはPhase 0開始時にサポート状況と脆弱性を確認し、ロックファイルで固定する。メジャーバージョンを自動追従しない。

## ADR-001 独立Serverプロセス

### 決定

ASP.NET Core ServerをWPFから独立したプロセスとして配置する。

### 理由

- WPFを閉じてもLANブラウザから利用できる。
- Server障害とWPF障害を分離できる。
- 将来、Server専用PCへ移しやすい。
- すべての業務操作をAPIへ統一できる。

### 不採用案

- WPF内蔵Server: 起動状態がWPFに依存する。
- Webのみ: Windows固有操作と導入案内が弱くなる。

### 見直し条件

単一PC専用製品へ要求が変更された場合。

## ADR-002 .NET 10 LTS

### 決定

ServerとWPFは.NET 10 LTSを基準とする。

### 理由

設計基準日時点で.NET 10は現行LTSであり、.NET 8はサポート終了が近い。長期開発と保守期間を考慮すると、新規実装を.NET 8で開始する利点が小さい。

### 代替案

.NET 8 LTS。既存資産互換が必要な場合だけ再検討する。

### 見直し条件

利用予定ライブラリまたは配布先Windows環境が.NET 10を正式に利用できないPoC結果となった場合。

## ADR-003 共通Web UIとReact候補

### 決定

WPF WebView2と通常ブラウザで同一のWeb UIビルド成果物を使用する。React＋TypeScript＋Viteを第一候補とするが、Phase 0 PoC合格までは正式採用しない。

### 採用判定条件

- 編集可能なテスト表がWebView2とブラウザで同等に動く。
- キーボード、フォーカス、スクロール、IME入力が安定する。
- ライト／ダーク、100～200% DPI、狭幅でVol.5を満たす。
- 認証Cookie、アップロード、ダウンロード、競合表示が成立する。
- 保守に必要な依存パッケージ数と更新負担が許容範囲である。

### 代替案

Blazor WebAssembly。C#統一の利点はあるが、テーブル部品、初期ロード、ブラウザ試験、WebView2挙動を同じPoC条件で比較する。

### 見直し条件

React PoCが合格基準を満たさない場合、または保守要員の技術制約が確定した場合。

## ADR-004 Markdown・添付・`.adm-meta`を正本とする

### 決定

- 業務文書とテスト結果: Markdown
- バイナリ: 添付ファイル
- 既存文書の補助情報と利用者固有状態: `.adm-meta`
- SQLite: 再構築可能な索引キャッシュ

### 理由

- 人とAIが同じ形式を読める。
- Git差分、手動閲覧、外部ツールとの互換性を維持できる。
- 既存原本を無断変更しない。
- キャッシュ破損から復旧できる。

### 不採用案

- DBのみを正本にする。
- 既存MarkdownへFront Matterを一括追記する。
- 確認状態を再構築不能なSQLiteだけに保存する。

## ADR-005 テスト結果を別Markdownへ保存

### 決定

TestCaseとTestResultを分離し、実施ごとに`execution_id`を持たせる。

### 理由

- 同じケースを複数回・複数環境で実行できる。
- ケース定義と実施履歴の差分が混ざらない。
- AI生成したケースを保ったまま人の結果を蓄積できる。

## ADR-006 ULIDと人向け連番の併用

### 決定

参照用の不変ULIDと、画面・会話用の連番IDを併用する。

### 理由

ULIDは改名や統合に強く、連番は人が読み上げやすい。片方だけでは両方の要件を満たしにくい。

## ADR-007 HTTPSと小規模LAN認証

### 決定

LAN内でもHTTPSを必須とする。人はCookie、AIはスコープ付きAPIトークンを使う。

### 初期設定方針

1. localhost限定で初回起動する。
2. 管理者を作成する。
3. 利用するLANアドレスとServer名を選択する。
4. ローカル認証局とServer証明書を生成する。
5. クライアント信頼用証明書と案内ファイルを出力する。
6. クライアント側で内容を確認して信頼設定する。
7. 接続試験後にLAN待受を有効にする。
8. 必要なFirewall規則だけを確認付きで追加する。

証明書の自動インストール範囲と管理者権限の扱いはPoCで決める。

## ADR-008 楽観的排他とETag

### 決定

長時間ロックを行わず、読込版を表すETagと`If-Match`で競合を検知する。

### 理由

LAN切断や画面放置でロックが残らず、古い画面からの無断上書きを防止できる。

## ADR-009 SQLite FTS5を索引候補とする

### 決定

SQLite FTS5をMVPの索引候補とし、Phase 0で日本語検索品質、再構築時間、1万文書性能を検証する。

### 代替案

- メモリ索引: 実装は単純だが再起動、メモリ量、複数要求への対応が弱い。
- 専用検索Server: MVPには過剰。

### 見直し条件

標準tokenizerで日本語要求を満たせず、軽量な分かち書き方式も運用負担に見合わない場合。

## ADR-010 WPFブリッジの制限

### 決定

WPFブリッジはファイル／フォルダー選択、Explorer起動、OS通知、Server制御、アプリ設定だけに限定する。

Markdown、テスト結果、添付、状態の操作はすべてAPIを経由する。

## ADR-011 MVPでは汎用拡張基盤を作らない

### 決定

責務境界と内部インターフェースは設けるが、外部プラグインSDK、マーケットプレイス、動的ロードは作らない。

### 理由

利用実績のない拡張点を先に固定すると、MVPの複雑性と保守負担だけが増えるため。

## ADR-012 Server起動方式とWindows依存の隔離

### 決定

正式運用はWindows Serviceとする。同じASP.NET Core Hostを、開発用コンソール、管理者による手動起動、任意のトレイ起動でも利用できる構成を維持する。

業務ロジック、API、Web UIはWindows固有APIへ直接依存させない。Service制御、Explorer、Firewall、証明書ストア、WPFはAdapter層へ隔離する。

### 理由

常時LAN利用、デバッグ容易性、将来の配置変更を両立し、起動方式ごとの実装分岐を防ぐため。

### P0-002検証結果

- `poc/hosting-modes`で同一ASP.NET Core Hostを4モードから再利用できた。
- `Adm.Core`は`Adm.Infrastructure.Windows`を参照せず、Windows ServiceパッケージはAdapter側だけが参照する。
- Windows Service相当モードでは`AddWindowsService`によるService設定境界を確認した。実Service登録・権限差・インストーラーは本PoC対象外とした。
- 同一ポートで二重起動するとKestrelが`address already in use`を明示して終了する。

## ADR-013 大容量添付アップロード

### 決定

初期上限を1ファイル500 MiB、1回合計1 GiBとする。アップロードは進捗、取消、具体的な失敗理由、同じ入力からの再試行を提供する。

### 理由

動画やログの送信時間が長くなるため、無反応に見える操作と入力やり直しを防ぐ必要がある。

## ADR-014 大容量添付バックアップの重複抑制

### 決定

Phase 0で内容ハッシュによる重複検知、参照管理、NTFS機能の利用を比較する。正本の単純性、復元の自己完結性、参照破損時の回復性を満たす方式だけを採用する。

### 理由

動画を世代ごとに複製すると容量上限へ早く到達するが、重複排除がバックアップの独立復元性を損なう危険もあるため。

## 参考にした公式情報

- .NET Support Policy: https://dotnet.microsoft.com/en-us/platform/support/policy
- Kestrel HTTPS endpoints: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-10.0
- WebView2 development practices: https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/developer-guide
- React releases: https://react.dev/versions
- Vite guide: https://vite.dev/guide/
- SQLite FTS5: https://www.sqlite.org/fts5.html
