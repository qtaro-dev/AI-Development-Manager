# UIワイヤーフレーム・基準契約（P0-021）

## 状態

仮基準としてユーザー承認済み。P0-018のReact + TypeScript + Vite PoCと既存のUI設計を参照して、主要画面の責務・レイアウト・状態・操作導線を固定した。フォントサイズ、余白、アイコン、ボタンサイズなどの細かな調整はPhase 1の実装・実機確認で行う。

## 共通契約

- 共通WebとWPF内WebView2は同じ情報構造・文言・状態を表示する。
- 標準幅は左ナビゲーション248px、上部バー71px、本文左右43pxを基本とする。
- 820px幅では左ナビゲーションを64pxへ縮小し、一覧と詳細を1列へ切り替える。検索、保存、取消、競合確認などの主要操作を隠さない。
- 一画面一責務とし、主操作は右上または下部アクション領域へ集約する。
- 保存済み、未保存、競合、接続エラー、容量警告、復元検証中を文言・色・操作で区別する。
- WPF固有操作は共通操作と分離して補助表示し、Webのみで利用できる操作をWPF限定と誤認させない。

## 画面と状態

画面の責務・寸法・次の操作は`design/ui/screen-inventory.md`、色・余白・文字は`design/ui/design-tokens.md`、視覚配置は`design/ui/wireframes/`を正本とする。

## 将来要望（今回未実装）

容量表示単位を設定で十進（KB / MB / GB / TB）または二進（KiB / MiB / GiB / TiB）から選択する。表示形式のみを変更し、保存値・上限値は変更しない。基本機能完成後に実装する。

## 受入確認

- 標準幅・狭幅・状態別SVGのXMLが妥当である。
- 全9画面に主目的、主操作、次の操作、WPF固有操作が定義されている。
- キーボード操作、テーマ、DPI、狭幅、主要状態を回帰チェックリストへ登録している。
- 基準画像受領後に、主要レイアウトと視覚バランスの差分を確認する。細部はPhase 1で調整する。

## P1-016実装結果

`src/Adm.Web/src/styles/tokens.css`を寸法、余白、radius、focus、文字、意味色の実装正本とした。`:root`をLight、`:root[data-theme="dark"]`をDarkとして同じ意味Tokenを共有し、`ThemeProvider`が`light`／`dark`／`system`を解決する。利用者選択の永続化は`localStorage`の`adm.theme`に限定し、秘密情報や業務データは保存しない。最終基準画像への全画面調整、個別業務画面、独自アイコンは対象外とした。

## P1-037起動・接続・設定ワイヤーフレーム

P1-037では、P0-021の共通シェルを前提に、Local既定起動、Server接続失敗、実行プロファイル設定、Web UI読込失敗時のWPF fallbackを追加の設計ゲートとして整理した。P1-037専用SVGは`design/ui/wireframes/p1-037/`、状態遷移と文言は`design/ui/p1-037-transition-and-copy.md`を正本とする。製品コード変更、Server自動起動、認証、業務画面は含めない。

## P1-023実装結果

Edge／Chromeで標準production Web buildを実行し、レスポンシブshell、Theme、Dialog、キーボード、focus復帰、deep linkの互換検査を追加した。DPI相当値は自動検査とWindows表示倍率の実機確認を区別し、WebView2はRuntime版とWPF Shell起動を別証拠として記録する。
