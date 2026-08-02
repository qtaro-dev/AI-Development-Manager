# P1-022 PlaywrightブラウザE2E基盤

## 目的

実ASP.NET Core Serverが配信する共通Web UIを、Chromium系ブラウザで自動操作・画面証拠化するE2E基盤を作る。

## 背景

Vitestだけでは実Navigation、CSS、フォーカス、レスポンシブ、Server配信を確認できない。業務画面前に共通シェルと状態部品の回帰手順を固定する。

## 前提・依存関係

- P1-005完了
- P1-014完了
- P1-018完了
- P1-019完了
- WebテストはPlaywrightを採用済み

## 対象範囲

- Playwright固定依存
- Server自動起動・停止
- 標準幅、820px、320px
- light/dark、keyboard、dialog、状態部品
- screenshot、trace、失敗証拠

## 対象外

- 認証・業務フロー
- WebView2自動操作
- UI画像の全面的pixel一致

## 対象ファイルまたは対象モジュール

- `tests/Adm.Web.E2E`
- Playwright設定
- CI成果物設定

## 具体的な実装内容

1. 実Serverを一時ポートで起動するE2E fixtureを作る。
2. 共通UI shell、Theme、Dialog、keyboard操作のスモークを作る。
3. 1440/820/320pxの代表screenshotを保存する。
4. 失敗時にtrace、screenshot、console、network証拠を保存する。
5. 動的時刻等を安定化し、不要に脆いpixel比較を避ける。

## テスト内容

- production buildの表示
- deep linkと再読込
- Theme切替
- Tab/Enter/Escape
- Dialog focus
- 各viewportで主操作が見えること
- JavaScript error、404 asset、console error検出

## 完了条件

- クリーン環境とCIでE2Eを再現実行できる。
- Server起動から終了まで自動管理される。
- 失敗時に原因を確認できる証拠が残る。
- レイアウト基準の主要退行を検出できる。

## ユーザーが目視確認する内容

- 代表viewportのscreenshot
- keyboard操作動画または結果
- 失敗時証拠の例

## 想定されるリスク

- 環境差で不安定なテストになる
- screenshot差分だけで操作不能を見逃す
- 開発Serverだけをテストしてproduction配信を見ない

## 完了後に更新すべき設計資料

- Web E2Eテスト方針
- `design/ui/ui-regression-checklist.md`
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

## 実装結果

`tests/Adm.Web.E2E`へPlaywright 1.55.1を固定し、Debug build済みproduction Serverの自動起動・readiness待機・停止、Chromiumスモーク、1440／820／320pxのviewport、deep link再読込、dark theme、DialogのEnter／Escape／focus復帰、console／HTTPエラー検査を追加した。代表screenshot、失敗時screenshot／trace／video、JUnitを`artifacts/ci-evidence/playwright`へ保存する。CI品質ゲートからnpm ci、Chromium導入、E2Eを実行する。WebView2、認証、業務フロー、pixel全面比較はP1-023以降へ分離した。
