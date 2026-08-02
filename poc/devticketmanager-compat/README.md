# P0-022 DevTicketManager互換PoC

DevTicketManagerの匿名化済みコピーを読み取り専用で棚卸しし、Markdown一覧・本文・日時・検索・確認状態・添付関係の互換可否を記録する。入力が未指定の場合は自己検証だけを行い、実データの互換判定は`BLOCKED`とする。元ファイルへ書き込まず、Front Matterや`.adm-meta`を追加しない。

## 実行

```powershell
dotnet build .\poc\devticketmanager-compat\DevTicketManager.Compat.Poc.sln --configuration Release
dotnet .\poc\devticketmanager-compat\src\DevTicketManager.Compat.Poc\bin\Release\net10.0\DevTicketManager.Compat.Poc.dll --self-test
dotnet .\poc\devticketmanager-compat\src\DevTicketManager.Compat.Poc\bin\Release\net10.0\DevTicketManager.Compat.Poc.dll --input D:\path\to\anonymized-devticketmanager-copy
```

結果は`%TEMP%\AI-Development-Manager\poc\P0-022\<run-id>\result.json`へ保存する。実データ、バックアップ、キャッシュ、入力コピーはリポジトリへ置かない。

## 入力契約

`--input`にはユーザーが提供した隔離済み匿名化コピーだけを指定する。PoCは入力ディレクトリを再帰的に読み、MarkdownのUTF-8 BOM、UTF-8、Windows code page 932（画面表記はShift_JIS）を判定候補として扱う。未知の形式や不正ファイルは文書単位のエラーにし、全体を停止しない。
