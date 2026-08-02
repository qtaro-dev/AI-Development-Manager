# P1-018 共通状態・フィードバック部品

## 目的

保存、接続、進捗、失敗、競合、警告などの状態を、色だけに頼らず文言・アイコン・次の操作で示す共通UI部品を作る。

## 背景

P0-021で状態表示の共通契約を定めた。業務画面ごとの異なるToastやDialogを防ぐため、機能実装前に状態表現を固定する。

## 前提・依存関係

- P1-014完了
- P1-017完了
- `design/ui/ui-regression-checklist.md`

## 対象範囲

- Status indicator、banner、toast、dialog、progress、empty/error state
- 保存済み、未保存、競合、接続エラー、容量警告、処理中
- 再試行、取消、詳細、閉じる
- ARIA live、focus trap、Escape

## 対象外

- 実保存・アップロード・競合API
- 業務画面
- 自動マージ

## 対象ファイルまたは対象モジュール

- `src/Adm.Web/src/components/feedback`
- Componentテストと表示カタログ

## 具体的な実装内容

1. 各状態の意味、文言キー、色、アイコン、操作を定義する。
2. Modal/Dialogのfocus trap、Escape、フォーカス復帰を実装する。
3. 長時間処理に進捗、取消、失敗理由、再試行を表示できる契約を作る。
4. 競合は最新版・自分の変更・差分を載せられる枠だけを作る。
5. Component表示カタログまたは同等の目視確認入口を作る。

## テスト内容

- 状態ごとの文言・role・aria-live
- 色なしでも意味が分かること
- DialogのTab/Escape/フォーカス復帰
- 進捗・取消・再試行イベント
- 長文エラーと狭幅表示

## 完了条件

- P0-021の主要状態を共通部品で表現できる。
- 色だけで状態を区別しない。
- キーボードとスクリーンリーダー向け意味がある。
- API未接続の表示部品に業務判断を埋め込まない。

## ユーザーが目視確認する内容

- 全状態のlight/dark・標準幅・狭幅表示
- Dialogと進捗の操作
- 一般ユーザー向け日本語

## 想定されるリスク

- Toastだけで重要な失敗を消してしまう
- Dialogがフォーカスを失う
- 共通部品へ業務ロジックが入り巨大化する

## 完了後に更新すべき設計資料

- `design/ui/ui-regression-checklist.md`
- UI状態・部品仕様
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

## 実装結果

- `src/components/feedback/Feedback.tsx`にStatus、Banner、Toast、Progress、Dialog、Empty、Error部品を追加した。
- `FeedbackCatalog.tsx`で保存済み、未保存、競合、接続、容量警告、アップロード、復元、空、エラーの表示見本を提供した。
- 色だけに頼らず、文言、アイコン、`aria-live`、role、次の操作を組み合わせた。
- DialogはTab循環、Escape閉じ、フォーカス復帰を実装した。
- 進捗は`progressbar`と数値、取消／再試行操作を提供した。
- 実保存、アップロード、競合API、自動マージ、業務画面、P1-019以降は対象外とした。

## 画面確認

```powershell
cd D:\Dev\AI Development Manager\src\Adm.Web
npm ci
npm run dev
```

通常は`http://127.0.0.1:5173/`へアクセスする。画面内の「状態表示の見本」で各状態、進捗、取消、再試行、確認ダイアログを確認する。標準幅・狭幅、Light／Dark、`Tab`、`Escape`を確認し、停止は`Ctrl+C`で行う。
