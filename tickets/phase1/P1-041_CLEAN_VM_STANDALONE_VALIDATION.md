# P1-041 クリーンVM単体運用試験

## 目的

クリーンなWindows 11 64-bit VMで、Serverと.NET Desktop Runtimeを導入せずにClient MSIの導入からLocal利用、更新、削除までを再現し、Windowsアプリ単体運用の正式な実機証拠を残す。

## 背景

CIや自動テストだけでは、Runtime、MSI、Shortcut、WebView2、OS権限、既存環境依存を検出できなかった。本チケットは検証専用とし、失敗をその場で修正せず、再現証拠と是正チケットへ分離する。

## 前提・依存関係

- P1-038、P1-039、P1-040完了・承認済み
- Windows 11 Enterprise Evaluation等のクリーンスナップショット
- Server未導入、.NET Desktop Runtime未導入の基準状態

## 対象範囲

- VM前提条件とスナップショットの記録
- Client MSI install、起動、Local mode、設定、終了
- Server未導入・停止、ネットワーク切断時の動作
- スタートメニュー、ARP、repair、upgrade、uninstall
- WebView2あり／不足時の動作または再現可能な検査
- 初回・再起動の時間、プロセス、待受ポート
- ログ、画面、コマンド結果の証拠化
- 不合格項目の是正チケット候補作成

## 対象外

- 試験中に発見した製品・MSIコードの修正
- Server MSI、Windows Service、LAN複数利用者
- 認証、証明書、Firewall
- Phase 2業務機能
- Windows 10、macOS、Linux
- 性能基準1万文書の本測定

## 対象ファイルまたは対象モジュール

- `tests/manual/`または既存の実機試験手順・結果置場
- `evidence/`等のコミット可能な非機密証拠索引
- P1-041クリーンVM試験結果
- `design/48_WPF_CLIENT_INSTALLER_CONTRACT.md`
- `design/49_PHASE1_INTEGRATION_GATE_RESULT.md`
- 本チケットとPhase 1索引

## 具体的な実装内容

1. OS build、VirtualBox、Guest Additions、Windows Update、Runtime、Server、WebView2の初期状態を記録する。
2. クリーンスナップショットから検証を開始し、MSIとハッシュを固定する。
3. 標準ユーザーのinstall、スタートメニュー起動、Local画面、設定、終了を順に確認する。
4. Serverなし、ネットワーク切断、再起動後もLocal利用できることを確認する。
5. repair、upgrade、uninstallとユーザーデータ保持を確認する。
6. プロセス、localhost待受、Server Service不存在、初回・再起動時間を測る。
7. 各結果へ時刻、手順、期待値、実測、証拠パスを付ける。不合格は修正せず是正候補として分離する。

## P1-041使用成果物（P1-040 Release生成物）

試験担当者は、次のMSIをP1-041のインストーラー試験に使用する。ファイル名、版数、ハッシュが一致しない成果物は使用しない。

| 項目 | 値 |
|---|---|
| MSI生成先 | `D:\Dev\AI Development Manager\artifacts\packages\client\` |
| MSIファイル名 | `AI-Development-Manager-Client-0.1.0-1-x64.msi` |
| 製品版数 | `0.1.0` |
| Build番号 | `1` |
| SHA-256 | `5604B0BB9C9FFBF1DC619C797C3DC720524BBD7CA10C1DA9A0C87684BA8A781F` |
| 配布方式 | `win-x64` Self-contained、複数ファイル、trimmingなし |

### VMへコピーする成果物

最小構成として、次の2ファイルをVMの試験用フォルダーへコピーする。

1. `AI-Development-Manager-Client-0.1.0-1-x64.msi`（P1-041で実際にinstall／repair／upgrade／uninstallへ使用）
2. `manifest.json`（同じ`artifacts\packages\client\`にある成果物情報・SDK・Runtime境界の確認用）

VM上でハッシュを再確認する。

```powershell
Get-FileHash .\AI-Development-Manager-Client-0.1.0-1-x64.msi -Algorithm SHA256
```

期待値は次のとおり。

```text
5604B0BB9C9FFBF1DC619C797C3DC720524BBD7CA10C1DA9A0C87684BA8A781F
```

Self-contained EXEを直接確認する必要がある場合の診断用出力は、`D:\Dev\AI Development Manager\artifacts\package-input\wpf-client\Adm.Wpf.exe`にある。ただし、P1-041の正式なインストール試験、Shortcut試験、ARP試験、uninstall試験ではMSIだけを使用し、EXEを直接起動して合格扱いにしない。Server、.NET Desktop Runtime、開発用`bin`／`obj`はVMへコピーしない。

## テスト内容

- MSI install／repair／upgrade／uninstall
- .NET Runtimeなしの起動
- Local初回・再起動・ネットワーク切断
- Server process／Service／localhost port非依存
- 設定変更と復元、接続失敗からLocalへ戻る操作
- WebView2有無の前提条件案内
- Shortcut、アイコン、ARP、終了
- 連続5回起動と異常終了・残存プロセス確認

## 成功条件

- Serverと.NET RuntimeなしでClientを導入・起動・Local利用できる。
- ネットワーク切断でも通常のLocal開始を妨げない。
- install、repair、upgrade、uninstallが合格する。
- 終了後に不要なプロセスやlocalhost待受が残らない。
- 全判定に再現可能な証拠がある。

## 完了条件

- 試験項目がすべて合格するか、不合格が再現証拠付き是正チケットとして分離されている。
- 試験中に対象コードを変更していない。
- VM初期状態、MSIハッシュ、手順、結果、未解決事項が記録されている。

## ユーザーが目視確認する内容

- クリーンVMの前提条件
- installからLocal利用、終了までの録画または画面証拠
- Shortcut、アイコン、ARP
- Server非依存と待受なしの結果
- upgrade／uninstall後の状態

## 想定されるリスク

- 開発PCにあるRuntimeや環境変数を暗黙に利用する。
- 同じスナップショットを使わず試験間で条件が変わる。
- 試験中の即時修正で再現性とチケット境界を失う。
- WebView2と.NET Runtimeの存在を混同する。
- 秘密情報や巨大ログをリポジトリへ保存する。

## 完了後に更新すべき設計資料

- `design/00_INDEX.md`
- `design/46_INSTALLER_DISTRIBUTION_CONTRACT.md`
- `design/48_WPF_CLIENT_INSTALLER_CONTRACT.md`
- `design/49_PHASE1_INTEGRATION_GATE_RESULT.md`
- P1-041クリーンVM試験結果
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`
- 本チケット

## 完了時に残す証拠

- VM環境表とスナップショット識別子
- MSIハッシュと実施手順
- 画面キャプチャ、ログ、プロセス／ポート確認
- 各試験の期待値・実測・判定
- 不合格時の是正チケット候補

## 状態

作成済み。未着手。
