# P1-005 CI品質ゲート基盤

## 目的

クリーン環境で製品をビルド・テストし、依存脆弱性、ライセンス、SBOM、生成物を継続確認するCI品質ゲートを作る。

## 背景

P0-026ではSQLite依存の安全候補を確認したが、製品全体の継続監査は未実装である。後続チケットが品質ゲートを迂回しないよう、基盤実装の初期にCIを導入する。

## 前提・依存関係

- P1-003完了
- P1-004完了
- P0-026結果

## 対象範囲

- .NET restore/build/test
- npm clean install/build/test
- Architecture tests
- NuGet/npm脆弱性監査
- ライセンス一覧とSBOM
- 成果物・ログ・テスト結果の保存
- 秘密情報を含まないCI設定

## 対象外

- 本番署名
- 公開リリース
- Phase 2以降の性能・E2E全試験

## 対象ファイルまたは対象モジュール

- CIワークフロー設定
- `scripts/`または同等の再現可能な品質コマンド
- `.NET`、Web、Architecture testプロジェクト

## 具体的な実装内容

1. 固定SDKと固定Node依存をクリーン取得する。
2. restore、format/静的検査、Debug/Release build、testを段階化する。
3. NuGetとnpmのHigh/Critical脆弱性を失敗条件にする。
4. 依存・ライセンス一覧とSBOMを成果物として保存する。
5. テスト結果と失敗ログを追跡可能に保存する。
6. 後続プロジェクト追加時に自動対象となる構成にする。

## テスト内容

- クリーンCIの成功
- コンパイルエラー、テスト失敗、参照違反の検出
- High脆弱性fixtureまたは安全な模擬条件の検出
- lockfile不一致の検出
- SBOMとライセンス成果物の生成
- 秘密情報がログ・成果物に含まれないこと

## 完了条件

- ローカルとCIで同じ主要コマンドを実行できる。
- Build、Test、Architecture、High/Critical監査のいずれかが失敗すればゲートが失敗する。
- ライセンスとSBOMがビルド単位で追跡できる。
- PoC、実データ、秘密情報、キャッシュを成果物へ含めない。

## ユーザーが目視確認する内容

- CI工程と合否条件
- 生成された依存・ライセンス・SBOM一覧
- 意図的な失敗がゲートで止まる例

## 想定されるリスク

- 外部監査サービス障害で再現性が下がる
- 警告抑制で脆弱性を見逃す
- CIだけ成功しローカル再現できない

## 完了後に更新すべき設計資料

- `design/06_REPOSITORY_RULES.md`
- `design/02_TECHNOLOGY_AND_ADR.md`
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

## 実装結果（P1-005）

- `.github/workflows/quality-gates.yml`を追加し、`main`へのpush、Pull Request、手動実行でWindows runnerの品質ゲートを起動する。失敗時を含め、`artifacts/ci-evidence`をGitHub Actionsアーティファクトへ保存する。
- `scripts/ci/Invoke-QualityGates.ps1`をローカル／CI共通入口として追加した。固定SDK・Node確認、restore、Debug／Release build・test、P1-003 Architecture検査、NuGet脆弱性監査、ライセンス一覧、CycloneDX 1.5 SBOM、禁止生成物・秘密情報検査を実施する。
- Web製品基盤（`src/Adm.Web/package.json`）が未作成のため、npm工程は未導入として`web-not-present.txt`へ記録した。P1-013で追加後は同じゲートが自動的にnpm工程を対象とする。
- ローカル実行証拠は`artifacts/ci-evidence`へ生成される。これは`.gitignore`によりコミット対象外である。

## 検証結果

実行コマンド:

```powershell
pwsh -NoProfile -File .\scripts\ci\Invoke-QualityGates.ps1
```

結果: 成功（exit code 0）。使用SDKは`10.0.302`、Node.jsは`.node-version`の`22.18.0`。Debug／Release build、Debug／Release test、P1-003 Architecture検査（Debug／Release）、NuGet脆弱性監査、ライセンス一覧、SBOM、追跡ファイル検査が成功した。NuGetのHigh／Critical検出はなく、Web npm工程は未導入証拠を保存した。

P1-005のCI基盤以外の製品機能は変更していない。P1-006以降は対象外とし、既存の未コミット変更および生成物はコミット対象から除外する。
