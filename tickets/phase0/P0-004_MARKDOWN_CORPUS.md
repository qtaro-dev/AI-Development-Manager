# P0-004 Markdown検証コーパス作成

状態: 完了
完了日: 2026-08-02

## 目的

解析・判別PoCを再現可能にする、匿名の検証用Markdown集合を作る。

## 前提・依存関係

- P0-003完了

## 対象範囲

- 正常な各MVP文書種別
- Front Matterなし
- 壊れたYAML、未知キー、旧schema_version
- テスト表の列不足、列追加、巨大セル
- UTF-8、BOM、想定する既存文字コード
- 欠落添付、危険な相対パス

## 対象外

- DevTicketManager実データの代替生成
- 個人情報を含む実データ
- 1万文書性能データ一式

## 対象ファイルまたは対象モジュール

- `poc/fixtures/markdown`
- `poc/fixtures/manifest.yaml`

## 具体的な実装内容

1. 各fixtureの期待する文書種別、警告、抽出値をmanifestへ記述する。
2. 正常系と異常系を別フォルダーへ分ける。
3. 入力が変更されていないことを確認するハッシュを記録する。
4. 実データ由来ではないことを明記する。

## テスト内容

- manifestに存在する全fixtureが読み取れること。
- fixtureハッシュの再計算が一致すること。
- 各異常ケースに期待エラーが一つ以上定義されること。

## 受け入れ条件

- Front Matter有無と破損を含む主要ケースが揃う。
- 期待結果が機械判定可能である。
- 後続PoCが入力を変更せず再実行できる。

## ユーザーが目視確認する内容

- サンプル一覧と、想定している既存資料の種類。
- 実データではないこと。

## 想定されるリスク

- 合成データだけでは実際の揺れを再現できない。
- 文字コード対応範囲を過剰に広げる。

## 完了後に更新すべき設計資料

- Markdown互換性仕様
- テスト方針
- `design/03_PHASE_0_POC_PLAN.md`

## 実施結果

### 成果物

- `poc/fixtures/README.md`
- `poc/fixtures/manifest.yaml`
- `poc/fixtures/markdown/normal/`: Ticket、TestCase、TestResult、Design、ADR、Knowledge、ActivityLog
- `poc/fixtures/markdown/edge/`: Front Matterなし、壊れたYAML、未知キー、旧schema、表列異常、巨大セル、文字コード、欠落添付、危険相対パス
- `poc/fixtures/generate-encoded-fixtures.ps1`: UTF-8 BOM／Shift_JIS fixtureの再生成手順

### 検証結果

- fixture数: 18（正常系7、異常系11）
- 全fixtureの存在確認: 合格
- manifestのSHA-256一致: 合格
- 未知・破損・列異常・欠落添付・危険相対パスの期待警告: 定義済み
- UTF-8 BOMの先頭3バイト、Shift_JIS（Windows-31J）のラウンドトリップ: 合格
- 全fixtureが合成データであり、実データを含まないこと: 確認済み

検証コマンド:

```powershell
python -c "from pathlib import Path; import hashlib,yaml; root=Path('poc/fixtures'); m=yaml.safe_load((root/'manifest.yaml').read_text(encoding='utf-8')); fs=m['fixtures']; assert len(fs)==18; assert all((root/'markdown'/x['path']).exists() for x in fs); assert all(hashlib.sha256((root/'markdown'/x['path']).read_bytes()).hexdigest()==x['sha256'] for x in fs); assert (root/'markdown/edge/encoding-utf8-bom.md').read_bytes().startswith(bytes.fromhex('efbbbf')); b=(root/'markdown/edge/encoding-shift-jis.md').read_bytes(); assert b.decode('cp932').encode('cp932')==b; print('PASS')"
```

P0-004ではMarkdownを解析・判別する製品コードを作成していない。解析実装と機械的な期待結果照合はP0-005以降で行う。
