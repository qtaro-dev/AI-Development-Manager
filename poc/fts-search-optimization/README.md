# P0-025 FTS索引・検索最適化PoC

P0-015互換の固定seed 15015、10,000件の匿名合成Markdownを一時領域へ作成し、次の構成を比較する。

- `unicode61_external`: external-content FTS5。本文・見出し・パスをunicode61で索引化
- `scoped_trigram`: unicode61に加え、パス・ファイル名・見出しだけをtrigramで索引化
- `full_trigram_control`: 本文を含む全検索列をtrigramで索引化する容量比較用

```powershell
dotnet build .\poc\fts-search-optimization\FtsSearchOptimization.sln --configuration Release
dotnet .\poc\fts-search-optimization\src\FtsSearchOptimization.Poc\bin\Release\net10.0\FtsSearchOptimization.Poc.dll
```

結果は`%TEMP%\AI-Development-Manager\poc\P0-025\<run-id>\result.json`へ保存する。SDKはルート`global.json`の10.0.302で固定する。PoC実行は生成コーパス、SQLiteキャッシュ、ログを一時領域へ置き、リポジトリへ保存しない。

各検索語は低・中・高ヒット、識別子、エラーコード、部分一致、0件に分類し、p50/p95/p99、結果件数、正解集合、snippet、安定ソートを記録する。通常検索とsnippet生成を分離した上位候補方式も測定する。
