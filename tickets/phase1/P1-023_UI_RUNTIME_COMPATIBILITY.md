# P1-023 Edge・Chrome・WebView2実機互換確認

## 目的

React条件付き採用の残条件であるEdge、Chrome、WebView2、実IME、100～200% DPI、レスポンシブ、キーボード操作をWindows 11実機で確認する。

## 背景

P0-018はChromium系ローカル確認までで、複数Runtime、IME、DPIの正式確認をPhase 1へ残した。技術の再比較ではなく、採用条件の受け入れ確認として実施する。

## 前提・依存関係

- P1-018完了
- P1-020完了
- P1-021完了
- P1-022完了
- `design/ui/ui-regression-checklist.md`

## 対象範囲

- Microsoft Edge、Google Chrome、WebView2 Evergreen
- 100%、125%、150%、200% DPI
- 日本語IME入力・変換・確定・取消
- 1440、820、320px相当
- Theme、keyboard、focus、scroll、dialog
- Windows 11 64-bit

## 対象外

- Firefox、Safari、Windows 10
- 業務画面の全機能
- 認証Cookie、アップロード、Range

## 対象ファイルまたは対象モジュール

- `output/phase1/ui-runtime-compatibility`
- UI回帰チェックリスト
- 必要な基盤修正は別修正チケットへ分離

## 具体的な実装内容

1. Browser/Runtime/OS/DPI/IME版を記録する。
2. 同じproduction Web buildを各環境で表示する。
3. チェックリストに沿って操作・表示を確認する。
4. 代表screenshotと操作結果を保存する。
5. 差異を重大、許容、後続業務画面で再確認に分類する。
6. 基盤不具合は本チケット内で無関係な大規模変更をせず、小修正または別チケット化する。

## テスト内容

- 日本語IMEの入力、変換候補、確定、Escape
- Tab/Enter/Escape/Ctrl+S予約
- Focus ring、Dialog、scroll
- light/dark
- 各DPI・viewportの情報欠落
- Edge/Chrome/WebView2間の主要操作一致

## 完了条件

- 対象全環境の結果と証拠がある。
- IME、DPI、keyboard、主要レイアウトに重大な操作不能がない。
- WebView2と通常ブラウザで同じ情報構造・文言・主要操作を維持する。
- 残差異の影響と再確認フェーズが明記される。
- React採否を再検討せず、条件付き採用の確認結果を記録する。

## ユーザーが目視確認する内容

- 環境別結果表
- DPI別・Browser別の代表screenshot
- IMEとキーボード操作結果
- 残差異一覧

## 想定されるリスク

- WebView2 Runtime自動更新による差
- DPI切替時の再起動要否
- screenshotではIME操作不良を確認できない
- 基準画像未受領部分を推測で確定する

## 完了後に更新すべき設計資料

- `design/21_WEB_UI_CONTRACT.md`
- `design/24_UI_WIREFRAMES_CONTRACT.md`
- `design/ui/ui-regression-checklist.md`
- ADR-003
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

## 実装結果

`tests/Adm.Web.E2E`へEdge／Chromeの実ブラウザプロジェクトを追加し、100／125／150／200%相当の`deviceScaleFactor`で同一production Web buildを検証する。shell、Theme、Dialog、Enter／Escape、focus復帰、SPA deep link、console／page error、HTTP 4xx／5xxを自動確認し、JUnit、失敗時screenshot／trace／videoを`artifacts/ci-evidence/ui-runtime-compatibility`へ保存する。

Windows実機ではEdge 151.0.4129.59、Chrome、Microsoft Edge WebView2 Runtime 150.0.4078.105を検出した。WebView2はWPF ShellをServer originへ接続する起動スモークを行い、Runtime版、プロセス継続、起動結果を記録する。OS表示倍率の変更、実IME入力・変換・確定、WebView2内の詳細操作は環境変更を伴うためP1-027の目視確認へ分離し、deviceScaleFactor検証と混同しない。

再現コマンド:

```powershell
pwsh -NoProfile -File .\scripts\compatibility\Invoke-UiRuntimeCompatibility.ps1
```

判定: Edge／Chromeの全DPI相当テスト、console／HTTPエラー検査、WebView2起動スモークが成功し、重大な操作不能がないことを合格とする。
