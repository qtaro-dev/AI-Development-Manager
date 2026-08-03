# P1-034 WPFローカルComposition Root

## 目的

WPFを主製品とするLocal modeで、組み込みWeb UIからLocal Application Channelを経由して`Adm.Application`の処理を呼び出せる製品実行経路を構成する。Server、Kestrel、HTTP、localhost待受がなくてもWPFが起動し、最小の製品情報要求へ応答できる状態を作る。

## 背景

P1-030でWPFとServerを独立Composition Rootとし、P1-031～P1-033でDataAccess Port、組み込みUIローダー、最小Channel契約を分離して実装した。本チケットでは新しい業務機能を追加せず、確定済みの境界を製品WPF内で初めて接続する。

## 前提・依存関係

- P1-028～P1-033完了・承認済み
- `design/50_ADR_019_LOCAL_FIRST_EXECUTION_MODEL.md`
- `design/51_P1_032_WPF_EMBEDDED_WEB_UI_LOADER.md`
- `design/52_P1_033_LOCAL_APPLICATION_CHANNEL_CONTRACT.md`

## 対象範囲

- WPF専用Composition Root
- `Adm.Application`の最小製品情報Use Case
- Local Channelの明示的Handler登録
- Local DataAccess AdapterからApplication処理までの接続
- 起動・終了時のDIライフサイクルと例外処理
- Server非依存を保証するArchitecture検査

## 対象外

- Markdown、プロジェクト、チケット、添付、テスト、検索の業務処理
- ファイル保存、SQLite、`.adm-meta`
- Serverの自動起動、探索、インストール
- HTTP API、Server Composition Rootの変更
- 実行プロファイル、設定画面、初回セットアップ
- Progress、Cancel、Streaming
- UIレイアウトや文言の変更

## 対象ファイルまたは対象モジュール

- `src/Adm.Application/`
- `src/Adm.Wpf/`のComposition Root、Local Channel登録、起動処理
- `src/Adm.Web/src/data-access/local/`
- `tests/Adm.Application.Tests/`
- `tests/Adm.Architecture.Tests/`
- WPF Channel用テスト
- `design/`のLocal Composition資料

## 具体的な実装内容

1. 製品名、製品版、実行モード等の非機密情報を返す最小Use Caseと結果型を`Adm.Application`へ定義する。
2. WPF Composition RootでUse Case、Local Channel Handler、関連Adapterを明示登録する。
3. P1-033の許可リストへ製品用operationを1件だけ登録し、任意型探索やReflectionによる自動公開は行わない。
4. Web側Local AdapterがDataAccess Portを満たし、Request／Response／Errorへ変換する。
5. WPF終了時に保留要求を安全に失敗させ、リソースを破棄する。
6. WPFから`Adm.Server.Host`、ASP.NET Core Host、HTTP Clientを経由しないことをArchitecture検査で固定する。

## テスト内容

- Use Caseの正常系と内部例外の秘匿
- Local AdapterからApplicationまでのRequest／Response往復
- 未登録operation、不正payload、重複ID、終了中要求の拒否
- Serverプロセスなし、ネットワーク切断状態でのWPF起動
- localhost待受とHTTP通信が発生しないこと
- Debug／Release Build、.NET Test、Web Test、Architecture検査

## 成功条件

- 組み込みUIから製品情報を取得できる。
- Server未導入・停止中でも同じ結果を得られる。
- Local経路が`Adm.Application`を利用し、Server Hostを参照しない。
- 製品Channel registryには承認したoperationだけが存在する。
- 既存Local／Server mode表示を壊さない。

## 完了条件

- 実装、設計更新、全対象テスト、実機スモークが完了している。
- WPF単体起動、要求往復、正常終了の再現手順と証拠が残っている。
- P1-035以降を実装していない。

## ユーザーが目視確認する内容

- Serverを停止した状態でWPFが組み込みUIを表示すること
- 製品情報要求が成功し、利用者向けエラーに内部情報が出ないこと
- WPF終了後にプロセスや待受ポートが残らないこと

## 想定されるリスク

- Composition Rootへ業務判断を置く。
- Handlerを自動登録して意図しない操作を公開する。
- WPFからServer実装を参照してローカル単体性を失う。
- UIスレッド上で同期的に処理し、停止や応答不能を招く。

## 完了後に更新すべき設計資料

- `design/00_INDEX.md`
- `design/30_PHASE1_IMPLEMENTATION_PLAN.md`
- `design/50_ADR_019_LOCAL_FIRST_EXECUTION_MODEL.md`
- P1-034 Local Composition資料
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`
- 本チケット

## 完了時に残す証拠

- 依存登録一覧と責務図
- Local要求往復ログまたは自動テスト結果
- Server非依存・待受なしの確認結果
- Build、Test、Architecture検査結果
- `dotnet --version`と`git diff --check`結果

## 実施結果

- `Adm.Application`へ`GetFoundationStatusUseCase`と`FoundationStatus`を追加した。
- WPF専用`LocalCompositionRoot`を構成し、`getFoundationStatus`だけを明示登録した。
- Local ChannelからApplication Use Caseへ同一プロセスで接続し、Server、Kestrel、HTTP、localhost待受へ依存しない。
- Web側へ`createLocalDataAccess`を追加し、固定Local originのWebView2 TransportからDataAccess Portへ接続した。
- 終了時CancellationTokenを伝播し、保留要求を`channel_unavailable`へ変換する。
- `getHostInfo`はPlatform Bridge専用のまま維持した。

検証結果は最終報告へ記録する。P1-035以降は着手していない。
