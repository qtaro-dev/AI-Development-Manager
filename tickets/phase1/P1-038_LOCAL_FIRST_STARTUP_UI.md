# P1-038 ローカルファースト起動UI

## 目的

P1-037で承認されたワイヤーフレームを製品UIへ実装し、WindowsアプリがServerを待たずLocalで起動でき、利用者が必要なときだけLAN Serverを設定・再試行できる起動UXを完成させる。

## 背景

設定モデルと画面設計を分離したうえで、実機検証で判明した「接続待ちで利用不能」「設定へ進めない」「終了できない」を解消する。本チケットは承認済み基準画面の実装に限定し、新しいUI基盤や業務機能を作らない。

## 前提・依存関係

- P1-036完了・承認済み
- P1-037のワイヤーフレームがユーザー承認済み
- P1-032～P1-034完了・承認済み
- Vol.5 UIガードレール

## 対象範囲

- Local既定起動と通常画面への遷移
- 実行プロファイルの表示・変更UI
- Server URLの入力、検証結果、保存
- 明示的なServer接続、接続中、失敗、再試行
- 「このPCで続ける」と終了の導線
- 組み込みUI読込失敗時のWPF fallback操作
- 表示文言の日本語辞書化
- 承認済みワイヤーフレームに対する視覚確認

## 対象外

- 新しいUIフレームワーク、テーマ基盤、状態管理基盤への変更
- Server自動起動、探索、インストール
- 認証、権限、証明書配布
- Local／Server業務データ同期
- 業務画面の実装
- Progress、Cancel、Streaming
- MSI、Runtime、配布方式

## 対象ファイルまたは対象モジュール

- `src/Adm.Web/src/`の起動・設定・状態UI
- `src/Adm.Web/src/data-access/`のP1-036操作利用箇所
- `src/Adm.Wpf/`の起動制御とfallback画面
- `src/Adm.Web/src/messages/`等の日本語辞書
- Web unit test、Playwright、WPF UI／起動テスト
- `design/ui/`とP1-038実装結果資料

## 具体的な実装内容

1. 起動時にLocal profileを即時適用し、Server接続を待たず組み込みUIを表示する。
2. 設定UIからLocal／Serverを明示選択し、P1-036の型付き操作で読込・保存する。
3. Server選択時だけ接続を試行し、失敗時も画面全体を塞がずLocalへ戻れるようにする。
4. 接続失敗状態へ「このPCで続ける」「接続先を設定」「もう一度試す」「終了」を実装する。
5. 終了操作はWindows固有操作としてPlatform Bridgeの狭い許可リストへ追加するか、承認済みWPF fallbackで提供し、業務Channelとは分離する。
6. 全文言を辞書キー経由とし、内部例外、URL中の秘密値、パスを表示しない。
7. 基準画像／ワイヤーフレームと実画面を比較し、配置、寸法、余白、色、整列を調整する。

## テスト内容

- 初回・設定なし・Server未導入・Server停止・ネットワーク切断でLocal起動
- Local／Server切替、URL検証、保存、再起動後の復元
- Server失敗からLocal、設定、再試行、終了への各導線
- 組み込みUI読込失敗時の再読込と終了
- キーボード操作、フォーカス、100～200% DPI、狭幅
- 日本語辞書未登録検査と専門用語チェック
- Web unit、Playwright、WPF実機、Architecture、Debug／Release Build

## 成功条件

- Serverが存在しなくてもWindowsアプリが通常利用画面へ進む。
- Serverへの接続は利用者が明示選択した場合だけ行う。
- 接続失敗時にLocal利用が妨げられない。
- 設定、再試行、終了の全導線が機能する。
- 実画面が承認済みワイヤーフレームの主要レイアウトと視覚バランスに一致する。

## 完了条件

- 実装、全テスト、Windows 11実機確認、画面比較、設計更新が完了している。
- 差分画像または画面キャプチャと確認記録が残っている。
- P1-039以降を実装していない。

## ユーザーが目視確認する内容

- Serverなしの初回起動からLocal画面まで
- 設定画面とServer接続失敗時の全導線
- 終了と再起動後の設定復元
- 基準ワイヤーフレームとの配置・寸法・余白・色・整列

## 想定されるリスク

- 起動処理が裏でServer応答を待ち、Local表示を遅らせる。
- Platform Bridgeへ汎用終了・コマンド実行機能を追加する。
- 設定失敗で全画面を操作不能にする。
- ワイヤーフレーム未承認の変更を実装へ混ぜる。
- UI文言をコンポーネントへ直書きする。

## 完了後に更新すべき設計資料

- `design/00_INDEX.md`
- `design/21_WEB_UI_CONTRACT.md`
- `design/24_UI_WIREFRAMES_CONTRACT.md`
- `design/40_WEB_APP_SHELL_CONTRACT.md`
- `design/43_WPF_WEBVIEW2_SHELL_CONTRACT.md`
- `design/44_WPF_BRIDGE_CONTRACT.md`
- `design/ui/`の基準画面・回帰資料
- P1-038実装結果資料
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`
- 本チケット

## 完了時に残す証拠

- 実画面キャプチャと基準比較
- 全状態遷移の実機結果
- Server非依存起動と待受なしの確認
- Web、Playwright、WPF、Architecture、Build結果
- `dotnet --version`と`git diff --check`結果

## 実装結果

実装済み。P1-037で承認されたワイヤーフレームを基準に、Webの初回Local起動・実行プロファイル設定・接続失敗導線と、WPFの読込／接続失敗fallbackを実装した。

- 初回または未設定時だけ起動案内を表示し、設定済みの通常起動はLocalホームへ直接遷移する。
- Local選択時はServer URLを無効化し、LAN Server選択時だけHTTPS URLを有効化する。
- Server未導入・停止・接続失敗時もLocalで続ける、設定確認、再試行、終了を選択できる。
- WebView2読込失敗時はWPF fallbackへ切り替える。Platform Bridgeへ新しい操作は追加していない。
- 詳細な検証結果は`design/ui/p1-038-local-first-startup-ui.md`へ記録した。

## 状態

実装済み。P1-039以降は未着手。
