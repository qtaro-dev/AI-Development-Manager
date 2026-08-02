# P1-024 配布・インストーラー方式ADR

## 目的

Server管理者向けとWPF標準ユーザー向けを別パッケージにするため、Phase 1成果物に適したインストーラー方式、更新、削除、署名経路を確定する。

## 背景

配布をServer/WPF別パッケージにする方針は確定しているが、インストーラー実装技術はPhase 0で固定していない。採用済みの製品アーキテクチャを変更せず、配布実装のADRを作る。

## 前提・依存関係

- P1-012完了
- P1-020完了
- D-03、D-15

## 対象範囲

- Server per-machine管理者インストール
- WPF Client per-user標準ユーザーインストール
- install、update、repair、uninstall
- Windows Service登録境界
- WebView2 Runtime前提確認
- 署名・版・ロールバック経路

## 対象外

- 公開配布
- 正式コード署名証明書購入
- HTTPS証明書・Firewall設定
- 自動更新Server

## 対象ファイルまたは対象モジュール

- 新規配布ADR
- `installer/`予定構成
- Build/CI成果物設計

## 具体的な実装内容

1. Windows 11/.NET 10/WPF/Serviceに対応する候補を比較する。
2. per-machine/per-user、権限、Service、更新、修復、削除を評価する。
3. Server/WPFを別パッケージにする構成を決める。
4. 開発署名と将来正式署名の差込境界を決める。
5. 採用、不採用、制約、見直し条件をADRへ記録する。

## テスト内容

- 最小packageの試作
- 標準ユーザー/管理者の権限境界
- downgrade拒否または明示扱い
- upgrade/repair/uninstallの実現可能性
- silent optionの安全な範囲

## 完了条件

- ServerとWPFの採用インストーラー方式がADRで一意に決まる。
- 権限、更新、削除、Service、Runtime、署名経路を説明できる。
- Firewall・証明書変更を無断実行する設計になっていない。
- P1-025/P1-026が追加判断なしで実装開始できる。

## ユーザーが目視確認する内容

- 候補比較表と採用理由
- Server/WPFパッケージ境界
- 管理者権限が必要な操作一覧

## 想定されるリスク

- インストーラー技術比較が過剰に広がる
- per-user WPFが管理者権限を要求する
- アンインストールで利用者データを削除する

## 完了後に更新すべき設計資料

- `design/02_TECHNOLOGY_AND_ADR.md`
- 配布・更新設計
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

## 実装結果

ServerとWPF Clientを別パッケージとし、両方の配布方式にWiX Toolset v4のMSIを採用するADRを確定した。Serverは管理者向けper-machine、WPF Clientは標準ユーザー向けper-userとし、Service、Firewall、証明書ストア、Serverデータの変更境界を分離した。WebView2 Evergreen Runtimeは前提条件として検査・案内し、無断同梱・無断昇格を行わない。

候補比較、パッケージ境界、install／update／repair／uninstall、ダウングレード、署名、Build番号、CI成果物、ロールバック、データ保持方針を`design/46_INSTALLER_DISTRIBUTION_CONTRACT.md`へ記録した。P1-025はServer MSIとService登録、P1-026はWPF per-user MSIとRuntime前提確認を、このADRを入力として実装する。

## 再現可能な確認

```powershell
Get-Content .\design\46_INSTALLER_DISTRIBUTION_CONTRACT.md
git diff --check
```

P1-024は方式ADRの確定チケットであり、インストーラー生成、Service登録、実環境install／update／repair／uninstallはP1-025およびP1-026へ分離した。
