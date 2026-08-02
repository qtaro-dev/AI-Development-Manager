# P0-005 Markdown・Front Matter解析PoC

状態: 完了
完了日: 2026-08-02

## 目的

既存Markdownを変更せず、本文、Front Matter、見出し、テスト表を安全に解析できる方式を決める。

## 前提・依存関係

- P0-004完了

## 対象範囲

- MarkdigとYamlDotNet候補の検証
- Front Matter有無と破損の分離
- 未知キー保持
- 見出し、本文、表、添付参照の抽出
- 文書単位のエラー隔離

## 対象外

- 文書種別のヒューリスティック確定
- Markdown保存・整形
- UI表示

## 対象ファイルまたは対象モジュール

- `poc/markdown-parser`
- 設計候補: `Adm.Documents.Parsing`
- P0-004コーパス

## 具体的な実装内容

1. バイト列、文字コード判定、Front Matter、Markdown本文を段階的に解析する。
2. YAMLオブジェクト生成を制限し、任意型生成を許可しない。
3. 抽出結果、警告、致命的エラーを別モデルにする。
4. テスト表から列と`item_id`を抽出する。
5. 入力ファイルへ一切書き込まない。

## テスト内容

- P0-004の全fixtureに対するゴールデンテスト
- 壊れたFront Matterでも可能な範囲で本文が読めること
- 未知キーを消失させないこと
- 同じ入力から同じ抽出結果が得られること

## 受け入れ条件

- fixture期待値をすべて満たす、または未対応理由が一覧化される。
- 1文書の破損で処理全体が停止しない。
- ライブラリ採否と制約がADRへ記録できる。

## ユーザーが目視確認する内容

- 正常、警告、読込不能の表示例。
- Front Matterなし文書の本文が欠落していないこと。

## 想定されるリスク

- Markdown方言による表解析差異。
- YAMLの暗黙型変換で値が意図せず変わる。

## 完了後に更新すべき設計資料

- `design/01_INTEGRATED_BASIC_DESIGN.md` Markdown仕様
- `design/02_TECHNOLOGY_AND_ADR.md`
- Markdown解析契約

## 実施結果

### 成果物

- `poc/markdown-parser/README.md`
- `poc/markdown-parser/MarkdownParser.sln`
- `poc/markdown-parser/src/Adm.Documents.Parsing/`: 解析モデルとMarkdig／YamlDotNet実装
- `poc/markdown-parser/src/MarkdownParser.Poc/`: P0-004全fixtureのゴールデン検証ランナー
- `design/07_MARKDOWN_PARSING_CONTRACT.md`: 入力、結果、エラー分離、採用候補の契約

### 実行環境

- `global.json`: .NET SDK 10.0.302、`rollForward: disable`
- `dotnet --version`: 10.0.302
- TargetFramework: `net10.0`
- Markdig: 0.41.3
- YamlDotNet: 16.3.0

### 検証結果

- P0-004全18fixture: `fixtures=18/18`
- 期待文書種別と警告: 全件一致
- Front Matterなし・壊れたYAML: 本文保持と文書単位エラー隔離を確認
- 未知キー、旧schema、表列不足・追加、巨大セル、添付欠落、危険相対パス: 期待警告を確認
- 見出し、表列・行、Front Matter添付配列を抽出
- 2回連続実行の標準出力一致: 合格
- 全fixtureの入力SHA-256不変: 合格

結果: `PASS all golden fixtures; inputs unchanged`

P0-005では製品コード、Markdown保存・整形、文書種別ヒューリスティック、UIを実装していない。MarkdigとYamlDotNetは採用候補としてADRへ記録し、巨大セルの正式上限は後続PoCへ委ねる。
