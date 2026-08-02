# P0-019 大容量添付アップロード・閲覧PoC

1ファイル500 MiB、1回合計1 GiBの上限を、全データをメモリへ展開しないストリーミング方式で検証する。途中取消・通信断・容量超過では一時ファイルを確定せず、同じ入力の再試行を可能にする。生成データは論理ストリームと小容量の一時ファイルだけを使用し、500 MiB／1 GiBの実データや添付をリポジトリへ作成しない。

```powershell
dotnet build .\poc\large-attachment\LargeAttachment.Poc.sln --configuration Release
dotnet .\poc\large-attachment\src\LargeAttachment.Poc\bin\Release\net10.0\LargeAttachment.Poc.dll
```

結果は`%TEMP%\AI-Development-Manager\poc\P0-019\<run-id>\result.json`へ保存する。製品コード、実データ、キャッシュは含まない。
