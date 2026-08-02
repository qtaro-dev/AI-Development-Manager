# Markdown検証コーパス

P0-004で作成した匿名・合成の検証用Markdown集合。製品コードや実データではない。

- `markdown/normal/`: 正常なMVP文書種別
- `markdown/edge/`: 解析・判別・安全性の異常系
- `manifest.yaml`: 期待結果、警告、抽出値、SHA-256
- `generate-encoded-fixtures.ps1`: UTF-8 BOMとShift_JIS fixtureの再生成手順

再生成後は`manifest.yaml`のハッシュを更新し、入力ファイルが変更されていないことを確認する。P0-005以降はmanifestを入力契約として扱う。
