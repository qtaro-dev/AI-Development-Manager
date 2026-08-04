# P1-044 初回設定の保存・再読込・保存後画面遷移の是正

## 目的

P1-041のP1-043修正版クリーンVM再試験で確認された、初回設定の保存・再読込・保存後画面遷移の不整合を是正する。

## 対象

- 初回設定値の保存
- 次回起動時の設定再読込
- 保存完了後のLocalホームまたは選択プロファイル画面への遷移
- 保存失敗時の利用者向け案内と再試行導線
- 設定値の破損・欠落時の安全なLocal復帰

## 対象外

- Server、Windows Service、LAN接続
- 業務機能、認証、プロジェクト登録
- P1-041試験結果の書き換え

## 完了条件

- 初回設定を保存できる。
- アプリ再起動後に保存値を再読込できる。
- 保存成功後に意図した画面へ一度だけ遷移する。
- 保存失敗時に入力内容を保持し、安全に再試行できる。
- Local既定と明示的Server設定の境界を維持する。
- Build、Test、Architecture検査、Windows実機確認を実施する。

## 実施時期・優先順位

Phase 2開始時に判断する。本チケットでは実装しない。

## 状態

再オープン。クリーンVMで初回設定ボタン押下後の応答・保存・遷移が未解決だったため、Local ChannelのWebView2文字列メッセージ送受信を是正中。

## 実装結果

- 実行プロファイル読込結果へ`hasPersistedProfile`を追加し、永続設定の存在をWeb UIへ明示した。
- 有効な保存済みプロファイルがある場合、LocalStorageの状態に依存せず初回画面を再表示せずLocalホームへ遷移する。
- Local／Serverいずれの保存成功時も初回起動済み状態を保存し、次回起動の通常遷移を統一した。
- Local設定の保存失敗時はホームへ遷移せず、入力画面にエラーを表示して再試行できるようにした。
- 保存後は成功応答を受けてから画面遷移し、WPF側の既存Server切替経路と整合させた。

## 検証結果

- Debug／Release Build: 成功、警告0・エラー0
- .NET Test: 74件成功
- Web Test: 40件成功
- Web typecheck／format: 成功
- Web DataAccess／Message／Token／Bundle検査: 成功
- Architecture検査: 成功
- Windows実機確認: クリーンVMで未解決。修正後に再確認する。

## 再調査結果

- Web側Local Channelは`window.chrome.webview.postMessage`へJSON文字列を渡しているが、WPF側が`WebMessageAsJson`を直接解析していた。文字列メッセージではJSON値のラッピングが発生し、契約Requestとして処理されない経路になっていた。
- WPF側の応答も、JSON文字列を`PostWebMessageAsJson`へ渡していたため、Web側の`MessageEvent.data`がLocalChannelClientの期待する文字列にならず、要求Promiseが完了しない経路になっていた。
- Local ChannelはWPF側で`TryGetWebMessageAsString()`、応答で`PostWebMessageAsString()`を使用するよう修正し、Platform Bridgeのオブジェクト送受信とは分離する。
- 保存先は`%LOCALAPPDATA%\AI Development Manager\Config\execution-profile.json`。保存は一時ファイルへ書込み・Flush後に初回はMove、既存時はReplaceする。
- 保存完了応答をWeb側が受信してから`setView("home")`し、再起動時は同じファイルを読み、`hasPersistedProfile`で初回画面を抑止する。

## 追加再調査結果（終了操作）

クリーンVMで初回設定画面下部の「終了」が反応しなかったため、完了扱いにせず再オープンを継続する。

- 原因は、Web UIが`window.close()`のみを呼んでいたこと。WebView2へ埋め込まれたページはブラウザから起動した新規ウィンドウではないため、`window.close()`ではWPFプロセスを終了できない。
- Web UIの上部・下部の終了操作は、WebView2埋め込み時に文字列メッセージ`"exit"`を送信する方式へ統一した。ブラウザ単体では従来どおり`window.close()`へフォールバックする。
- WPF側はLocal WebViewの許可originを検証したうえで、完全一致する`"exit"`だけをUIスレッドへ渡して`Close()`する。Local ChannelのJSON要求とは混在させない。
- 上部ヘッダーのWPF終了ボタンは既存の`ExitButton_Click`から同じ`Close()`経路を使用する。
- Web単体テストで文字列メッセージ送信とブラウザフォールバックを追加確認した。WPFプロセス残留を含むクリーンVM確認は修正版MSIで再試験待ちのため、P1-044は未完了とする。

## 終了操作修正版成果物

- MSI: `artifacts/packages/client/ja-JP/AI-Development-Manager-Client-0.1.0-1-x64.msi`
- SHA-256: `723E8101CFEA0B22DB309453780025B91198DBE5DF867A16A149D27933DF95E1`
