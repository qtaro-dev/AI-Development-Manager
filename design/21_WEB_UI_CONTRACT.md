# P0-018 共通Web UI・React採否PoC契約

版: 1.0-p0-018
状態: React採用候補、製品UIへの正式昇格は後続設計ゲート
基準日: 2026-08-02

## 検証対象

- チケット一覧・詳細
- 編集可能なテスト結果表
- 保存済み・未保存・競合状態
- P0-017 Cookie認証を前提とした接続表示
- 競合時の最新版／自分の変更／差分表示
- ライト・ダーク、通常幅・820px幅、キーボード操作
- 文字列はUI内へ直接散在させず、モック契約と表示定義へ分離

## 実装

`poc/web-ui-react`にReact + TypeScript + Viteの独立PoCを作成した。製品API、Markdown更新、認証サーバー、AIチャットは実装せず、`poc/web-ui-contract`のモック契約を利用する。4画面相当の主要状態を一画面内で確認できる構成とした。

## 検証結果

- `npm install`: 成功、脆弱性0件
- `npm run build`: 成功
- ブラウザ表示: 成功（Chromium系のローカル確認）
- 競合ダイアログ: 開閉、最新版選択を確認
- 編集・保存: 入力変更で未保存化、保存で完了トーストを確認
- テーマ切替: `light` / `dark`切替を確認
- 検索入力: チケット一覧の絞り込みUIを確認
- 820px幅: アイコンレール、2列一覧、1列詳細へ変化し主要操作を維持
- Escape: 競合ダイアログを閉じる
- Ctrl+S: 編集中の保存処理を実行

## 判断

今回のPoC条件ではReact + TypeScript + Viteを第一採用候補とする。UIはモックAPI境界に分離され、P0-015の検索方式保留やP0-017の認証契約へ密結合していないため、後続の正式APIへ置換できる。別ブラウザ（Edge／Chrome）とWebView2実ランタイム、100～200% DPI、実IMEは製品採用前の実機確認事項とし、React採用をこのPoCだけで最終確定しない。

## P1-023実装結果

Edge／Chromeの同一production Web buildをPlaywrightで検証し、100／125／150／200%相当のdeviceScaleFactorでshell、Theme、Dialog、keyboard、focus、deep link、console／HTTPエラーを確認する。WebView2は導入Runtimeの版記録とWPF Shell起動スモークを行う。実IME、OS表示倍率変更、WebView2内の詳細操作はP1-027の目視確認へ引き継ぐ。
