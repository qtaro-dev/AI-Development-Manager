# P1-025 Serverインストーラー・Service登録

## 目的

ASP.NET Core Serverを管理者向けパッケージで安全にインストールし、Windows Serviceとして登録・更新・削除できるようにする。

## 背景

Windows Serviceが正式運用方式であり、P0-002/P1-012でHost境界を確認した。実運用にはService登録、権限、ログ・設定領域、更新・削除の実機確認が必要である。

## 前提・依存関係

- P1-005完了
- P1-008完了
- P1-011完了
- P1-012完了
- P1-024完了

## 対象範囲

- Server package作成
- per-machine install
- Windows Service登録、開始、停止、再起動
- アプリ、設定、ログ、データ領域の分離
- update、repair、uninstall

## 対象外

- Firewall、HTTPS証明書、LAN公開
- ユーザー・プロジェクトデータ
- WPF Client
- 自動更新配信

## 対象ファイルまたは対象モジュール

- `installer/server`
- `src/Adm.Server.Host`
- `src/Adm.Infrastructure.Windows`
- CI package成果物

## 具体的な実装内容

1. 固定Version/Build番号でServer packageを作る。
2. UAC確認後にWindows Serviceを登録する。
3. 実行ファイル、設定、ログ、将来データ領域を分離する。
4. 更新前停止、ファイル更新、再起動、失敗時案内を実装する。
5. uninstallで業務データ・設定を無断削除しない。
6. localhost限定起動とhealth確認をインストール検証へ含める。

## テスト内容

- clean install、Service開始・停止
- 標準ユーザーからの通常状態確認
- 同版repair、上位版update、下位版扱い
- 使用中ファイル・起動失敗
- uninstallと残置データ確認
- install前後のlocalhost限定確認

## 完了条件

- Windows 11 64-bitでinstall/update/repair/uninstallを再現できる。
- Windows Serviceが同一Hostを使用しhealthを返す。
- Firewall・証明書を無断変更せずlocalhost限定を維持する。
- uninstallで設定・将来の業務データを無断削除しない。
- 失敗時に復旧または再試行手順を示す。

## ユーザーが目視確認する内容

- インストール画面とUACタイミング
- Windows Service状態
- 更新・削除後の残置内容

## 想定されるリスク

- Serviceアカウントが設定・ログへアクセスできない
- 更新失敗でServiceが起動不能になる
- uninstallが利用者データを消す

## 完了後に更新すべき設計資料

- Server配布・Service運用手順
- ADR-012
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

## 実装結果

`installer/server/server.wixproj`と`Package.wxs`を追加し、固定SDKでServerをwin-x64 publishした成果物からper-machine MSIを生成する`Build-ServerInstaller.ps1`を実装した。MSIは`AIDevelopmentManagerServer`をLocalService・自動開始で登録し、`ServerHostFactory`と既存のlocalhost限定Hostを利用する。Config／Logs／DataはProgramData配下へ分離し、Service停止・登録解除後も内容を無条件削除しない。

対象外のFirewall、HTTPS証明書、LAN公開、WPF Client、WebView2 Runtime、業務データ処理は追加していない。MSI buildと`wix msi validate`は成功したが、Windows Installerによる実機installはInstaller側の停止で完遂できなかった。Service開始・停止、health、update、repair、uninstallのWindows実機確認はP1-027へ引き継ぐ。

## 再現コマンド

```powershell
pwsh -NoProfile -File .\scripts\installer\Build-ServerInstaller.ps1 -Configuration Release
```
