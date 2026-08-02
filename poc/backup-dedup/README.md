# P0-020 バックアップ重複抑制PoC

同一添付を内容ハッシュで1つのblobとして保存し、世代manifestから参照する方式を検証する。500 MiBは論理サイズで比較し、実ファイルは小容量の検証データだけを一時領域に生成する。

```powershell
dotnet build .\poc\backup-dedup\BackupDedup.Poc.sln --configuration Release
dotnet .\poc\backup-dedup\src\BackupDedup.Poc\bin\Release\net10.0\BackupDedup.Poc.dll
```

結果は`%TEMP%\AI-Development-Manager\poc\P0-020\<run-id>\result.json`に保存する。PoC成果物は製品コードへ昇格させない。

## 暫定結論

単純コピーではなく、SHA-256・バイト長・保存blobの実体検証を組み合わせた自己完結型バックアップ集合を採用候補とする。manifest/blobの欠落・破損時は復元先へ書き込まず、復元前退避と監査記録を残す。NTFS固有機能には依存しない。
