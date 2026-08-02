# P1-003 参照方向・Windows依存境界テスト

## 目的

Phase 0で確定したモジュール参照方向とWindows依存隔離を、自動テストで継続的に保証する。

## 背景

設計図だけでは後続実装でCoreからWindows AdapterやWPFを参照する退行を防げない。機能実装前に禁止依存を機械判定する。

## 前提・依存関係

- P1-002完了
- ADR-010、ADR-012

## 対象範囲

- .NETプロジェクト参照
- Assembly・Namespaceの禁止依存
- WPF、Windows Service、Explorer、Firewall、証明書ストアの境界
- PoCプロジェクトへの参照禁止

## 対象外

- 業務ロジックの単体テスト
- Web UIのimport規則
- 外部プラグイン境界

## 対象ファイルまたは対象モジュール

- `tests/Adm.Architecture.Tests`
- `src/Adm.Core`
- `src/Adm.Application`
- `src/Adm.Infrastructure.Windows`
- `src/Adm.Server.Host`
- `src/Adm.Wpf`

## 具体的な実装内容

1. ProjectReferenceとコンパイル済みAssemblyを検査するテストを作る。
2. Core/ApplicationからWindows Adapter、WPF、Server Hostへの逆参照を禁止する。
3. WPFブリッジ実装をWPF/Windows側へ限定する。
4. `poc/`配下Assemblyへの製品参照を禁止する。
5. 意図的な違反fixtureでテスト自体の検出能力を確認する。

## テスト内容

- 正常な参照グラフの合格
- CoreからWindows Adapterへの違反検出
- ApplicationからWPFへの違反検出
- 製品からPoCへの違反検出
- Windows固有Namespace混入の検出

## 完了条件

- 禁止参照を追加するとテストが確実に失敗する。
- 現行の正しい参照グラフでは成功する。
- 禁止規則と例外が設計資料と一致する。
- CIから同じテストを実行できる。

## ユーザーが目視確認する内容

- 許可・禁止参照図
- 意図的な違反が検出されるテスト結果

## 想定されるリスク

- Namespace名だけの検査で実依存を見逃す
- 例外を増やして境界が形骸化する
- テストライブラリ自体が過剰依存になる

## 完了後に更新すべき設計資料

- `design/01_INTEGRATED_BASIC_DESIGN.md`
- `design/06_REPOSITORY_RULES.md`
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

## 実施結果（2026-08-03）

`tests/Adm.Architecture.Tests/Invoke-ArchitectureBoundaryTests.ps1`を追加し、P1-004のxUnit/TestServer基盤とは分離した。

検査内容:

- 5製品プロジェクトのProjectReferenceと許可・禁止グラフ
- Debug/Releaseのビルド済みAssembly参照
- Core/ApplicationへのWindows固有Namespace混入
- 全製品プロジェクトから`poc/`への参照
- CoreからWPFを参照する意図的fixtureと、CoreへWindows Namespaceを混入するfixtureの検出能力

実行コマンド:

```powershell
pwsh -NoProfile -File .\tests\Adm.Architecture.Tests\Invoke-ArchitectureBoundaryTests.ps1 -Configuration Debug
pwsh -NoProfile -File .\tests\Adm.Architecture.Tests\Invoke-ArchitectureBoundaryTests.ps1 -Configuration Release
```

両構成で5プロジェクトの検査に成功した。P1-003ではxUnit、TestServer、CI、業務コード、PoC参照を追加していない。
