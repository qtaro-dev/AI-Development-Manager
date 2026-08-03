# P1-017 Webアプリシェル契約

## 構成

`AppShell`が共通レイアウトを所有し、`RouteOutlet`がページタイトルと本文だけを差し込む。業務画面はこの枠を利用するが、P1-017では画面固有の一覧、詳細、編集、認証を実装しない。

```text
AppShell
├─ skip link
├─ sidebar: brand / workspace / navigation / connection / user
├─ topbar: breadcrumb / reserved actions
└─ main: page heading / reserved actions / route outlet
```

## 寸法契約

| 条件 | sidebar | topbar | 本文 |
|---|---:|---:|---|
| 標準幅（1440px） | 248px | 71px | 左右43px、最大1640px |
| 狭幅（820px） | 64px icon rail | 57px | 左右18px、一覧・詳細1列 |
| 最小幅（320px以上） | 64px icon rail | 57px | 主操作Reserved領域を保持 |

値は`src/Adm.Web/src/styles/tokens.css`を参照し、CSSへ任意の寸法を追加しない。900pxを狭幅切替の媒体条件とし、520pxではbreadcrumbを簡略化する。

## アクセシビリティ

- 最初のTabで「本文へ移動」skip linkへ到達できる。
- `main`へ`tabIndex=-1`を設定し、ページ本文のフォーカス移動先を固定する。
- navは日本語のARIA labelを持ち、現在ページは`aria-current="page"`で表す。
- 狭幅のicon railでも可視アイコンとARIA labelを併用し、意味をアイコンだけに依存しない。
- ページ見出し、説明、本文、次の操作Reserved領域の読み順を固定する。

## 確認コマンド

```powershell
cd D:\Dev\AI Development Manager\src\Adm.Web
npm run test
npm run build
```

実機確認は`npm run dev`で1440px、820px、320pxを確認する。P1-017では実ブラウザ／WebView2差異と最終基準画像の調整は確定しない。

## P1-038 ローカルファースト起動UI

組み込みLocal modeでは、`localStorage`の`adm.startup.localAcknowledged`が未設定の初回だけ起動案内を表示する。設定済みの場合はLocalホームへ直接遷移し、Server接続を待たない。

実行プロファイル設定では、Local選択時のServer URL入力を無効化し、LAN Server選択時だけHTTPS URLを入力・保存できる。接続失敗時もLocal継続、設定、再試行、終了の操作を表示する。Web UIはP1-036のDataAccess Portだけを利用し、HTTP APIやWPF Bridgeを直接参照しない。
