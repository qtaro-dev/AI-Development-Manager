# P1-016 Web Theme・デザイントークン契約

## 実装正本

`src/Adm.Web/src/styles/tokens.css`をCSS custom propertiesの単一正本とする。コンポーネントの色、余白、radius、focus幅、基準文字サイズは直接値を追加せず、Tokenを参照する。

P0-021の`sidebar`、`topbar`、`content.gutter`は後続のP1-017アプリシェルで利用できるよう、Token契約と値の対応を設計資料へ残す。P1-016では既存の最小画面の構造変更は行わない。

## Theme境界

| モード | 解決方法 |
|---|---|
| `light` | Light意味Tokenを適用 |
| `dark` | Dark意味Tokenを適用 |
| `system` | `prefers-color-scheme`を解決し、変更を監視 |

`ThemeProvider`が`document.documentElement.dataset.theme`を更新する。利用者選択は`adm.theme`へ保存し、未設定時はOS設定を利用する。初期化はReact描画前に行い、初期テーマのちらつきを抑える。P1-016では選択UIを作らず、後続画面からProvider APIを利用できる境界だけを提供する。

## 状態とアクセシビリティ

Primary、success、warning、danger、disabled、focusは意味Tokenとして定義する。状態を色だけで伝えない部品を後続UIが実装できるよう、文言・ARIA・操作をP1-015／P1-014の契約と組み合わせる。focus-visibleにはToken化したfocus ringを適用する。

## 静的検査

```powershell
cd D:\Dev\AI Development Manager\src\Adm.Web
npm run tokens:check
```

必須Tokenの欠落、Light／Dark定義の欠落、CSS内の直接色、Token外のpx値を検出する。breakpoint等の構造上必要な媒体条件は、例外として既存UIのCSSへ残さず、後続でToken境界を拡張する。

## 将来拡張

容量表示のKB／MB／GB／TBとKiB／MiB／GiB／TiB切替、初心者向け用語表示は、表示値と文言辞書の拡張として追加できる。保存値、上限値、内部コード、API契約は変更しない。
