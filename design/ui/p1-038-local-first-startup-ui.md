# P1-038 ローカルファースト起動UI実装結果

## 実装基準

P1-037で承認されたLocal既定、Server接続失敗時の継続導線、設定画面、WebView2読込失敗時のWPF fallbackを製品UIへ反映した。

## 実装結果

- 初回または未設定時だけ起動案内を表示し、Localを選ぶと次回以降はLocalホームへ直接遷移する。
- Local選択時はServer URLを無効化し、LAN Server選択時だけHTTPS URL入力と保存を有効にする。
- Server接続失敗時は、Localで続ける、設定確認、再試行、終了をWeb UIとWPF fallbackで提供する。
- WebView2資産・初期化失敗時はWPF側の再試行・設定・Local継続・終了導線へフォールバックする。
- Local Application Channelを介して実行プロファイルをWPFへ伝達し、Platform Bridgeの許可操作は変更していない。

## 検証証拠

- Web unit: 初回Local遷移、Local時URL無効、Server時URL有効、HTTPS検証、接続失敗導線を確認。
- WPF実画面: Server非依存の初回Local設定画面を起動し、承認済みワイヤーフレームの見出し、説明、Local主操作、設定、終了導線を確認。
- WPFは引数なし起動時にlocalhostポートを待ち受けず、固定仮想HTTPS originのWeb資産を表示した。
