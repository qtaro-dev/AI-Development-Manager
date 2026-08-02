# P0-013 ファイル走査・監視・再同期PoC

Markdownと`.adm-meta` JSONを初回全走査し、FileSystemWatcherの重複・欠落を前提にデバウンス、定期再走査、手動再走査、読込再試行、進捗、文書単位エラーを検証します。製品索引や検索ランキングではありません。

## 方針

- 対象は`.md`と`.adm-meta`配下の`.json`。
- 監視イベントはパス単位でデバウンスし、同一文書を無限処理しない。
- 定期・手動の全走査を正とし、監視漏れを差分で補正する。
- 1文書の読込失敗はエラーとして記録し、全走査を停止しない。
- 読込中ファイルは上限付き再試行後に文書単位エラーとする。

## 実行

```powershell
dotnet restore .\poc\file-scan-watch\FileScanWatch.sln
dotnet build .\poc\file-scan-watch\FileScanWatch.sln --configuration Release
dotnet .\poc\file-scan-watch\src\FileScanWatch.Poc\bin\Release\net10.0\FileScanWatch.Poc.dll
```

SDKはリポジトリ直下の`global.json`で固定します。
