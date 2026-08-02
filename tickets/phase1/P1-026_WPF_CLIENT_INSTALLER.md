# P1-026 WPF Clientインストーラー

## 目的

WPF Clientを標準ユーザー向け別パッケージとしてインストールし、WebView2 Runtime不足時の案内を含めて更新・削除できるようにする。

## 背景

D-15でServerとWPFを別パッケージにすると確定した。WPFは通常利用で管理者権限を要求せず、Serverや業務データを所有しない構成にする。

## 前提・依存関係

- P1-021完了
- P1-023完了
- P1-024完了

## 対象範囲

- WPF per-user package
- WebView2 Runtime確認と案内
- start menu/shortcutの必要最小限
- update、repair、uninstall
- WPF user dataとアプリ本体の分離

## 対象外

- Server同梱
- WebView2 Runtimeの無断システム変更
- 証明書信頼登録
- 業務データ削除

## 対象ファイルまたは対象モジュール

- `installer/wpf-client`
- `src/Adm.Wpf`
- CI package成果物

## 具体的な実装内容

1. 固定Version/Build番号でWPF packageを作る。
2. 標準ユーザーでinstall・起動できるようにする。
3. WebView2 Runtimeを確認し、不足時に公式導入案内を表示する。
4. update/repair/uninstallとuser data保持を実装する。
5. Server packageと独立して導入・削除できることを確認する。

## テスト内容

- 標準ユーザーclean install
- WPF起動とlocalhost Server接続
- WebView2 Runtimeあり/なし
- update、repair、uninstall
- user data保持
- Server未導入・停止時の案内

## 完了条件

- 管理者権限なしでWPF Clientをinstall・通常利用できる。
- ServerとWPFを独立して更新・削除できる。
- Runtime不足とServer接続失敗を一般向け日本語で案内する。
- uninstallでServer・プロジェクト・業務データを変更しない。

## ユーザーが目視確認する内容

- 標準ユーザーでの導入手順
- Runtime不足・Server停止時の画面
- update/uninstall後の状態

## 想定されるリスク

- WPF user data folderが更新で失われる
- Runtime導入が管理者権限を暗黙要求する
- Server packageとのVersion依存が強くなる

## 完了後に更新すべき設計資料

- WPF配布・利用手順
- D-15配布設計
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

## 実装結果

`installer/wpf-client/wpf-client.wixproj`と`Package.wxs`を追加し、固定SDKでWPF Clientをwin-x64 publishした成果物からper-user MSIを生成する`Build-WpfClientInstaller.ps1`を実装した。MSIはLocalAppData配下へClientファイルを配置し、WebView2 Evergreen RuntimeのHKLM／HKCU検査と未導入時の日本語LaunchConditionを含む。WebView2 Runtime、Server、Service、Firewall、証明書、業務データを無断変更しない。

Client UserDataとWebView2 UserDataを配布ファイルから分離し、uninstallでServer・プロジェクト・業務データを削除しない契約を`design/48_WPF_CLIENT_INSTALLER_CONTRACT.md`へ記録した。P1-027の実機install、起動、Runtimeあり／なし、update、repair、uninstall確認には着手していない。

## 再現コマンド

```powershell
pwsh -NoProfile -File .\scripts\installer\Build-WpfClientInstaller.ps1 -Configuration Release
```
