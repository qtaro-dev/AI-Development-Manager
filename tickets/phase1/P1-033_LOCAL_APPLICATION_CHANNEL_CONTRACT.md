# P1-033 Local Application Channel契約

## 目的

組み込みReact UIとWPF Hostの間でLocal modeの業務要求を受け渡すため、Request、Response、Errorだけからなる最小のLocal Application Channel契約を定義し、型・検証・安全境界を実装する。

Phase 1では契約とTransport境界だけを成立させ、Application Serviceの業務操作、Progress、Cancel、Streaming等を先行実装しない。

## 背景

P1-028／ADR-019で、Local Application ChannelはPlatform Bridgeと分離し、Phase 1ではRequest、Response、Errorだけを扱うと確定した。P1-031でWeb UIのDataAccess Portを具体Transportから分離し、P1-032で固定仮想HTTPS originから製品組み込みUIを表示する予定である。

次のComposition Root実装へ進む前に、WebMessageの形式、要求と応答の対応、安全なエラー、origin、入力上限、操作許可リストを小さな契約として固定する必要がある。

## 前提・依存関係

- P1-028承認済み
- P1-029条件付き採用として承認済み
- P1-030承認済み
- P1-031完了・承認済み
- P1-032完了・承認済み
- `design/44_WPF_BRIDGE_CONTRACT.md`
- `design/50_ADR_019_LOCAL_FIRST_EXECUTION_MODEL.md`
- P1-031 Web UI DataAccess Port契約
- P1-032で確定したLocal modeの固定仮想HTTPS origin

## 対象範囲

- Local Application Channel v1のRequest、Response、Error Envelope
- TypeScript側の型、serialize／parse、要求・応答対応
- C#側の型、JSON parse、検証、安全なserialize
- WebView2 `WebMessageReceived`を利用する専用Channel境界
- 固定Local originとトップレベル文書の検証
- 明示的な操作許可リスト
- 要求ID、操作名、payload、JSONサイズの最小検証
- 未知version、未知kind、未知operation、不正JSONの拒否
- 内部例外を露出しないError変換
- テスト専用HandlerによるRequest／Response／Error往復試験
- Platform BridgeとLocal Application Channelの分離検査
- Channel契約資料とサンプルJSON

## 対象外

- プロジェクト、Markdown、チケット、添付、テスト、検索等の製品業務operation
- Application Service、Repository、保存Adapterへの接続
- WPF Composition Rootの本実装
- Progress通知
- Cancel要求
- Streaming
- 分割転送
- 双方向イベント購読
- 長時間処理の汎用ジョブ基盤
- 大容量添付をChannel payloadで送受信すること
- 任意メソッド呼出、Reflection、汎用RPC、プラグイン機構
- Platform Bridgeの既存`getHostInfo`変更
- Server modeのHTTP API変更
- UI画面、文言、レイアウト変更
- P1-034以降のチケット作成または着手

## 対象ファイルまたは対象モジュール

- `src/Adm.Web/src/data-access/local/`
- `src/Adm.Web/src/data-access/`のLocal Adapter接続点
- `src/Adm.Wpf/LocalChannel/`
- `src/Adm.Wpf/Bridge/`は分離確認のみ
- `tests/Adm.Infrastructure.Windows.Tests/`またはWPF Channel用テスト
- `src/Adm.Web/src/**/*.test.ts`
- `design/44_WPF_BRIDGE_CONTRACT.md`
- P1-033 Local Application Channel契約資料
- `tickets/phase1/P1-033_LOCAL_APPLICATION_CHANNEL_CONTRACT.md`
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

`Adm.Application`、`Adm.Core`、`Adm.Server.Host`、`installer/`へ業務実装を追加しない。

## 最小Envelope

### Request

```json
{
  "version": 1,
  "kind": "request",
  "requestId": "request-identifier",
  "operation": "operation.name",
  "payload": {}
}
```

### Response

```json
{
  "version": 1,
  "kind": "response",
  "requestId": "request-identifier",
  "result": {}
}
```

### Error

```json
{
  "version": 1,
  "kind": "error",
  "requestId": "request-identifier",
  "error": {
    "code": "stable_error_code",
    "messageKey": "errors.localChannel.failed"
  }
}
```

Phase 1ではEnvelopeへProgress、Cancel、Streaming、timestamp、汎用metadata、任意extensionsを追加しない。要求追跡は`requestId`で行い、別の汎用追跡基盤を作らない。

## 初期検証規則

- `version`は整数`1`のみ。
- `kind`は`request`、`response`、`error`のいずれか。
- `requestId`は1～64文字の安全なASCII文字列。空白、制御文字、改行を拒否する。
- `operation`はRequestだけに存在し、1～100文字の許可された名前形式に限定する。
- `payload`と`result`はJSON objectまたは`null`に限定する。
- Errorは安定した`code`とReact側日本語辞書の`messageKey`だけを返す。Hostから任意の表示文言を返さない。
- 1メッセージのUTF-8換算上限は1 MiBとする。大容量データは別方式を後続設計する。
- 未知フィールドの扱いはv1契約で一意に定め、TypeScriptとC#で同じ結果にする。
- JSON depth、文字列長、配列要素数等は.NET／Webの安全な既定と明示上限を契約資料へ記録する。
- 設計資料に登録されていないoperationは実行しない。

## 具体的な実装内容

1. Local Application Channel v1の正本契約、Envelope、検証規則、Errorコード、`messageKey`を設計資料へ記録する。
2. TypeScriptへRequest／Response／Errorの判別Union、parse、serialize、要求・応答対応を実装する。
3. C#へ同等の型、JSON parse、検証、安全なserializeを実装する。
4. TypeScriptとC#で共通利用する正常・異常JSON fixtureを作り、判定結果を一致させる。
5. P1-032で確定した固定Local originとトップレベル文書だけからRequestを受け付ける。
6. Local Channel専用のoperation registryを設け、未登録operationを`operation_not_allowed`等の安定コードで拒否する。
7. テスト専用operation／Handlerだけで正常Responseと安全なErrorを確認し、製品業務operationは登録しない。
8. Handler例外、parse例外、未知version、不正Requestを`code`と`messageKey`だけのErrorへ変換し、例外本文、Stack Trace、ローカルパス、秘密値を返さない。
9. Web側は`requestId`でResponse／Errorを対応付け、重複、未知、応答済みIDを安全に無視または拒否する。
10. Platform BridgeとChannelのEvent Handler、型、operation registry、Namespaceを分離し、`getHostInfo`をLocal Channelへ移さない。
11. 1 MiB超、深すぎるJSON、不正文字、未知kind、未知operation等の境界テストを追加する。
12. Build、Test、Architecture、Web Test、Playwright、製品WPFのRequest／Response／Error往復スモークを実行する。

## 初期Errorコード

- `invalid_json`
- `invalid_request`
- `unsupported_version`
- `message_too_large`
- `operation_not_allowed`
- `handler_failed`
- `channel_unavailable`

内部例外名やHTTP StatusをErrorコードとして公開しない。追加コードは具体的な製品operationのチケットで定義する。

## テスト内容

### 正常系

- 有効Requestのparse
- テストHandlerへのdispatch
- 対応するResponse
- Web側Promise相当の要求・応答対応
- 同じfixtureをTypeScript／C#で同じ結果に判定

### Error系

- Handlerが返す安全なError
- Handler例外の`handler_failed`変換
- Errorに例外本文、Stack Trace、パス、秘密値が含まれない
- 未知response ID、重複response ID、応答済みID

### 入力境界

- 不正JSON
- 未知version
- 未知kind
- 欠落／不正requestId
- 欠落／不正operation
- 未登録operation
- 不正payload型
- 1 MiB境界と超過
- 深すぎるJSON、過大文字列、過大配列

### WebView2安全境界

- 固定Local originのトップレベル文書は許可
- Server origin、外部origin、`file://`、子Frameは拒否
- Platform BridgeとLocal Channelのoperationを相互に実行できない
- 任意コード、コマンド、パス操作を実行できない

### 回帰

- Debug／Release Build、Test、Architecture検査
- Web Build、型検査、Test、Playwright
- Local mode組み込みUI表示
- 明示的Server mode
- UIの視覚差分なし
- `git diff --check`

## 完了条件

- Request、Response、Errorだけからなるv1契約が設計正本に記録されている。
- TypeScriptとC#の型・検証が共通fixtureで一致する。
- 固定Local originのトップレベル文書以外からRequestを受け付けない。
- 未登録operation、未知version、不正JSON、過大Messageを安全に拒否する。
- Handler例外が内部情報を含まないErrorへ変換される。
- テスト専用HandlerでRequest／Response／Errorの往復が成功する。
- Platform BridgeとLocal Application Channelが別Namespace、別registry、別operation集合として維持されている。
- 製品業務operation、Application Service dispatch、Repositoryを実装していない。
- Progress、Cancel、Streaming、イベント、汎用RPCを実装していない。
- Build、Test、Architecture、Web Test、Playwrightが合格する。
- Local／Server modeの既存表示を壊していない。
- P1-034以降を作成・着手していない。

## ユーザーが目視確認する内容

- Request、Response、ErrorのJSON例
- DataAccess Port、Local Channel、Platform Bridge、Applicationの責務図
- 正常Responseと利用者向けErrorの往復結果
- 外部origin、未知operation、過大Messageの拒否結果
- Errorへ内部例外・パスが露出していないこと
- Progress、Cancel、Streamingが含まれていないこと
- Local modeとServer modeの既存画面に視覚差分がないこと

## 想定されるリスク

- Channelを汎用RPC基盤へ拡張する。
- Platform Bridgeと業務Channelを同じregistryへ統合する。
- Errorへ例外本文、Stack Trace、ファイルパスを返す。
- origin検証をNavigation検証だけに依存し、Message受信時に検証しない。
- 任意operation名をReflectionやDIから自動公開する。
- 大容量添付をJSON payloadで送る。
- Progress、Cancel、Streamingを将来用として先行追加する。
- TypeScriptとC#で未知フィールドや上限の判定が異なる。
- 契約実装と業務operation実装を混在させる。

## 完了後に更新すべき設計資料

- `design/00_INDEX.md`
- `design/30_PHASE1_IMPLEMENTATION_PLAN.md`
- `design/44_WPF_BRIDGE_CONTRACT.md`
- P1-033 Local Application Channel契約資料
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`
- `tickets/phase1/P1-033_LOCAL_APPLICATION_CHANNEL_CONTRACT.md`

## 完了時に残す証拠

- v1契約正本とJSON例
- TypeScript／C#共通fixture一覧
- 正常・異常・上限テスト結果
- origin／top-level検証結果
- Platform Bridge分離検査結果
- Debug／Release Build、Test、Architecture結果
- Web Build、型検査、Test、Playwright結果
- WPF Request／Response／Error往復スモーク
- `git diff --check`結果

## 実施結果

- Local Application Channel v1のRequest／Response／Error型、strict parse、safe serializeをTypeScript／C#へ実装した。
- 固定Local origin、トップレベル文書、1 MiB、JSON深度16、ID／operation形式、未知フィールドを検証する。
- `LocalChannelClient`はrequestIdでResponse／Errorを相関し、未知・重複応答を安全に処理する。
- 製品registryは空のままとし、`test.echo`はテスト専用handlerに限定した。`getHostInfo`は拒否される。
- 共通fixture、origin／上限／例外秘匿／Platform Bridge分離テストを追加した。
- P1-032のLocal mode表示、Server mode、UIレイアウトは変更していない。

検証結果は最終報告へ記録する。P1-034以降は着手していない。
