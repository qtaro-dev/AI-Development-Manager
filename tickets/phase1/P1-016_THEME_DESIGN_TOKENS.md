# P1-016 Theme・デザイントークン基盤

## 目的

P0-021で承認された寸法・色・文字・余白を一つのToken正本として実装し、ライト／ダークThemeで共有する。

## 背景

UI実装ごとの任意色・余白を防ぎ、基準画像受領後の調整をToken変更へ集約する必要がある。

## 前提・依存関係

- P1-013完了
- `design/ui/design-tokens.md`
- `design/24_UI_WIREFRAMES_CONTRACT.md`

## 対象範囲

- CSS design tokens
- light/dark Theme
- spacing、radius、focus、typography、state color
- OS設定・利用者選択の適用境界
- contrast基礎確認

## 対象外

- 個別業務画面
- 最終基準画像に対する全画面調整
- 独自アイコンセット制作

## 対象ファイルまたは対象モジュール

- `src/Adm.Web/src/styles/tokens`
- `src/Adm.Web/src/theme`
- Themeテスト

## 具体的な実装内容

1. P0-021のTokenをCSS custom properties等の単一正本へ移す。
2. light/darkの意味Tokenを実装する。
3. focus、hover、pressed、disabledの状態Tokenを定義する。
4. Theme初期化時のちらつきと永続化境界を設計する。
5. 直接色・任意余白の追加を検出する規則を作る。

## テスト内容

- Token欠落・重複
- light/dark切替
- 主要文字と状態色のcontrast
- Focus ring表示
- 直接色・未承認寸法の静的検査

## 完了条件

- P0-021 Tokenが一つの実装正本から参照される。
- light/darkで状態の意味が変わらない。
- 色だけで状態を表現しない部品を作れる。
- 基準画像調整を個別画面の一括書換えなしで反映できる。

## ユーザーが目視確認する内容

- light/darkのToken見本
- focus、disabled、warning、danger状態
- P0-021値との対応表

## 想定されるリスク

- Tokenと既定ブラウザスタイルの競合
- 小さすぎる文字をそのまま採用する
- 任意値がコンポーネントへ散在する

## 完了後に更新すべき設計資料

- `design/ui/design-tokens.md`
- `design/24_UI_WIREFRAMES_CONTRACT.md`
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

## 実装結果

- `src/styles/tokens.css`をToken実装正本とし、P0-021の寸法、余白、radius、focus、文字、意味色を集約した。
- `ThemeProvider`へ`light`／`dark`／`system`、OS設定追従、`adm.theme`永続化境界を追加した。
- 既存最小画面のCSSをToken参照へ変更し、直接色・任意px値を除外した。
- `tokens:check`で必須Token、Light／Dark、直接色、Token外px値を検査し、CI品質ゲートへ追加した。
- Themeテストでlight→dark切替、system判定、文書表示の維持を確認した。
- 選択UI、最終基準画像調整、個別業務画面、P1-017以降の実装は対象外とした。

## 再現コマンド

```powershell
cd D:\Dev\AI Development Manager\src\Adm.Web
npm ci
npm run tokens:check
npm run test
```
