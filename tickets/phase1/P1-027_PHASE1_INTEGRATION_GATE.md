# P1-027 Phase 1統合・Phase 2引継ぎゲート

## 目的

P1-001～P1-026の成果物、テスト、実機証拠、設計更新を監査し、Phase 2開始可否をユーザーが判断できる状態にする。

## 背景

個別基盤が完成しても、Server、Web、WPF、CI、配布が一つの製品基盤として再現できなければPhase 2の認証・LAN機能を安全に開始できない。

## 前提・依存関係

- P1-001～P1-026完了
- 各チケットの結果と証拠
- `design/30_PHASE1_IMPLEMENTATION_PLAN.md`

## 対象範囲

- 全Phase 1チケットの完了監査
- クリーンbuild/test/package
- console/Service/browser/WPF統合スモーク
- UI実機互換結果
- 設計・ADR・OpenAPI・SBOMの整合
- Phase 2入力と残課題

## 対象外

- 認証、HTTPS、プロジェクト登録の実装
- Phase 2チケットの一括実装
- 未達事項を黙って合格扱いにすること

## 対象ファイルまたは対象モジュール

- `src/`
- `tests/`
- `installer/`
- `design/`
- `tickets/phase1/`
- Phase 1出力・証拠

## 具体的な実装内容

1. 全チケットの受け入れ条件と証拠を照合する。
2. 固定ツールチェーンからbuild、test、Web build、E2E、packageを再実行する。
3. Server console/Service、browser、WPFの統合スモークを行う。
4. localhost限定と、認証前LAN非公開を確認する。
5. 依存脆弱性、ライセンス、SBOM、秘密情報除外を監査する。
6. Phase 2へ送る事項を実装可能な入力契約として整理する。
7. 残課題をPhase 2前必須、後続フェーズ、外部入力待ちへ分類する。

## テスト内容

- clean Debug/Release build
- .NET/Web/Architecture/E2E全テスト
- Server/WPF packageのinstall/update/uninstall
- Windows Serviceとconsoleの同一Host
- Edge、Chrome、WebView2、IME、DPI結果
- API/OpenAPI/Error/Health契約
- localhost限定・LAN非公開
- secrets/実データ/PoC生成物の成果物非混入

## 完了条件

- P1-001～P1-026が完了またはユーザー承認済み例外である。
- Phase 1完了条件を再現可能な証拠で満たす。
- 採用済み技術を再検討したり、条件未達を無断承認したりしていない。
- Phase 2の認証、HTTPS、権限、プロジェクト登録が追加判断なしで着手できる。
- 未解決事項と影響、担当フェーズ、開始条件が明示される。
- ユーザーがPhase 2開始可否を判断できる。

## ユーザーが目視確認する内容

- Phase 1結果サマリー
- Server、browser、WPFの同一UI
- 実機互換・配布結果
- 品質ゲートと残課題
- Phase 2への引継ぎ一覧

## 想定されるリスク

- 個別合格だけで統合不具合を見逃す
- 認証前ServerをLAN公開する
- 配布未確認を開発起動成功で代替する
- Phase 2機能をゲートへ混在させる

## 完了後に更新すべき設計資料

- `design/00_INDEX.md`
- `design/01_INTEGRATED_BASIC_DESIGN.md`
- `design/02_TECHNOLOGY_AND_ADR.md`
- `design/04_PHASE_PLAN.md`
- `design/30_PHASE1_IMPLEMENTATION_PLAN.md`
- Phase 2実装計画とチケット一覧

## 実施結果（2026-08-03）

統合ゲートを実施し、自動品質ゲート、Server console、localhost限定Health、Playwright、Edge／Chrome互換、WebView2起動スモークを確認した。結果の正本は`design/49_PHASE1_INTEGRATION_GATE_RESULT.md`とする。

- Debug／Release build、test、Architecture、OpenAPI、NuGet監査、npm監査、SBOM、ライセンス、禁止追跡ファイル検査: 合格。
- Playwright E2E: 9件合格。
- Edge／Chrome互換: 24件合格。DPIは100／125／150／200%のdeviceScaleFactor相当条件。
- WebView2 Runtime検出とWPF起動スモーク: 合格。Runtime 150.0.4078.105。
- Server console: 127.0.0.1:5198でlive／ready 200、非ループバック待受なし、正常停止を確認。
- Server MSIのper-machine UAC実機ライフサイクル、Windows Service、ProgramData保持、Client MSIの実インストール後ライフサイクル、IME、実OS DPIは未完了。

Architecture検査のTargetFramework取得、WebView2レジストリ列挙、Web index.html整形の最小品質ゲート修正を行った。Phase 2機能は追加していない。

## Phase 1正式完了可否

正式完了は保留。管理者セッションまたはクリーンWindows 11 VMでP1-025／P1-026の実機確認を完了するまで、P1-027は完了扱いにしない。
