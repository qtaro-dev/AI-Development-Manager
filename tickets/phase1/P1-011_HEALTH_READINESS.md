# P1-011 ヘルス・Ready状態基盤

## 目的

Serverプロセスの生存と、要求を受けられる準備状態を分離して確認できるヘルス基盤を作る。

## 背景

Windows Service、WPF、インストーラー、将来の管理画面が同じ方法でServer状態を確認する必要がある。詳細な走査ヘルスはPhase 3で追加する。

## 前提・依存関係

- P1-006完了
- P1-007完了
- P0-002、P0-013のヘルス方針

## 対象範囲

- liveness
- readiness
- Build、起動モード、時刻の安全な情報
- 後続機能用Health contributor境界

## 対象外

- 走査件数・エラー文書数
- 認証済み管理診断
- 外部監視製品連携

## 対象ファイルまたは対象モジュール

- `src/Adm.Server.Host/Health`
- `src/Adm.Application/Health`
- `tests/Adm.Server.IntegrationTests`

## 具体的な実装内容

1. livenessとreadinessを別Routeへ定義する。
2. 後続依存が未準備の場合だけreadinessを失敗させる拡張点を作る。
3. 秘密、ローカルパス、内部例外を公開応答へ含めない。
4. WPF/Installerが待機・再試行に使える安定形式を作る。

## テスト内容

- 起動直後・準備完了・停止中の状態遷移
- contributor失敗時のreadiness
- livenessが業務依存失敗と分離されること
- 応答の秘密情報非含有

## 完了条件

- 生存と準備状態を機械的に区別できる。
- console/Serviceで同じ契約を返す。
- 後続の走査・索引ヘルスを追加できる。
- WPFが内部ログを読まず状態判定できる。

## ユーザーが目視確認する内容

- 起動からReadyまでの状態表示
- 障害時の応答と利用者向け説明

## 想定されるリスク

- 重い検査をlivenessへ追加して誤停止する
- 公開応答に構成・パスを出す
- Ready前にWPFが業務画面を表示する

## 完了後に更新すべき設計資料

- Server運用・ヘルス設計
- `design/01_INTEGRATED_BASIC_DESIGN.md`
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

## 実装結果

- `/health/live`と`/health/ready`を追加し、プロセス応答性と準備状態を分離した。
- `Adm.Application.Health.IHealthContributor`を後続依存の拡張点として追加した。
- 起動完了・停止処理のライフサイクルをreadinessへ反映した。
- Contributor失敗はreadinessのみ503とし、livenessは200を維持する。
- 応答は状態、Build、起動モード、UTC時刻、失敗Contributorの安全なコードだけとし、秘密・パス・例外本文を含めない。
- Server、Serviceで同じHost構成を利用できる拡張方式とし、Windows Service登録、走査、索引、外部監視は実装していない。

## 検証結果

使用SDK: `10.0.302`（`global.json`固定値と`dotnet --version`実測値が一致）。

実行したコマンド:

```powershell
dotnet build .\AIDevelopmentManager.sln --configuration Debug
dotnet test .\tests\Adm.Server.IntegrationTests\Adm.Server.IntegrationTests.csproj --configuration Debug --no-build --no-restore
pwsh -NoProfile -File .\scripts\api\Validate-OpenApiContract.ps1
```

統合テストではlive/readinessの正常応答、Contributor失敗時の503、liveness分離、停止・追跡基盤との共存、秘密情報非含有を確認した。

P1-011は完了状態とし、P1-012以降は対象外とする。
