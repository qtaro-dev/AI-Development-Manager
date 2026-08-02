# P0-024 走査パイプライン性能PoC

10,000件の匿名生成Markdownを一時領域へ作成し、パス列挙、属性取得、`.adm-meta`読取、差分判定、本文読取、SHA-256、Markdown解析、後続引き渡しを工程別に測定する。実データと製品コードは使用しない。

```powershell
dotnet build .\poc\scan-pipeline-performance\ScanPipeline.Performance.Poc.sln --configuration Release
dotnet .\poc\scan-pipeline-performance\src\ScanPipeline.Performance.Poc\bin\Release\net10.0\ScanPipeline.Performance.Poc.dll
```

結果は`%TEMP%\AI-Development-Manager\poc\P0-024\<run-id>\result.json`へ保存する。SDKはルート`global.json`の10.0.302で固定する。

各シナリオはウォームアップ1回後に5回測定し、p50/p95/p99、最大値、工程別経過時間、本文読取数、ハッシュ数、読取バイト数、差分件数、エラー件数を保存する。

## 暫定方式

通常の再走査では、相対パス・サイズ・最終更新時刻を`.adm-meta`相当のスナップショットと比較し、属性が一致する文書の本文とフルハッシュを読まない。追加・変更・削除・改名候補だけを本文読取・ハッシュ・解析へ渡す。属性が変わらない内容変更は通常の属性比較では検知できないため、イベント通知または定期的な強制ハッシュを安全弁として別途必要とする。
