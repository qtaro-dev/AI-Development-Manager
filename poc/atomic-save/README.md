# P0-008 NTFS原子的保存PoC

ローカルNTFS上で、同一ディレクトリ一時ファイル、Flush、保存前バックアップ、原子的置換、障害注入、孤立一時ファイル清掃を検証します。製品保存APIではありません。

## 保存手順

1. 保存先の親ディレクトリを確認する。
2. 同一ディレクトリに`.adm-tmp-<名前>-<GUID>.tmp`を作成する。
3. 入力バイトを書き込み、`Flush(true)`する。
4. 既存原本をバックアップ領域へコピーする。
5. 元ファイルの属性を記録し、一時ファイルを原本へ置換する。
6. 属性を復元し、結果を呼び出し側へ返す。
7. 失敗時は一時ファイルを削除し、原本またはバックアップを残す。

アクセス拒否、容量不足、使用中、強制停止は障害注入で再現します。実際のACL継承は製品採用前にWindows実機で追加検証します。UNC/NAS、ETag、複数文書トランザクションは対象外です。

## 実行

```powershell
dotnet restore .\poc\atomic-save\AtomicSave.sln
dotnet build .\poc\atomic-save\AtomicSave.sln --configuration Release
dotnet .\poc\atomic-save\src\AtomicSave.Poc\bin\Release\net10.0\AtomicSave.Poc.dll
```

SDKはリポジトリ直下の`global.json`で固定します。
