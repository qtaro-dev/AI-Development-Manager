# P0-011 パス境界・リンク安全性PoC

プロジェクトルートを越える読取・書込・ダウンロード・アップロードを、共通の`SafePathService`で拒否できるか検証します。製品ファイルAPIではありません。

## 初期方針

- 相対パスだけを受け付け、`..`、絶対、UNC、デバイスパスを拒否する。
- URLエンコードを最大2回復号してから検証する。
- ルート外へ解決されるパス、シンボリックリンク／ジャンクション等のReparse Point、ADS、予約名、末尾ドット・空白を拒否する。
- アップロード名はディレクトリ要素を持たないUnicode NFCの単一ファイル名だけを受け付ける。
- 文字列検証と実体検証の間の差替え競合は残余リスクとし、製品では安全なハンドル・ACL・再検証を組み合わせる。

## 実行

```powershell
dotnet restore .\poc\path-security\PathSecurity.sln
dotnet build .\poc\path-security\PathSecurity.sln --configuration Release
dotnet .\poc\path-security\src\PathSecurity.Poc\bin\Release\net10.0\PathSecurity.Poc.dll
```

SDKはリポジトリ直下の`global.json`で固定します。
