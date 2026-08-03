# P1-035 Local／HTTP契約一致テスト

## 目的

同一のApplication処理をLocal Application ChannelとHTTP APIのどちらから呼び出しても、製品上の成功結果と安全なエラー意味が一致することを契約テストで保証する。

## 背景

Local modeを主経路、Server/APIを追加機能としても、UIやApplicationの意味を経路ごとに分岐させてはならない。一方、Channel EnvelopeとHTTP Status等のTransport固有表現まで同一にする必要はない。本チケットで比較境界を明文化し、今後の業務Use Caseが同じ規則に従える基準を作る。

## 前提・依存関係

- P1-034完了・承認済み
- P1-009、P1-010完了済み
- P1-031～P1-033完了・承認済み
- `design/33_API_OPENAPI_CONTRACT.md`
- `design/34_API_ERROR_CONTRACT.md`

## 対象範囲

- P1-034の最小Application処理を用いたLocal／HTTP比較
- 成功値、エラーコード、利用者向けメッセージキーの正規化規則
- Local ErrorとProblem Detailsの対応表
- 共通fixtureと契約一致テスト用Harness
- Server endpointが同じApplication処理を使うための最小配線
- 後続Use Case向け契約テスト雛形

## 対象外

- EnvelopeとHTTP Responseのバイト単位一致
- HTTP固有のStatus、Header、trace IDのLocal側への持込み
- Local固有のrequest IDの公開APIへの持込み
- 新しい業務APIや保存処理
- 認証、権限、HTTPS、LAN設定
- UI変更、性能最適化

## 対象ファイルまたは対象モジュール

- `src/Adm.Application/`
- `src/Adm.Server.Host/`の最小endpoint配線
- `src/Adm.Wpf/`のLocal Handler
- `src/Adm.Web/src/data-access/`
- `tests/Adm.Application.Tests/`
- API統合テスト、WPF Channelテスト、Web Adapterテスト
- `design/`のTransport契約一致資料

## 具体的な実装内容

1. Transport非依存のApplication結果とエラー分類を比較正本とする。
2. 成功、入力不正、未登録／未対応、内部失敗についてLocalとHTTPの対応表を作る。
3. 同じfixtureを両経路へ投入し、正規化後の結果を比較するHarnessを作る。
4. Serverの対象endpointがP1-034と同じApplication Use Caseを解決するよう最小限配線する。
5. Transport固有情報を比較対象外として明示し、今後の契約テスト雛形を残す。
6. API/OpenAPIに変更が生じた場合だけ正本と生成物を同期する。

## テスト内容

- 成功payloadの意味的一致
- 入力不正、未対応、内部失敗のコードとメッセージキー一致
- Localで例外本文が漏れず、HTTPでもProblem Detailsへ内部情報が漏れないこと
- 一方のAdapterだけ値を変更した場合に契約テストが失敗する自己検証
- OpenAPI回帰、Web Adapter回帰、Architecture検査
- Debug／Release Buildと全関連テスト

## 成功条件

- 比較対象とTransport固有の対象外項目が一意に定義されている。
- 共通fixtureでLocal／HTTPの成功・失敗結果が一致する。
- 両経路が同じApplication Use Caseを利用する。
- 後続Use Caseが再利用できる契約テスト雛形がある。

## 完了条件

- 契約表、実装、テスト、設計更新が完了している。
- OpenAPI差分の有無を確認し、差分がある場合は承認可能な形で記録している。
- P1-036以降を実装していない。

## ユーザーが目視確認する内容

- Local／HTTPの結果比較表
- 成功と代表エラーのテスト結果
- Transport固有項目が無理に共通化されていないこと

## 想定されるリスク

- 表面的なJSON一致を優先してTransport責務を混ぜる。
- ServerとWPFでApplication処理を複製する。
- 内部例外文字列を一致比較に使い、情報漏えいを固定化する。
- テストHarnessが実経路を通らず、偽陽性になる。

## 完了後に更新すべき設計資料

- `design/00_INDEX.md`
- `design/30_PHASE1_IMPLEMENTATION_PLAN.md`
- `design/33_API_OPENAPI_CONTRACT.md`
- `design/34_API_ERROR_CONTRACT.md`
- P1-035 Transport契約一致資料
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`
- 本チケット

## 完了時に残す証拠

- Local／HTTP対応表
- 共通fixture一覧と両経路の実行結果
- OpenAPI差分確認
- Build、Test、Architecture検査結果
- `dotnet --version`と`git diff --check`結果

## 実施結果

- Local／HTTPの意味比較境界を設計資料へ固定した。
- Server `/api/v1/version`とWPF Local Composition Rootが同じ`GetFoundationStatusUseCase`を利用するようDI配線した。
- 成功値、入力不正、未対応、内部失敗の正規化対応表と共通fixtureを追加した。
- Local／HTTPの結果比較、変更値検出、OpenAPI回帰、内部情報非露出を検証した。
- OpenAPI path/schemaは変更していない。P1-036以降は着手していない。
