# P1-018 Web状態・フィードバック部品契約

## 部品一覧

| 部品 | 用途 | 利用者向け意味 |
|---|---|---|
| `StatusIndicator` | 保存、接続、競合、警告、処理中の短い状態 | 文言・アイコン・意味色を併記 |
| `FeedbackBanner` | 重要な警告、失敗、競合 | 内容、説明、追跡ID、次の操作 |
| `Toast` | 保存確認など短い通知 | `role=status`、閉じる、任意の操作 |
| `ProgressFeedback` | アップロード・復元などの長時間処理 | `progressbar`、進捗、取消、再試行 |
| `FeedbackDialog` | 確認や詳細表示 | `role=dialog`、focus trap、Escape |
| `EmptyState` / `ErrorState` | 結果なし、再試行可能な失敗 | 文言と次の操作 |

## 状態契約

保存済み、未保存、競合、接続中、接続エラー、容量警告、アップロード中、復元検証中を共通部品で表現する。状態を色だけで判断させず、一般語の文言、補助アイコン、必要な操作を併記する。表示文言はP1-015の`message()`から参照する。

競合部品は最新版、自分の変更、差分を表示できる枠と次の操作を受け取るが、自動マージやAPI呼出しは行わない。エラー部品は追跡IDを任意表示できるが、例外本文や秘密情報を表示しない。

## キーボード・ARIA

- Status／Toastは`role=status`と`aria-live="polite"`を基本とする。
- 重要な失敗は`role=alert`と`aria-live="assertive"`を使用する。
- 進捗は`role=progressbar`、`aria-valuemin`、`aria-valuemax`、処理中の`aria-valuenow`を持つ。
- Dialogは`aria-modal="true"`、見出し参照、Tab循環、Escape閉じ、閉じた後のフォーカス復帰を持つ。
- 取消、再試行、閉じる、詳細などの操作は日本語の可視ラベルまたはARIA labelを持つ。

## 表示カタログ

`FeedbackCatalog`は業務データを読み書きせず、各状態の表示と操作を確認するための見本である。開発サーバーでLight／Dark、標準幅／狭幅を目視確認できる。P1-018では実保存、アップロード、競合API、自動マージ、業務画面を実装しない。
