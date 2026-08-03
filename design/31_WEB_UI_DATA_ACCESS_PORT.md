# P1-031 Web UI DataAccess Port契約

## 目的

React UIからHTTP、WebView2、WPFなどの実行方式を分離し、同じUIを明示的に選択したDataAccess実装へ接続できる境界を定義する。

## 依存関係

```text
App / UI component
        |
        v
DataAccessPort
        ^
        |
Composition boundary ---- HTTP Adapter ---- api/client.ts ---- fetch
        |
        +---- Fake Adapter (tests)
        |
        +---- Local Adapter (future; not implemented in P1-031)
```

UIとPortはHTTP URL、HTTP status、例外本文、WebView2 APIを公開しない。`api/client.ts`と`fetch`はHTTP Adapterの内部に限定する。

## P1-031で公開する最小契約

| 項目 | 契約 |
| --- | --- |
| 実行モード | `local` または `server`。Composition境界から明示的に指定する |
| 操作 | `getFoundationStatus()` の1件のみ |
| 成功結果 | `FoundationStatus`（state、API version、contract version、UTC server time） |
| 失敗結果 | `DataAccessFailure`（安全なcode、利用者向けmessage、retryable、nextAction） |
| 呼出し結果 | 例外をUIへ渡さず、`DataAccessResult<T>`で成功／失敗を判別する |

業務操作、汎用RPC、進捗、取消、ストリーミング、イベント購読はこのPortへ追加しない。必要になった場合はユースケース単位の別チケットで契約を定義する。

## Composition方針

- `composeDataAccess`は、注入されたAdapterを最優先で使用する。
- `server` modeでAdapterが省略された場合だけHTTP Adapterを構成する。
- Adapterがなく、かつHTTP構成もない場合は、安全なCompositionエラーで起動を中止する。
- ブラウザ環境、WebView2環境、URLの有無によるグローバル自動判別は行わない。
- Local AdapterおよびWebMessage実装は後続チケットの対象とし、P1-031では作成しない。

## 検査とテスト

`Validate-WebDataAccessBoundary.mjs`がUIソース（`api`、`data-access`、テストを除く）を走査し、直接`fetch`、`window.chrome.webview`、`api/client`のimportを検出する。VitestではHTTP成功／安全な失敗、Fake Adapter差替、無効なCompositionを確認する。

この資料はP1-031の設計記録であり、P1-032以降の業務DataAccess契約やLocal Adapterの採用を決定するものではない。
