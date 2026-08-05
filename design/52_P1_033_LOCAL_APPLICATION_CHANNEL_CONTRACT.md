# Local Application Channel v1契約（P1-033）

## 目的と境界

Local modeの組み込みReact UIとWPF Host間で、業務機能へ昇格する前の最小メッセージ契約を固定する。対象はRequest、Response、Errorだけであり、製品のoperation registryは空のままとする。テストでのみ`test.echo` handlerを登録する。

Platform Bridge（`getHostInfo`）とは別の型、名前空間、dispatcher、許可リストを使用する。Local ChannelからPlatform Bridge、Server API、Application Service、Repositoryを直接呼び出さない。

## Envelope

Request:

```json
{"version":1,"kind":"request","requestId":"request-001","operation":"test.echo","payload":{"value":"sample"}}
```

Response:

```json
{"version":1,"kind":"response","requestId":"request-001","result":{"value":"sample"}}
```

Error:

```json
{"version":1,"kind":"error","requestId":"request-001","error":{"code":"operation_not_allowed","messageKey":"errors.localChannel.operationNotAllowed"}}
```

v1のトップレベルキーはEnvelopeごとに固定し、未知フィールド、未知`version`、未知`kind`を拒否する。`payload`と`result`はobjectまたはnullに限定する。

## 検証上限

- 固定Local origin: `https://app.ai-development-manager.local/index.html`（scheme、host、port、pathを完全一致）。
- トップレベル文書以外、外部origin、`file://`、Server originからのLocal Requestは拒否する。
- UTF-8メッセージ上限は1 MiB、JSON最大深度は16。
- `requestId`は1～64文字のASCII英数字、`_`、`-`。`operation`は1～100文字で、英字始まりの明示形式のみ許可する。
- v1ではRequestの`operation`、`payload`、Responseの`result`、Errorの`code`と`messageKey`以外の拡張フィールドを許可しない。

## 型とTransport

TypeScriptの正本実装は`src/Adm.Web/src/data-access/local/`に置き、`LocalChannelClient`が`requestId`で応答を対応付ける。未知、重複、応答済みIDは安全に無視または拒否する。WPF側は`src/Adm.Wpf/LocalChannel/`で検証、dispatch、safe error変換、serializeを行う。WebView2 Transportはこの境界の外側であり、製品業務operationを自動公開しない。

## Error

公開する安定コードは`invalid_json`、`invalid_request`、`unsupported_version`、`message_too_large`、`operation_not_allowed`、`handler_failed`、`timeout`、`cancelled`、`channel_unavailable`とする。Errorには`code`とReact側の`messageKey`だけを返し、例外本文、Stack Trace、パス、秘密値を返さない。

## P2-A01 lifecycle

Web側の`LocalChannelClient`要求は、Protocol Error、timeout、caller cancellation、channel unavailableのいずれかで有限に終了する。要求ごとにtimeoutと`AbortSignal`を指定でき、timeout／cancel／disposeで保留Mapから要求を除去する。`dispose`は冪等で、購読解除後に保留要求を`channel_unavailable`で終了し、以後の要求をTransportへ送信しない。timeout、cancel、dispose後のlate response、unknown response、duplicate responseは無視する。

DataAccess Adapterはlifecycle Errorを安全なDataAccess Failureへ変換し、例外本文をUIへ渡さない。Local Adapterは`dispose`を公開するが、HTTP Adapterの再試行方式やLocal Channelのoperation allowlist、Envelope v1、固定originは変更しない。

## Fixtureと検証

共通fixtureは`tests/fixtures/local-channel/`に置き、C#側テストでRequest、Response、Error、未知フィールドを検証する。テスト専用handlerで正常応答、handler例外の安全なError、未登録operation、origin違反、1 MiB超、Platform Bridge operationの拒否を確認する。製品operation、Progress、Cancel、Streaming、汎用RPCは登録・実装しない。
