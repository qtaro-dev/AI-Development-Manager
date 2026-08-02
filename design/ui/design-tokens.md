# P0-021 デザイントークン

版: 1.0-p0-021
状態: 仮基準、ユーザー承認済み

Phase 1では、この表の値を`src/Adm.Web/src/styles/tokens.css`へ実装正本として反映する。Light／Darkの意味Tokenは同じ名前を共有し、値だけをThemeごとに切り替える。

## 寸法

| Token | 値 | 用途 |
|---|---:|---|
| `sidebar.standard` | 248px | 標準幅の左ナビゲーション |
| `sidebar.narrow` | 64px | 820px幅のアイコンレール |
| `topbar` | 71px | 標準幅の上部バー |
| `content.gutter` | 43px | 標準幅の本文左右余白 |
| `content.gutter.narrow` | 18px | 狭幅の本文左右余白 |
| `space.1` / `2` / `3` / `4` | 4 / 8 / 12 / 16px | 基本間隔 |
| `space.5` / `6` / `8` | 20 / 24 / 32px | パネル・セクション間隔 |
| `radius.control` | 8px | ボタン・入力 |
| `radius.panel` | 11px | パネル |
| `focus.ring` | 3px | キーボードフォーカス |

## 色

| Token | Light | Dark | 用途 |
|---|---|---|---|
| `color.bg` | `#F6F7FB` | `#151924` | ページ背景 |
| `color.surface` | `#FFFFFF` | `#202534` | パネル・上部バー |
| `color.text` | `#172136` | `#F2F4FB` | 本文 |
| `color.muted` | `#7B8496` | `#A5AEC2` | 補助情報 |
| `color.border` | `#E6E9F0` | `#333B50` | 境界線 |
| `color.primary` | `#5964D9` | `#8D96FF` | 主操作・選択 |
| `color.success` | `#2D9D70` | `#5BD39D` | 保存済み・合格 |
| `color.warning` | `#B97918` | `#F0BB5A` | 未保存・容量警告 |
| `color.danger` | `#D6575D` | `#FF8589` | 競合・エラー |

## 文字

- 日本語: `Noto Sans JP`、英数字: `DM Sans`を第一候補とする。
- 本文は11〜12px、画面見出しは25px、パネル見出しは15pxを基準とする。
- 小さい補助文字は9〜10pxまでとし、色だけで意味を区別しない。
- アイコンボタンには必ず日本語の`aria-label`または可視ラベルを付ける。

## 将来の容量表示単位

設定で`KB / MB / GB / TB`または`KiB / MiB / GiB / TiB`を選択できるようにする要望を記録する。ただし表示形式のみで、保存値・上限値は変更しない。基本機能完成後に実装する。
