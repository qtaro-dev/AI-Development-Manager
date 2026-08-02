# P0-009 ETag競合検知PoC

同じ文書を複数クライアントが編集する場合の楽観的排他を検証します。製品APIや差分UIではありません。

## 契約候補

- 強いETagは、保存対象の現在バイト列のSHA-256をBase64URL化し、`"sha256-<value>"`として表す。
- 更新には`If-Match`を必須とし、欠落はHTTP 428相当で拒否する。
- ETag不一致はHTTP 409相当とし、追跡ID、最新版ETag、最新版内容、入力内容、差分取得先を返す。
- 古いETagでも入力内容が最新版と同一なら、安全な再送としてno-op成功とする。
- 長時間ロックは使わず、読み取り後のETag比較で競合を検知する。

## 実行

```powershell
dotnet restore .\poc\etag-concurrency\EtagConcurrency.sln
dotnet build .\poc\etag-concurrency\EtagConcurrency.sln --configuration Release
dotnet .\poc\etag-concurrency\src\EtagConcurrency.Poc\bin\Release\net10.0\EtagConcurrency.Poc.dll
```

SDKはリポジトリ直下の`global.json`で固定します。
