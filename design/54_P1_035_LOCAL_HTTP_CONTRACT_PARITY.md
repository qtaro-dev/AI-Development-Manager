# P1-035 Local／HTTP契約一致

## 比較境界

Local Application ChannelとHTTP APIはTransport形式を共有しない。共通の比較正本はApplication Use Caseが返す意味であり、成功時は次を比較する。

| 比較する | 内容 |
|---|---|
| 状態 | Local `result.state` とHTTP `status`を同じ意味へ正規化する |
| 契約版 | `contractVersion` |
| 時刻 | UTCのISO 8601文字列として妥当であること |
| 失敗分類 | 正規化後の安定`code` |
| 文言キー | 正規化後の`messageKey` |

次はTransport固有であり比較対象外とする。

- LocalのEnvelope、`requestId`、Local origin、WebView2 WebMessage
- HTTP Status、Header、`X-Request-Id`、Problem Detailsの`type`／`instance`
- 実行モード、API versionの表現差、Serverの接続情報
- 内部例外本文、Stack Trace、パス、秘密情報

## 共有Use Case

Server `/api/v1/version`はDI登録した`GetFoundationStatusUseCase`を解決し、Local WPF Composition Rootも同じUse Case型を明示登録する。Serverは`apiVersion=v1`、Localは`apiVersion=local`としてTransport経路を表現するが、状態と契約版の意味は一致させる。

## エラー正規化

| 意味 | Local Error | HTTP Problem Details |
|---|---|---|
| 入力不正 | `invalid_request` / `errors.localChannel.invalidRequest` | `validation_failed` / `errors.validation.invalid_input` |
| 未登録・未対応 | `operation_not_allowed` / `errors.localChannel.operationNotAllowed` | `not_found` / `errors.resource.not_found` |
| 内部失敗 | `handler_failed` / `errors.localChannel.handlerFailed` | `internal_error` / `errors.system.unexpected` |

正規化は比較Harnessだけで行い、Transport Envelopeの既存安定コードを変更しない。HTTPのStatusやLocalのrequest IDを他方へ持ち込まない。

## FixtureとHarness

`tests/fixtures/transport-parity/`の成功・エラーfixtureを正本とし、WPF Local Composition RootとServer実endpointへ投入する。`TransportParityTests`は成功値、代表エラー、内部情報非露出、変更値検出を確認し、後続Use Caseの比較テストへ再利用できる形を維持する。

OpenAPIのpath／schemaはP1-035で変更していない。`/api/v1/version`の既存契約と生成OpenAPIを回帰確認する。
