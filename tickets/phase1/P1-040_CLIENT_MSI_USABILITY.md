# P1-040 Client MSI操作性修正

## 目的

P1-039のSelf-contained成果物を標準ユーザーが迷わずインストール・起動・更新・削除できるClient MSIへ組み込み、実機検証で判明したShortcut、アイコン、製品情報、Runtime案内の問題を解消する。

## 背景

P1-026 MSIは配置に成功したが、スタートメニューから起動できず、製品アイコンやRuntime前提も不十分だった。本チケットはClientインストーラーの操作性と保守動作だけを扱い、製品機能やServer MSIを変更しない。

## 前提・依存関係

- P1-039完了・承認済み
- P1-024、P1-026の設計・実装・実機結果
- Clientは標準ユーザー向けper-user MSI

## 対象範囲

- P1-039 Self-contained publish物のMSI収録
- スタートメニューShortcut
- MSI、ARP、Shortcut、実行ファイルの製品アイコン
- 製品名、発行元、版数、サポート情報
- install、repair、upgrade、uninstall
- WebView2前提条件の検出と分かりやすい案内
- ユーザーデータと設定を通常uninstallで保持する規則
- インストールログと失敗理由

## 対象外

- Server MSI、Service、Firewall、証明書
- .NET Desktop RuntimeのBootstrapper
- 既定のデスクトップShortcut
- 自動更新、オンラインDownload、コード署名の実運用
- WPF／Web UI機能変更
- ユーザーデータ削除機能

## 対象ファイルまたは対象モジュール

- `installer/`のClient MSI定義
- Client publish成果物取込処理
- 製品アイコン資産とインストーラー資産
- MSIビルド・検査スクリプト、CI
- `design/46_INSTALLER_DISTRIBUTION_CONTRACT.md`
- `design/48_WPF_CLIENT_INSTALLER_CONTRACT.md`
- P1-040実機確認資料

## 具体的な実装内容

1. MSIの入力をP1-039の検証済みSelf-contained成果物へ固定する。
2. スタートメニューへ製品Shortcutを追加し、作業フォルダ、アイコン、削除動作を設定する。
3. デスクトップShortcutは既定で作成しない。将来必要なら別途ユーザー選択式として設計する。
4. MSI、ARP、Shortcut、実行ファイルで一貫した製品名、発行元、版数、アイコンを使う。
5. .NET Runtime前提条件を削除し、WebView2だけを検出して不足理由と導入手順を案内する。
6. install、repair、同一版、upgrade、downgrade拒否、uninstallの動作とログを整える。
7. 通常uninstallではユーザー設定、`.adm-meta`、業務データ、WebView2 UserDataを無断削除しない。

## テスト内容

- 標準ユーザーでのinstallと管理者要求有無
- スタートメニュー検索・起動、Shortcutアイコン
- ARPの製品情報、修復、削除
- 同一版修復、上位版upgrade、下位版拒否
- .NET Runtime未導入環境での起動
- WebView2不足時の案内
- uninstall後の製品ファイル除去とユーザーデータ保持
- MSIログに秘密情報がないこと
- ICE等のMSI静的検査とCIビルド

## 成功条件

- スタートメニューから製品名とアイコンで起動できる。
- .NET Runtime未導入でも起動できる。
- install、repair、upgrade、uninstallが意図した権限で成功する。
- 通常uninstallが業務データや利用者設定を削除しない。
- 失敗時に理由とログ取得方法が分かる。

## 完了条件

- MSI実装、静的検査、更新経路、実機試験、設計更新が完了している。
- 生成MSIの版数、ハッシュ、ログ、画面キャプチャが残っている。
- Server MSIの修正を混在させていない。

## ユーザーが目視確認する内容

- インストーラー、スタートメニュー、ARPの名称とアイコン
- スタートメニューからの起動
- WebView2不足時とインストール失敗時の案内
- upgrade／uninstall後の状態

## 想定されるリスク

- Framework-dependent成果物を誤って収録する。
- Shortcutが一時パスや誤った作業フォルダを参照する。
- uninstallでユーザーデータを削除する。
- Server MSIの課題を同時修正して検証範囲を広げる。
- アイコンのサイズ・形式不足で表示が崩れる。

## 完了後に更新すべき設計資料

- `design/00_INDEX.md`
- `design/46_INSTALLER_DISTRIBUTION_CONTRACT.md`
- `design/48_WPF_CLIENT_INSTALLER_CONTRACT.md`
- P1-040 Client MSI実機資料
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`
- 本チケット

## 完了時に残す証拠

- MSI、ハッシュ、製品版
- install／repair／upgrade／uninstallログ
- スタートメニュー、ARP、Runtime案内の画面キャプチャ
- ユーザーデータ保持確認
- MSI静的検査、`git diff --check`結果

## 実装結果

実装済み。P1-039のSelf-contained Clientを入力として、per-user MSIのスタートメニュー起動、製品アイコン、ARP情報、WebView2前提条件、データ保持境界を整備した。

- スタートメニューに「AI Development Manager」ショートカットを作成し、Client配置先の`Adm.Wpf.exe`をTargetにする。
- デスクトップショートカットは作成しない。
- MSI、ARP、Shortcut、WPF実行ファイルで決定的な製品アイコンと製品名を使用する。
- .NET Desktop Runtimeの前提条件は設けず、WebView2 Evergreen Runtimeだけを検出する。
- repair／upgrade／uninstallのMSI入口を維持し、通常uninstallでUserData、`.adm-meta`、業務データ、WebView2 UserData、Serverデータを削除しない。
- `msiexec /l*v`による詳細ログ取得方法を設計資料へ記録した。

## 検証結果

- Product.wxsのXML妥当性：成功
- 製品アイコン生成：成功（32x32 ICO、4,414 bytes）
- Debug／Release Build：成功、警告0・エラー0
- Web Test：既存38件成功
- .NET Test：Debug／Release各74件成功
- Architecture検査：Debug／Release成功
- WiX MSIの最終ビルドは、WiX SDK取得時のNuGet SSL／認証環境エラーにより実機生成未完了。MSI実機install／repair／upgrade／uninstallはP1-041以降のクリーン環境確認へ引き継ぐ。

## 状態

実装済み。P1-041以降は未着手。
