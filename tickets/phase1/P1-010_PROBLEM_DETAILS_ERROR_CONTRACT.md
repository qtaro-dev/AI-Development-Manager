# P1-010 共通エラー応答

## 目的

API失敗時の内部エラー、HTTP状態、利用者向け日本語、次の操作、追跡IDを統一したProblem Details契約を実装する。

## 背景

専門用語や内部例外を画面へ露出せず、利用者が次に何をすべきか理解できる必要がある。Phase 2以降の全APIが共通利用する基盤とする。

## 前提・依存関係

- P1-008完了
- P1-009完了
- `design/01_INTEGRATED_BASIC_DESIGN.md`第7・15節

## 対象範囲

- RFC 9457系Problem Details形式
- 内部コード、利用者向けメッセージキー、追跡ID
- 入力保持可否、再試行可否、次の操作
- 予期済み・予期しない例外の変換

## 対象外

- 409競合の業務詳細
- 認証401/403の最終文言
- UIトースト実装

## 対象ファイルまたは対象モジュール

- `src/Adm.Application/Errors`
- `src/Adm.Server.Host/Errors`
- `tests/Adm.Server.IntegrationTests`

## 具体的な実装内容

1. 共通Problem Details DTOと内部エラー分類を作る。
2. 例外をHTTP状態と安全な日本語メッセージキーへ写像する。
3. 追跡ID、入力保持、再試行、次の操作を拡張項目にする。
4. 未処理例外を500へ変換し、Stack Traceを返さない。
5. Web UIが機械判定できる安定コードを定義する。

## テスト内容

- validation、not found、conflict予約、forbidden予約、500変換
- 追跡IDのログ一致
- 内部例外・パス・秘密値の非露出
- Content-TypeとOpenAPI契約
- 未知エラーコードの安全なフォールバック

## 完了条件

- すべてのAPIエラーが共通形式になる。
- 利用者向けに「何が起きたか」「入力が残るか」「次の操作」が示される。
- 内部例外と秘密情報が応答へ出ない。
- OpenAPIと統合テストが契約を固定する。

## ユーザーが目視確認する内容

- 正常な日本語エラー表示用データ
- 追跡IDと次の操作
- 内部詳細が隠される例

## 想定されるリスク

- 内部コードを専門用語のままUIへ表示する
- 例外を一律500にして利用者の修正可能性を失う
- メッセージ直書きで辞書とずれる

## 完了後に更新すべき設計資料

- APIエラー契約
- `design/01_INTEGRATED_BASIC_DESIGN.md`第7節
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

## 実装結果

- `Adm.Application.Errors`へ内部エラー分類と固定された安全なエラー定義を追加した。
- `ErrorHandlingMiddleware`でvalidation、not found、conflict、forbidden、unexpectedを共通Problem Detailsへ変換した。
- `application/problem+json`、安定コード、messageKey、入力保持、再試行可否、次の操作、追跡IDを返すようにした。
- `X-Request-Id`応答値とProblem Detailsの`traceId`を一致させた。
- 未処理例外は500へ変換し、例外本文、Stack Trace、秘密値を返さない。
- P1-011以降の認証、Health、UIトースト、409業務詳細は実装していない。

## 検証結果

使用SDK: `10.0.302`（`global.json`固定値と`dotnet --version`実測値が一致）。

実行したコマンド:

```powershell
dotnet build .\AIDevelopmentManager.sln --configuration Debug
dotnet test .\tests\Adm.Server.IntegrationTests\Adm.Server.IntegrationTests.csproj --configuration Debug --no-build --no-restore
pwsh -NoProfile -File .\scripts\api\Validate-OpenApiContract.ps1
```

統合テストではvalidation、404、未処理例外、Content-Type、追跡ID相関、内部例外本文・型の非露出を確認した。

P1-010は完了状態とし、P1-011以降は対象外とする。
