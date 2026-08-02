# P1-025 Server配布・Service運用契約

## 1. 成果物

- パッケージ: `AI-Development-Manager-Server-<ProductVersion>-<BuildNumber>-x64.msi`
- 方式: WiX Toolset v4／MSI
- 対象: Windows 11 64-bit、x64
- インストール範囲: per-machine、管理者UAC必須
- 出力: `artifacts/packages/server/`

Server MSIは、Server実行ファイルと同一HostのWeb静的成果物をProgram Files配下へ配置し、Windows Service `AIDevelopmentManagerServer`を登録する。WPF Client、WebView2 Runtime、Firewall、HTTPS証明書は含めない。

## 2. Service境界

| 項目 | 契約 |
|---|---|
| Service name | `AIDevelopmentManagerServer` |
| Display name | `AI Development Manager Server` |
| 実行アカウント | `LocalService` |
| 起動 | 自動開始 |
| 実行ファイル | `Adm.Server.Host.exe --adm-startup-mode=service` |
| Host | `ServerHostFactory`を使用 |
| 待受 | 既存契約どおりlocalhost／127.0.0.1限定 |
| 停止 | Windows Service停止制御、Host停止タイムアウト30秒 |
| Firewall | 変更しない |

Service登録はServer MSIのComponentへ限定し、WPF ClientやWeb UIからService操作を公開しない。Service開始後は既存の`/health/live`、`/health/ready`で状態を確認する。

## 3. 配置と保持

| 種別 | 配置 | uninstall時 |
|---|---|---|
| 実行ファイル・静的Web成果物 | Program Files\AI Development Manager\Server | 削除 |
| 設定 | ProgramData\AI Development Manager\Server\Config | 保持 |
| ログ | ProgramData\AI Development Manager\Server\Logs | 保持 |
| 将来データ | ProgramData\AI Development Manager\Server\Data | 保持 |

インストーラーは製品データ、設定、ログ、証明書を作成・移行・削除する業務処理を持たない。フォルダーは初期配置のために作成するが、アンインストールで中身を無条件削除しない。実データの移行・バックアップ・削除は後続の明示操作へ分離する。

## 4. ライフサイクル

- install: 管理者UAC後にファイル配置、フォルダー作成、Service登録、自動開始を行う。
- update: MSI major upgradeで旧Serviceを停止し、同一Service名を維持して置換・再起動する。
- repair: 同版MSIのrepairで実行ファイルと静的成果物を復元し、Config／Logs／Dataを変更しない。
- uninstall: Service停止・登録解除と製品ファイル削除を行い、Config／Logs／Dataは保持する。
- downgrade: `MajorUpgrade`の既定動作で拒否する。

失敗時はService状態とインストーラーログを確認し、再試行または直前版への復元手順へ案内する。Firewall、HTTPS証明書、LAN公開は自動変更しない。

## 5. 再現手順と検証

固定SDK確認とServer MSI生成:

```powershell
pwsh -NoProfile -File .\scripts\installer\Build-ServerInstaller.ps1 -Configuration Release
```

確認項目:

1. `artifacts/packages/server/*.msi`と`manifest.json`が生成される。
2. manifestへSDK、SHA-256、Service名、LocalService、per-machine、データ保持方針が記録される。
3. WiX buildが成功し、ServiceInstall／ServiceControlがMSIへ含まれる。
4. Windows 11実機またはクリーンVMでのUAC、Service開始・停止、health、update、repair、uninstall、残置データはP1-027統合確認で実施する。

## 6. 未解決事項

- 実機での管理者／標準ユーザー権限差、Serviceアカウントの保存先ACL、更新失敗時の復元はP1-027で最終確認する。
- 署名証明書の正式な保管先とCI署名ジョブは、秘密情報をリポジトリへ置かない境界のまま、配布運用時に確定する。
