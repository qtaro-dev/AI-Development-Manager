# P1-017 レスポンシブ・アプリシェル

## 目的

全業務画面が共有する左ナビゲーション、上部バー、本文、主操作領域を、標準幅・狭幅で一貫して表示する。

## 背景

P0-021では1440px、820px、320px以上のレイアウト基準を定めた。業務画面より先に共通シェルを固定し、主要操作が狭幅で消える退行を防ぐ。

## 前提・依存関係

- P1-015完了
- P1-016完了
- `design/ui/screen-inventory.md`
- `design/ui/wireframes/`

## 対象範囲

- 左ナビ、上部バー、本文、アクション領域
- Route outletとページタイトル
- 1440px、820px、320px以上
- skip link、Tab順、フォーカス移動
- 接続状態表示の予約領域

## 対象外

- プロジェクト・チケット等の業務画面
- 認証画面
- WPF固有ボタンの実機能

## 対象ファイルまたは対象モジュール

- `src/Adm.Web/src/app-shell`
- `src/Adm.Web/src/routes`
- 関連テスト

## 具体的な実装内容

1. 標準幅の248px sidebar、71px topbar、43px gutterを実装する。
2. 820pxで64px icon railと1列本文へ切り替える。
3. 320px以上で主操作、戻る、取消領域を画面内に保つ。
4. ページ見出しから主要内容、次の操作への読み順を固定する。
5. キーボードだけで主要領域へ到達できるようにする。

## テスト内容

- 1440/820/320pxレイアウト
- Tab順、skip link、フォーカス復帰
- 長い日本語ページ名
- light/dark
- 主操作領域の表示維持

## 完了条件

- P0-021の主要寸法と情報構造を満たす。
- 狭幅でも主要操作が非表示・到達不能にならない。
- 一画面一責務のRoute枠を提供する。
- 業務機能の仮実装を含まない。

## ユーザーが目視確認する内容

- 標準幅・820px・320pxの代表画面
- ナビゲーション縮小と本文1列化
- キーボード移動

## 想定されるリスク

- アイコンだけで意味が分からなくなる
- 固定幅がDPIや長い日本語で崩れる
- 仮メニューを業務仕様として固定する

## 完了後に更新すべき設計資料

- `design/ui/screen-inventory.md`
- `design/ui/ui-regression-checklist.md`
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

## 実装結果

- `src/app-shell/AppShell.tsx`と`src/routes/RouteOutlet.tsx`を追加した。
- 248px sidebar、71px topbar、43px gutter、最大本文幅1640pxをToken参照で実装した。
- 900px以下で64px icon rail、57px topbar、本文1列へ切り替えた。
- 320px以上で本文、skip link、Reserved action領域、主要ナビを保持した。
- 長い日本語ページ名、`aria-current`、ナビゲーションARIA、Tab順をテストした。
- 業務画面、認証、WPF固有機能、P1-018以降の状態部品は対象外とした。

## 画面確認

```powershell
cd D:\Dev\AI Development Manager\src\Adm.Web
npm ci
npm run dev
```

通常は`http://localhost:5173/`で開く。ポート使用中はViteが表示するURL（例：`http://localhost:5174/`）を使用する。1440px、820px、320pxで、標準sidebar、icon rail、本文1列、skip link、ナビゲーション、長い見出しを確認する。停止は`Ctrl+C`。
