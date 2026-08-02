# P1-002 製品ソリューション・モジュール骨格

## 目的

PoCから独立した製品本体のVisual Studioソリューションと、Server・Core・Windows Adapter・WPF・テスト・Web UIの配置骨格を作る。

## 背景

Phase 0ではPoC専用ソリューションだけを作成し、製品コードへ自動昇格させなかった。P0-023完了後に、確定した責務境界で製品本体を開始する。

## 前提・依存関係

- P1-001完了
- `design/01_INTEGRATED_BASIC_DESIGN.md`
- ADR-001、ADR-002、ADR-010、ADR-012

## 対象範囲

- ルート製品ソリューション
- `src/`の.NET製品プロジェクト
- `tests/`の対応テスト配置
- `src/Adm.Web`のWeb UI配置予約
- プロジェクト責務と参照方向の文書化

## 対象外

- 業務エンティティ、業務API、保存処理
- PoCコードのコピー
- UIコンポーネント実装

## 対象ファイルまたは対象モジュール

- ルート`AIDevelopmentManager.sln`
- `src/Adm.Core`
- `src/Adm.Application`
- `src/Adm.Server.Host`
- `src/Adm.Infrastructure.Windows`
- `src/Adm.Wpf`
- `src/Adm.Web`
- `tests/`

## 具体的な実装内容

1. ルートに製品用Visual Studioソリューションを作成する。
2. 各責務の最小プロジェクトを作成し、共通ビルド設定を適用する。
3. `Adm.Core`と`Adm.Application`をWindows非依存Target Frameworkにする。
4. WPFとWindows AdapterだけをWindows固有Target Frameworkにする。
5. 空の責務説明と参照規則を各モジュールへ記録する。
6. PoCプロジェクトを製品ソリューションへ含めない。

## テスト内容

- ソリューション全体のDebug/Releaseビルド
- Windows非依存プロジェクトのTarget Framework確認
- PoC参照がないことの確認
- Visual StudioとCLIの双方からのロード・ビルド

## 完了条件

- 製品ソリューションが固定SDKでDebug/Releaseビルドできる。
- 各モジュールの責務が一つで、PoCと物理的・参照上分離されている。
- Core/ApplicationにWindows固有参照がない。
- 後続チケットが対象モジュールを一意に選べる。

## ユーザーが目視確認する内容

- Solution Explorerのプロジェクト構成
- 製品コードとPoCの分離
- モジュール責務図

## 想定されるリスク

- 将来機能を見越して不要なプロジェクトを増やす
- Windows固有パッケージがCoreへ推移混入する
- Web成果物の配置責務がServerと重複する

## 完了後に更新すべき設計資料

- `design/01_INTEGRATED_BASIC_DESIGN.md`
- `design/06_REPOSITORY_RULES.md`
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

## 実施結果（2026-08-03）

ルートに`AIDevelopmentManager.sln`を作成し、以下の製品プロジェクトを追加した。

- `src/Adm.Core`: `net10.0`、Windows非依存
- `src/Adm.Application`: `net10.0`、`Adm.Core`のみ参照
- `src/Adm.Server.Host`: ASP.NET Core Web SDK、空骨格、P1-006までLibrary出力
- `src/Adm.Infrastructure.Windows`: `net10.0-windows`、Windows Adapter境界
- `src/Adm.Wpf`: `net10.0-windows`、WPF骨格、P1-020までLibrary出力
- `src/Adm.Web`: P1-013用のWeb UI配置予約
- `tests/`: P1-003/P1-004用のテスト配置予約

各モジュールに責務と参照規則のREADMEを配置した。P1-002では業務API、保存、認証、WebView2、UI、xUnitテスト、CIを実装していない。PoCプロジェクトはソリューションにもProjectReferenceにも含めていない。

### 検証結果

- 固定SDK: `dotnet --version` = `10.0.302`
- `dotnet restore AIDevelopmentManager.sln`: 成功
- `dotnet build AIDevelopmentManager.sln --configuration Debug --no-restore`: 成功、警告0、エラー0
- `dotnet build AIDevelopmentManager.sln --configuration Release --no-restore`: 成功、警告0、エラー0
- 全5.NETプロジェクトのTarget FrameworkとProjectReferenceを確認
- Solutionと各プロジェクトから`poc/`参照がないことを確認
