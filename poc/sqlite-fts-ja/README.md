# P0-014 SQLite FTS5日本語検索PoC

同じ文書集合をSQLite FTS5の`unicode61`と`trigram`で索引化し、日本語、英数混在、エラーコード、部分一致、snippet、ランキング、再構築を比較します。SQLiteは検索キャッシュであり、Markdownと`.adm-meta`を正本とします。

## 実行

```powershell
dotnet restore .\poc\sqlite-fts-ja\SqliteFtsJa.sln
dotnet build .\poc\sqlite-fts-ja\SqliteFtsJa.sln --configuration Release
dotnet .\poc\sqlite-fts-ja\src\SqliteFtsJa.Poc\bin\Release\net10.0\SqliteFtsJa.Poc.dll
```

SDKはリポジトリ直下の`global.json`で固定します。
