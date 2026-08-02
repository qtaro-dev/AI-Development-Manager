# P0-012 ZIP安全閲覧PoC

ZIPを自動展開せず、一覧検査と個別エントリーの期限付き一時閲覧を行う検証コードです。永続展開キャッシュや製品添付APIではありません。

## 初期上限

- ZIP本体: 500 MiB
- エントリー数: 10,000
- 個別展開サイズ: 250 MiB
- 合計展開見積: 2 GiB
- 圧縮率: 100倍
- ネスト深度: 2
- 一時閲覧保持: 24時間

設定は`ZipInspectionLimits`で変更可能です。暗号化、壊れたZIP、Zip Slip、重複名、ZIP Bomb、過大ネスト、過大容量、過大件数を拒否し、実行形式は表示せずダウンロード扱いにします。

## 実行

```powershell
dotnet restore .\poc\zip-safety\ZipSafety.sln
dotnet build .\poc\zip-safety\ZipSafety.sln --configuration Release
dotnet .\poc\zip-safety\src\ZipSafety.Poc\bin\Release\net10.0\ZipSafety.Poc.dll
```

SDKはリポジトリ直下の`global.json`で固定します。
