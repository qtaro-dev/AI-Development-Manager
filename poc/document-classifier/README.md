# P0-006 文書種別自動判別PoC

P0-005の解析結果を入力に、Front Matter、ファイル名、フォルダー、見出し、表構造の根拠と信頼度から文書種別を判別する独立PoC。AI分類、原本へのFront Matter追加、UI実装は対象外。

## ルール

自動判別の優先順位は次のとおり。

1. Front Matterの`document_type`（信頼度1.00）
2. ファイル名（0.82）
3. フォルダー（0.72）
4. 見出し（0.68）
5. 表構造（0.66）

最上位の優先順位で複数種別が競合した場合は、既知種別へ無理に分類せず`unknown`（信頼度0.00）とする。自動判別結果と手動オーバーレイ結果は別フィールドで保持する。

## 実行

```powershell
dotnet restore .\poc\document-classifier\DocumentClassifier.sln
dotnet build .\poc\document-classifier\DocumentClassifier.sln --configuration Release
dotnet .\poc\document-classifier\src\DocumentClassifier.Poc\bin\Release\net10.0\DocumentClassifier.Poc.dll --verify .\poc\fixtures .\poc\document-classifier\rules.yaml
```

SDKはリポジトリ直下の`global.json`で固定する。結果は標準出力へ出し、入力fixtureへ書き込まない。
