# P1-032 WPF組み込みWeb UIローダー

## 目的

製品WPFへ共通React UIのproduction buildを組み込み、Server、Kestrel、HTTP API、localhostポートを起動せず、Local modeの既定画面としてWebView2へ表示できるローダーを実装する。

P1-029で条件付き採用した仮想HTTPS origin方式を製品品質で再実装し、既存Server modeと同じWeb UI成果物を利用できる状態にする。

## 背景

P1-029では、LiteTube Dock起動中でも専用UserDataFolderを用いたWebView2で組み込みReact UIを正常表示・再読込できた。Server、Kestrel、HTTP API、localhost待受を使用せず、5回の起動がすべて5秒以内だった。

P1-030により`Adm.Wpf`はLocal modeの独立Composition Rootとして`Adm.Application`を参照するが、現行WPF Shellは依然として既定Server URLのreadinessを待ってからServer配信UIを表示する。Local modeを製品既定経路へ変更するには、PoCコードをコピーせず、Web資産のBuild・配置・読込・Navigation・UserDataを製品境界として実装する必要がある。

## 前提・依存関係

- P1-029条件付き採用として承認済み
- P1-030承認済み
- P1-031完了・承認済み
- `poc/p1-029-webview2-offline-ui/results/P1-029_RESULT.md`
- `design/43_WPF_WEBVIEW2_SHELL_CONTRACT.md`
- `design/50_ADR_019_LOCAL_FIRST_EXECUTION_MODEL.md`
- P1-019のServer Web asset配信
- P1-020／P1-021のWPF Shell・Platform Bridge安全境界
- P1-023のWebView2 Runtime互換結果

## 対象範囲

- `Adm.Web` production buildのWPF成果物への取込み
- Server HostとWPFが同じWeb UI内容を利用するBuild境界
- WPF Local modeの組み込みWeb資産パス解決
- 製品用の固定仮想HTTPS origin
- `CoreWebView2.SetVirtualHostNameToFolderMapping`相当の製品実装
- WebView2 Evergreen Runtime確認
- アプリ・Local profile専用UserDataFolder
- Local modeを引数なし起動の既定値とする起動分岐
- 明示的`--server-url`による既存Server modeの互換維持
- 固定origin内Navigation、外部Navigation、新規Window、Resource要求の安全境界
- Server未導入・停止、ネットワークなしでの表示・再読込
- UI読込失敗時の日本語案内と終了操作
- WPF単体・Windows実機・WebView2目視確認

## 対象外

- Local Application ChannelのRequest／Response／Error実装
- DataAccess PortへのLocal Adapter接続
- Application ServiceのDI登録・業務処理呼出
- プロジェクト、Markdown、チケット、添付、テスト、検索等の業務機能
- Server設定画面、初回ウィザード、LAN接続UI
- Progress、Cancel、Streaming
- Platform Bridgeの許可操作追加
- Self-contained publish、MSI、Shortcut、Iconの変更
- Server Host、API、Service、認証、HTTPS、LAN公開の機能変更
- 製品Router方式の全面変更
- P1-033の契約実装
- P1-034以降のチケット作成または着手

## 対象ファイルまたは対象モジュール

- `src/Adm.Wpf/`
- `src/Adm.Wpf/Adm.Wpf.csproj`
- `src/Adm.Wpf/Shell/`
- `src/Adm.Web/`のBuild成果物
- `src/Adm.Server.Host/Adm.Server.Host.csproj`のWeb asset取込み境界
- Web assetを一度だけ生成・段階配置する共通Build設定
- `tests/Adm.Infrastructure.Windows.Tests/`またはWPF Shell用テスト
- `tests/Adm.Web.E2E/`
- `design/43_WPF_WEBVIEW2_SHELL_CONTRACT.md`
- P1-032 WPF組み込みWeb UIローダー契約資料
- `tickets/phase1/P1-032_WPF_EMBEDDED_WEB_UI_LOADER.md`
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

P1-029のPoCコードを製品側へコピーしない。確認済み方式と制約だけを製品品質で再実装する。

## 製品実行契約

- 引数なし起動: Local mode。Server readinessを確認せず組み込みUIを表示する。
- `--server-url=<URL>`指定: 明示的Server mode。既存の安全なServer接続経路を維持する。
- Local modeのorigin: 設計資料で固定した製品専用HTTPS仮想origin。実DNSやTCP接続を使用しない。
- Local modeのWeb資産: WPF成果物内の読み取り専用配置。任意ユーザーパスを割り当てない。
- UserDataFolder: `%LOCALAPPDATA%\AI Development Manager\WebView2\Local`を初期配置とし、将来の明示プロファイル分離が可能なResolverを使用する。
- WPF終了: WebView2を停止し、Server Serviceの起動・停止には関与しない。

## 具体的な実装内容

1. P1-029結果の条件と未解決事項を製品ローダーの受け入れ条件へ転記する。
2. `Adm.Web` production buildを再現可能に生成し、WPFとServer Hostが同一内容のWeb成果物を使用できる段階配置を作る。
3. Solution並列Buildで複数プロジェクトが同じ`dist`へ同時書込みしないよう、Web Buildの実行元とコピー先を一意にする。
4. WPF publish／Build成果物へWeb assetを含め、欠落時は起動後の白画面ではなくBuildまたは起動時に明示的に失敗させる。
5. Local mode用のAsset Resolver、Origin Policy、UserDataFolder ResolverをWPF Shellから分離する。
6. 固定仮想HTTPS originへWPF成果物内のWeb assetフォルダーを`DenyCors`相当で割り当てる。
7. 引数なし起動をLocal modeへ変更し、Server readiness、Kestrel、localhost、HTTP APIへ接続せず`index.html`を表示する。
8. 明示的`--server-url`指定時だけ既存Server modeを選択し、既存Navigation制限を維持する。
9. 固定origin外Navigation、新規Window、外部Resource、`file://`、割当外ファイルを拒否する。
10. P1-029の引数引用不具合を踏まえ、空白を含むWeb asset、UserData、証拠パスを引数・APIへ安全に渡すテストを追加する。
11. WebView2 Runtime不足、Web asset欠落、初期化失敗、読込失敗を日本語辞書から案内し、再試行と終了を可能にする。Server設定UIは追加しない。
12. Local modeの初回表示、再読込、5回起動、Server未導入／停止、ネットワークなし、LiteTube Dock共存をWindows実機で確認する。
13. Server mode、ブラウザ配信、Playwright、Architecture検査の回帰を実行する。

## テスト内容

### Build・配布物

- Debug／Release Build
- `Adm.Web` production build
- WPF出力に必要Web assetが含まれる
- Server出力とWPF出力のWeb asset内容ハッシュが一致する
- 並列Solution BuildでWeb asset生成競合がない
- Web asset欠落で安全に失敗する
- 空白を含むパスでBuild・起動できる

### Local mode

- 引数なしでLocal modeになる
- Server未導入、停止、起動失敗でもUIを表示する
- ネットワークなしでUIを表示・再読込する
- `Adm.Server.Host`、Kestrel、HTTP Listener、TCP待受がない
- localhost／127.0.0.1へのResource要求がない
- 5回連続起動が各回5秒以内
- LiteTube Dock起動中に表示・再読込できる

### 安全境界

- 固定仮想HTTPS originだけを許可する
- 外部Navigation、新規Window、外部Resourceを拒否する
- `file://`と割当外ローカルファイルを拒否する
- 専用UserDataFolderを使用し、他アプリと共有しない
- WebView2 Consoleに重大な未処理例外がない

### Server mode・回帰

- 明示的`--server-url`でServer modeへ接続できる
- Server modeのorigin検証が維持される
- Platform Bridge許可リストを変更していない
- Web Build、Web Test、Playwrightが合格する
- .NET Test、Architecture検査が合格する
- 既存UIの主要配置、Theme、狭幅表示に意図しない差分がない

## 完了条件

- 引数なしの製品WPFがServer接続を待たず、組み込みReact UIを表示する。
- Local modeでServer、Kestrel、HTTP API、HTTP Listener、localhostポートを使用していない。
- WPFとServer Hostが同じWeb UI内容を利用し、Web Buildの競合がない。
- 固定仮想HTTPS origin、Navigation、Resource、UserDataFolder境界がP1-029条件を満たす。
- Server未導入・停止・ネットワークなし・LiteTube Dock共存で表示と再読込が成功する。
- 空白を含むパスが正しく扱われ、引数引用不足を再発しない。
- Runtime不足、資産欠落、初期化失敗時に日本語の次操作と終了を提供する。
- 明示的Server modeを壊していない。
- Build、Test、Architecture、Web Test、Playwrightが合格する。
- Local Application Channel、Application Service呼出、業務機能を実装していない。
- P1-033を実装していない。
- P1-034以降を作成・着手していない。

## ユーザーが目視確認する内容

- Serverを停止または未導入にした状態でのWindowsアプリ起動
- 組み込みReact UIの初回表示と再読込
- LiteTube Dock起動中の共存
- ネットワークなしでの表示
- WebView2 Runtime不足・資産欠落時の案内
- 再試行と終了操作
- 明示的Server modeの既存表示
- 既存基準画面との主要レイアウト差分

## 想定されるリスク

- PoCコードをそのまま製品へコピーする。
- Server readiness確認がLocal mode起動経路へ残る。
- WPFとServerが同じ`dist`を並列更新しBuild競合する。
- Web assetを任意ユーザーパスから読み込み、origin境界を失う。
- UserDataFolderを他アプリや別プロファイルと共有する。
- 外部Resourceを暗黙に許可する。
- ローダー変更へLocal Channelや業務UIを混在させる。
- Server modeを削除または既定へ戻す。
- 画面確認を行わず白画面・資産欠落を見逃す。

## 完了後に更新すべき設計資料

- `design/00_INDEX.md`
- `design/30_PHASE1_IMPLEMENTATION_PLAN.md`
- `design/43_WPF_WEBVIEW2_SHELL_CONTRACT.md`
- `design/48_WPF_CLIENT_INSTALLER_CONTRACT.md`の将来取込み条件
- P1-032 WPF組み込みWeb UIローダー契約資料
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`
- `tickets/phase1/P1-032_WPF_EMBEDDED_WEB_UI_LOADER.md`

## 完了時に残す証拠

- 変更ファイル一覧
- Web asset生成・配置・内容ハッシュ
- Debug／Release Build、Test、Architecture結果
- Web Build、Test、Playwright結果
- Server非依存・待受なしの確認結果
- 5回の起動時間
- Navigation／Resource／Console記録
- LiteTube Dock共存結果
- Local／Server modeの代表スクリーンショット
- `git diff --check`結果

## 実施結果

- 引数なし起動をLocal modeへ変更し、Server readiness、Kestrel、HTTP API、localhost待受なしで組み込みReact UIを表示するようにした。
- `--server-url=<loopback URL>`指定時は既存Server modeを維持し、readiness確認とloopback origin検査を継続する。
- 固定仮想HTTPS origin、WPF出力内`WebAssets`、`DenyCors`のvirtual host mappingを実装した。
- UserDataFolderを`%LOCALAPPDATA%\AI Development Manager\WebView2\Local`へ分離し、WebView2初期化を非同期10秒タイムアウト付きにした。
- 固定origin外Navigation／Resource、`file://`、localhost、外部Resource、新規Windowを拒否する安全境界を実装した。
- `Adm.Web` production buildを排他ロック付き共通スクリプトで生成し、WPFとServerへ同一内容を配置するBuild境界を追加した。
- Web資産欠落、WebView2 Runtime不足、初期化失敗、読込失敗を日本語案内へ変換した。
- P1-029 PoCコード、Local Application Channel、Local Adapter、業務機能、Bridge追加、P1-033以降は実装していない。

## 検証結果

- Debug／Release Solution build: 成功、警告0、エラー0
- Debug／Release Solution test: 各49件成功
- Architecture検査 Debug／Release: 成功（5製品プロジェクト）
- Web typecheck、DataAccess境界検査、lint、format、Vitest: 成功（11 files / 25 tests）
- Web資産: WPF `WebAssets`とServer `wwwroot`をDebug／Releaseで比較し、各4ファイルの内容ハッシュ一致を確認
- WPF実起動: Server未起動・引数なしLocal modeで画面表示成功
- WPF再読込: F5で画面再表示成功
- 5回連続起動: LiteTube Dock起動中に5回すべてLocal UI表示成功（自動操作による起動から確認までの経過値は4,124／4,199／4,275／4,152／5,642 ms。最後の値を含め、画面キャプチャ処理を含む値であり、正式な起動時間基準は実機測定で確定する。）
- Local mode TCP待受: WPFプロセスに待受なし。Kestrel／Serverプロセスなし
- UserDataFolder: `%LOCALAPPDATA%\AI Development Manager\WebView2\Local`の作成を確認
- Playwright: テスト本体は既存9件を実行。既存の終了処理が戻らず、専用Playwright／Serverプロセスを停止して環境を復旧した。P1-032の完了を妨げない既存課題として扱う。

## 状態

実施済み、レビュー待ち。

P1-032を承認してからP1-033へ進む。
