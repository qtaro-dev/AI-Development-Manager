# AI Development Manager 採用技術案・技術ADR

版: 0.9-p0-023-phase0-gate
基準日: 2026-08-02

状態: P0-023 Phase 0設計確定ゲート完了、条件付き採用と保留事項は`29_PHASE0_DESIGN_GATE_DECISION.md`を参照

## 1. 採用技術の確定案

### P1-001ツールチェーン確定

製品コードはルート`global.json`の.NET SDK 10.0.302を`rollForward: disable`で使用し、`Directory.Build.props`から`net10.0`、Nullable、ImplicitUsings、分析器、警告エラー化、Version、Build番号、共通出力先を継承する。NuGetは`Directory.Packages.props`で中央管理し、Node.jsは`.node-version`の22.18.0を基準に、製品Web依存のlockfileを各Webチケットで固定する。P1-001では製品プロジェクトを作成せず、PoCコードも参照しない。

| 領域 | 採用案 | 状態 |
|---|---|---|
| Server | .NET 10 LTS / ASP.NET Core 10 / Kestrel | 確定 |
| .NET SDK | 10.0.302 / ルート`global.json`で固定 | 確定 |
| Windows Client | .NET 10 WPF | 確定案 |
| 埋込Web | Microsoft Edge WebView2 Evergreen Runtime | 確定案 |
| Web UI | React 19 + TypeScript + Vite | Phase 0 PoC後に確定 |
| Web UI代替 | Blazor WebAssembly | React PoC不合格時の比較対象 |
| API | REST / JSON / OpenAPI 3.x | 確定案 |
| Markdown | CommonMark系Markdown + YAML Front Matter | 確定案 |
| .NET Markdown解析 | Markdig 0.41.3 | P0-005採用候補 |
| YAML解析 | YamlDotNet 16.3.0のノード解析 | P0-005採用候補 |
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

### P1-005 CI品質ゲート

GitHub ActionsのWindows runner上で、リポジトリ直下の`global.json`と`.node-version`を使用して品質ゲートを実行する。ワークフロー固有の処理を増やさず、ローカルでも再現できる`scripts/ci/Invoke-QualityGates.ps1`を正本とする。restore、Debug／Release build・test、P1-003 Architecture検査、NuGetおよび導入後のnpm監査を必須工程とし、High／Critical脆弱性、失敗したテスト、参照境界違反、禁止された追跡ファイルを合否判定へ反映する。ビルド単位でログ、TRX、依存・ライセンス一覧、CycloneDX SBOMを保存し、失敗時もGitHub Actionsアーティファクトを保持する。

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

.NETコードを伴うPhase 0 PoCは、リポジトリ直下の`global.json`で.NET 10 SDK `10.0.302`を`rollForward: disable`として固定する。各PoC結果へ固定値と`dotnet --version`の実測値を記録する。固定SDKがない環境では.NET PoCを開始しない。

### 理由

設計基準日時点で.NET 10は現行LTSであり、.NET 8はサポート終了が近い。長期開発と保守期間を考慮すると、新規実装を.NET 8で開始する利点が小さい。

### 代替案

.NET 8 LTS。既存資産互換が必要な場合だけ再検討する。

### 見直し条件

利用予定ライブラリまたは配布先Windows環境が.NET 10を正式に利用できないPoC結果となった場合。

SDK servicing版を更新する場合は、`global.json`、リポジトリ規約、以後のPoC結果基準を同じチケットで更新する。完了済みPoCは、その変更だけを理由に再実施しない。

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

### P1-013実装結果

`src/Adm.Web`へReact `19.2.8`、TypeScript `6.0.3`、Vite `8.2.0`の製品基盤を追加した。依存は`package-lock.json`へ固定し、React PoCのコード・依存・モック画面は参照していない。TypeScript strict、ESLint、Prettier、公開環境値の型付き境界、API client差込境界、bundle内の秘密・ローカル絶対パス検査を整備した。Edge／Chrome／WebView2、IME、DPI、正式UIは後続チケットで確認する。

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

### P0-016検証結果

ローカル認証局5年、Server証明書397日、Server名・LANアドレスを含むSAN、秘密鍵を含まないクライアント信頼用公開証明書、期限切れ拒否、アドレス変更時の再発行、カスタムルート信頼を確認した。Firewall規則変更、証明書ストア登録、UAC昇格は自動実行せず、確認付き手順とロールバックへ分離した。別Windows 11 PCからの実LAN接続と実ストア権限は追加の実機確認事項とする。

### P0-017検証結果

人向けセッションCookieは`Secure`、`HttpOnly`、`SameSite=Strict`とし、書込み時のCSRFトークンを検証した。ブラウザとWebView2は同じCookieフローを利用する前提を確認した。AIトークンはハッシュ保存・発行時のみ表示・プロジェクト単位・読み取り専用を初期値とし、失効・期限切れ・範囲外を拒否した。401と403を分離し、監査イベントへ秘密値を出力しないことを確認した。

### P0-018検証結果

`poc/web-ui-react`でReact + TypeScript + Viteを検証し、チケット一覧・詳細、編集表、保存状態、409競合表示、テーマ切替、検索入力、キーボード操作、820px幅のレスポンシブ表示を確認した。P0-015検索方式やP0-017認証実装へ密結合しないモックAPI境界を採用した。Reactを第一採用候補とするが、Edge／Chrome／WebView2、実IME、100～200% DPIの実機確認後に正式確定する。

### P0-019検証結果

大容量添付は64KiBチャンクのストリーミング、一時ファイル確定、取消・通信断時の清掃、同一ID再試行、複数アップロード、Range応答、形式別閲覧モード、ログプレビュー制限を確認した。1ファイル500MiB、バッチ1GiBの境界値を許可し、超過を拒否する。実機の容量枯渇、ブラウザ／WebView2差、動画コーデック、分割アップロード正式採用は保留する。

## ADR-008 楽観的排他とETag

### 決定

長時間ロックを行わず、読込版を表すETagと`If-Match`で競合を検知する。

### 理由

LAN切断や画面放置でロックが残らず、古い画面からの無断上書きを防止できる。

## ADR-009 SQLite FTS5を索引候補とする

### 決定

SQLite FTS5をMVPの索引候補とし、Phase 0で日本語検索品質、再構築時間、1万文書性能を検証する。

P0-014では`unicode61`と`trigram`の候補比較に合格した。`unicode61`は日本語フレーズと英数検索、`trigram`は日本語・エラーコードの部分一致に適する候補と確認した。P0-015の測定は完了したが、初回走査・検索・依存安全性の未達を受けて現方式の正式採用は保留する。P0-024～P0-026の結果を含め、P0-023で正式判断する。

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

### P1-006実装結果

`Adm.Server.Host.ServerHostFactory`を共通Host生成元として実装し、コンソールエントリーポイントから再利用する。KestrelはIPv4 loopback（`127.0.0.1`）だけを待ち受け、ルートの基盤確認応答以外の業務APIは追加していない。実Kestrelのlocalhost接続、正常停止、同一ポート二重起動失敗を統合テストで確認した。LAN待受、HTTPS、認証、Windows Service登録は後続チケットへ分離した。

### P1-007実装結果

`ServerOptions`と`SecretReferenceOptions`を`ServerConfiguration`で登録し、JSON、環境変数、コマンドラインの最終値を型付きOptionsへバインドする。`ValidateOnStart`でloopback限定、ポート範囲、秘密値の直接指定を検査し、不正時は起動を拒否する。設定カタログは既定値、変更可否、再起動要否、秘密参照区分を持ち、秘密値そのものを含めない。P1-008の構造化ログやP1-010のProblem Detailsは追加していない。

### P1-008実装結果

`AdmJsonLoggerProvider`をMicrosoft.Extensions.LoggingのProviderとして追加し、1行1JSONの診断ログをConsoleへ出力する。`X-Request-Id`を検証して要求・応答・ログScopeを相関させ、未指定または不正時は新しいIDを生成する。秘密キー、Bearerトークン、機密QueryStringをマスキングし、例外型以外の内部詳細をログへ出さない。業務監査ログ、外部収集、ファイルローテーションは対象外とした。

### P1-012実装結果

`Adm.Infrastructure.Windows.Hosting.WindowsServiceHostAdapter`へWindows Service lifetime、Service名、30秒の停止タイムアウト、`console`／`manual`／`service`／`tray`の起動モード解決を隔離した。`Adm.Server.Host.Program`は同じ`ServerHostFactory`へAdapter設定だけを注入し、Health・API・業務ロジックの起動方式別複製を行わない。Service実登録、権限設定、Firewall、インストーラー、トレイUIは対象外とした。

## ADR-015 Markdown・Front Matter解析境界

### 決定

Markdig `0.41.3`をMarkdown本文・見出し・GFM表の解析候補、YamlDotNet `16.3.0`をFront Matterのノード解析候補とする。YamlDotNetは任意.NET型を生成せず、`YamlMappingNode`等のノードを安全に走査する。解析結果は本文、抽出値、警告、致命的エラーを分離し、入力は変更しない。

### P0-005検証結果

P0-004の全18fixtureで、期待文書種別と期待警告、見出し、表、Front Matter添付参照を検証できた。壊れたYAMLは文書単位の致命的エラーとして隔離し、Front Matterなしでも本文を保持した。同一入力の再実行結果と入力ハッシュは一致した。

### 見直し条件

実データ互換性、Markdown方言、巨大セル正式上限、ライブラリ脆弱性・保守状況の後続PoC結果が採用条件を満たさない場合。

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

### P0-020検証結果

`poc/backup-dedup`で、同一500 MiB添付を20世代保存する論理比較、同名別内容・別名同内容、SHA-256＋バイト長による識別、manifest/blobの別環境復元、復元前退避、参照欠落・blob破損・manifest破損・途中停止、30日保持・最低20世代・50 GiB上限・80%警告を確認した。全13項目に合格した。

単純コピーの論理10,000 MiBに対して重複抑制は500 MiB相当となる。正式採用、容量値、保持優先順位はP0-023で確定し、P0-020では製品コードへ昇格させない。

## ADR-017 FTS5索引構成の比較結果

P0-025でunicode61 external-content、パス・ファイル名・見出し限定trigram、全本文trigramを比較した。全構成で更新・改名・削除・再構築と整合性検査に合格したが、unicode61の標準日本語検索p95は1,063.640ms、全本文trigramは2,827.656msで暫定基準を満たさなかった。またunicode61は一部日本語中ヒット語の正解集合を満たさなかった。現時点ではunicode61を基礎候補、限定trigramをフォールバック候補として保持し、正式採用はP0-023およびP0-026の結果後に判断する。

## ADR-018 SQLite依存の明示固定候補

P0-026で、現行`Microsoft.Data.Sqlite 10.0.10`の推移依存`SQLitePCLRaw.lib.e_sqlite3 2.1.11`がNU1903 Highとなることを確認した。`Microsoft.Data.Sqlite.Core 10.0.10`と`SQLitePCLRaw.bundle_e_sqlite3 3.0.3`を明示参照するCandidateは、NU1903なし、SQLite 3.50.4、FTS5・unicode61・trigram回帰合格、win-x64 native DLLロード合格だった。Candidateを第一採用候補として保留し、依存安全性・配布・正式採用はP0-023で確定する。

## ADR-016 走査パイプラインの工程分解と増分差分

### 決定

走査を列挙・属性取得・メタデータ読取・キャッシュ差分・候補抽出・必要時の本文／ハッシュ／解析・後段引渡しへ分離する。通常の再走査では属性が変わらない文書の本文読取とフルハッシュを省略し、追加・変更・削除・改名を差分として扱う。FileSystemWatcherだけを正とせず、全走査・定期再走査・手動再走査で整合性を補完する。

### P0-024結果

10,000件の合成コーパスで初回p95 2,083.535ms、変更なしp95 117.721ms、単一変更p95 128.989ms、ピーク約107.8MiBを記録した。取消、読取失敗、同一属性変更時の強制ハッシュを含む検査に合格した。実データ、UNC/NAS、実機UI非ブロック性は未確認であり、正式採用はP0-023で判断する。

## 参考にした公式情報

- .NET Support Policy: https://dotnet.microsoft.com/en-us/platform/support/policy
- Kestrel HTTPS endpoints: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-10.0
- WebView2 development practices: https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/developer-guide
- React releases: https://react.dev/versions
- Vite guide: https://vite.dev/guide/
- SQLite FTS5: https://www.sqlite.org/fts5.html
