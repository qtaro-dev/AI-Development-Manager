# P1-004 .NETテスト基盤

## 目的

Server、Core、Application、Windows Adapterの後続実装で共通利用するxUnit単体・統合テスト基盤を作る。

## 背景

Phase 0のPoCは独立した実行形式で検証した。製品実装では、チケットごとに同じ方法でUnit、Integration、TestServerテストを追加できる必要がある。

## 前提・依存関係

- P1-002完了
- `design/01_INTEGRATED_BASIC_DESIGN.md`第16節

## 対象範囲

- xUnit共通設定
- Unit/Integrationカテゴリ
- ASP.NET Core TestServer/WebApplicationFactory基盤
- 一時ディレクトリとテスト証拠の安全な管理
- Windows限定テストの明示

## 対象外

- 業務機能テスト
- Playwright
- 実LAN・インストーラーテスト

## 対象ファイルまたは対象モジュール

- `tests/Adm.Core.Tests`
- `tests/Adm.Application.Tests`
- `tests/Adm.Server.IntegrationTests`
- `tests/Adm.Infrastructure.Windows.Tests`
- `tests/Adm.Testing`

## 具体的な実装内容

1. 共通fixture、時刻・一時パス・追跡IDなどのテスト補助境界を作る。
2. TestServerを起動する最小統合テストを作る。
3. Windows限定テストを属性・カテゴリで分離する。
4. テスト出力、ログ、失敗証拠の配置を統一する。
5. 並列実行で共有状態を汚染しない規則を作る。

## テスト内容

- Unitサンプルの成功・失敗検出
- TestServer起動・停止
- 一時領域の作成・清掃
- Windows限定テストの選択実行
- テスト失敗時のログ保存

## 完了条件

- 固定SDKからUnit/Integrationを個別・一括実行できる。
- TestServerが実ポートを占有せず再現実行できる。
- Windows限定条件が明示され、非該当環境で黙って成功扱いにならない。
- 後続チケットが共通fixtureを再実装せず利用できる。

## ユーザーが目視確認する内容

- テスト分類と実行コマンド
- 成功・失敗時の証拠保存例

## 想定されるリスク

- 共通fixtureが巨大化する
- テスト間で一時ファイルやポートが競合する
- Windows限定テストがCIで常時除外される

## 完了後に更新すべき設計資料

- `design/01_INTEGRATED_BASIC_DESIGN.md`第16節
- `design/06_REPOSITORY_RULES.md`
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

## 実施結果（2026-08-03）

以下のテスト基盤を追加した。

- `tests/Adm.Testing`: テストごとの一時ディレクトリと追跡IDを提供する共通補助境界
- `tests/Adm.Core.Tests`: xUnit単体テストの最小サンプル
- `tests/Adm.Application.Tests`: Application単体テストの最小サンプル
- `tests/Adm.Server.IntegrationTests`: `WebApplication.UseTestServer()`による実ポートを使わない統合テスト
- `tests/Adm.Infrastructure.Windows.Tests`: `Category=Windows`を付けたWindows限定テスト
- 中央NuGet管理へ`Microsoft.NET.Test.Sdk`、`xunit`、`xunit.runner.visualstudio`、`Microsoft.AspNetCore.TestHost`の固定版を追加

P1-003のArchitecture検査、CI、業務機能、Playwrightは変更していない。

### 検証結果

- SDK: `dotnet --version` = `10.0.302`
- `dotnet restore AIDevelopmentManager.sln`: 成功
- Debug/Release Solution build: 警告0、エラー0
- Debug/Release `dotnet test`: 4テストプロジェクト、各1件成功
- TestServer: 実ポートを占有せずHTTP 204応答を確認
- P1-003 Architecture検査: Debug/Releaseとも成功
