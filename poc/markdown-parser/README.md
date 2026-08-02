# P0-005 Markdown・Front Matter解析PoC

P0-004の合成fixtureを入力に、既存Markdownを変更せず、バイト列・文字コード・Front Matter・Markdown本文・表・添付参照を段階的に解析する独立PoC。製品コードへ自動昇格させない。

## 判定結果

- Markdig: Markdown本文、見出し、GFM表の抽出に採用候補
- YamlDotNet: YAMLノードを安全に走査するFront Matter抽出に採用候補
- 任意.NET型の生成: 許可しない。Front Matterは`YamlMappingNode`からスカラー、配列、マッピングだけへ変換する
- 壊れたFront Matter: 文書単位の致命的エラーとして記録し、Front Matter区切り後の本文は可能な範囲で保持
- 入力ファイル: 読み取り専用。ハッシュを計算するが書き戻さない

## 実行

リポジトリ直下で、固定SDKを確認してから実行する。

```powershell
dotnet --version
dotnet restore .\poc\markdown-parser\MarkdownParser.sln
dotnet build .\poc\markdown-parser\MarkdownParser.sln --configuration Release
dotnet run --project .\poc\markdown-parser\src\MarkdownParser.Poc --configuration Release --no-build -- --verify .\poc\fixtures
```

`--verify`はmanifestの全fixtureを解析し、期待文書種別、期待警告、SHA-256、入力未変更を確認する。結果は標準出力へ出し、ログはP0-003で定義した一時保存場所へリダイレクトする。
