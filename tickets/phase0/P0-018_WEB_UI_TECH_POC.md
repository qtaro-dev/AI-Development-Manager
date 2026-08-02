# P0-018 共通Web UI・React採否PoC

## 目的

React＋TypeScript＋Viteで、WPF WebView2と通常ブラウザに同じ主要UIを提供できるか判断する。

## 前提・依存関係

- P0-017完了

## 対象範囲

- チケット一覧＋詳細のUIサンプル
- 編集可能なテスト表サンプル
- 保存状態と競合ダイアログ
- 認証Cookie
- 日本語IME、キーボード
- ライト／ダーク、狭幅、DPI
- 文言辞書とデザイントークン

## 対象外

- 製品画面の完成
- 実際のMarkdown更新
- Phase 1の共通Component実装
- AIチャット

## 対象ファイルまたは対象モジュール

- `poc/web-ui-react`
- 必要時のみ`poc/web-ui-blazor`
- `poc/web-ui-contract`

## 具体的な実装内容

1. 同一モックAPI契約で最小4画面状態を作る。
2. 編集表の固定列、長文、スクロール、判定入力を検証する。
3. Cookie、アップロード入口、409競合を表示する。
4. 文字列と色・余白を集中管理する。
5. Reactが基準未達の場合だけBlazorを同条件で比較する。

## テスト内容

- Edge、Chrome、WebView2
- 日本語IME、Tab、Enter、Esc、Ctrl+S
- 100、125、150、200% DPI
- ライト／ダーク、Hover、Pressed、Disabled、Focus
- 最小幅、標準幅、広幅
- Playwright screenshotとキーボードテスト

## 受け入れ条件

- 全対象環境で主要操作が消えず、同じ意味で動く。
- Vol.5ガードレールを満たす。
- Reactの採用／不採用と理由をADRへ記録できる。
- UI基盤PoCに業務機能を混在させていない。

## ユーザーが目視確認する内容

- 環境・テーマ・DPI別スクリーンショット。
- 編集表の操作動画または実機デモ。
- React採否の比較表。

## 想定されるリスク

- 表ライブラリ依存が大きくなる。
- WebView2固有のIME、フォーカス、Cookie差異。
- PoC画面が承認済みデザインと誤解される。

## 完了後に更新すべき設計資料

- ADR-003
- 採用技術一覧
- UIテスト方針
- Phase 1 UI基盤前提

## 実装・実測結果（2026-08-02）

### 実装

- `poc/web-ui-react`にReact + TypeScript + Viteの独立UI PoCを追加した。
- `poc/web-ui-contract`にモックAPI境界を定義した。
- チケット一覧・詳細、編集表、保存状態、競合ダイアログ、テーマ切替、狭幅レイアウトを実装した。
- P0-015の検索方式やP0-017の認証実装へ密結合せず、製品APIへ自動昇格しない構成とした。

### 実行コマンド

```powershell
cd .\poc\web-ui-react
npm install
npm run build
npm run dev -- --host 127.0.0.1
```

### 結果

- `npm install`: 成功、脆弱性0件
- `npm run build`: 成功
- 通常幅ブラウザ: 一覧・詳細・編集表を確認
- 820px幅: アイコンレール、2列一覧、1列詳細を確認
- 競合確認: ダイアログ開閉、最新版選択を確認
- 編集保存: 未保存表示、保存ボタン有効化、保存トーストを確認
- テーマ切替: light／darkを確認
- キーボード: Ctrl+S、Escapeを確認

React + TypeScript + Viteを第一採用候補とする。正式採用は、Edge／Chrome／WebView2、100～200% DPI、日本語IMEの対象環境確認後に判断する。

### レビュー待ち事項

- Edge、Chrome、WebView2の実機・実ランタイム確認
- 日本語IME、Tab、Enter、Esc、Ctrl+Sの実環境確認
- 100%、125%、150%、200% DPIでの画面確認
- P0-021の承認済みUI基準画像との最終照合
- 製品API契約確定後のモック差替え
