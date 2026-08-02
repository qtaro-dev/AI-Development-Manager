# P1-026 WPF Client配布契約

## 1. 成果物

- パッケージ: `AI-Development-Manager-Client-<ProductVersion>-<BuildNumber>-x64.msi`
- 方式: WiX Toolset v4／MSI
- 対象: Windows 11 64-bit、x64
- インストール範囲: per-user、管理者権限不要を目標とする
- 出力: `artifacts/packages/client/`

Client MSIはWPF実行ファイルと依存ファイルをユーザーのLocalAppData配下へ配置する。Server MSI、Windows Service、Firewall、HTTPS証明書、証明書ストア、プロジェクトデータは含めない。

## 2. Runtime前提

MSIはWebView2 Evergreen Runtimeの導入状態をHKLM／HKCUのEdgeUpdate Client registryから確認する。未導入の場合は、日本語のLaunchConditionで「Microsoft Edge WebView2 Runtimeをインストールしてから、WPF Clientを導入してください。」と案内し、Runtimeを無断インストール・無断昇格しない。

インストール後にRuntimeが削除・破損した場合は、既存のWPF Shellが表示する「Microsoft Edge WebView2 Runtimeをインストールしてから、再試行してください。」を利用する。WebView2 UserDataは`%LocalAppData%\AI Development Manager\WebView2`に保存され、MSIの配布ファイルとは別管理とする。

## 3. 配置と保持

| 種別 | 配置 | uninstall時 |
|---|---|---|
| WPF実行ファイル・依存ファイル | `%LocalAppData%\AI Development Manager\Client` | 削除 |
| Clientユーザーデータ予約領域 | `%LocalAppData%\AI Development Manager\UserData` | 保持 |
| WebView2 UserData | `%LocalAppData%\AI Development Manager\WebView2` | MSIから削除しない |
| Serverデータ・設定・ログ | Server側ProgramData | 変更しない |

ClientアンインストールはServer、プロジェクト、業務データ、証明書、WebView2 Runtimeを削除しない。Client更新・repairは配布ファイルだけを対象とする。

## 4. ライフサイクル

- install: per-user MSIとしてLocalAppDataへ配置し、Runtime前提を確認する。
- update: 同一ProductCode系統のMSI major upgradeで配布ファイルを置換する。Server URLやWebView2 UserDataを変更しない。
- repair: 同版MSIでClient配布ファイルを復元し、ユーザーデータとServerデータを保持する。
- uninstall: Client配布ファイルと空の予約フォルダーだけを対象とし、業務データ・Server・Runtimeを保持する。
- downgrade: `MajorUpgrade`の既定動作で拒否する。

## 5. 再現手順と検証

```powershell
pwsh -NoProfile -File .\scripts\installer\Build-WpfClientInstaller.ps1 -Configuration Release
```

確認項目:

1. `artifacts/packages/client/*.msi`と`manifest.json`が生成される。
2. manifestへSDK、SHA-256、per-user、WebView2前提、Serverデータ非操作が記録される。
3. WiX buildとMSI validationが成功し、per-user ScopeとRuntime LaunchConditionが含まれる。
4. 標準ユーザーでのinstall、Runtimeあり／なし、Server未導入・停止、起動、update、repair、uninstallはP1-027で実機確認する。

## 6. 未解決事項

- per-user MSIのWindows Installer実機install、更新、repair、uninstallはP1-027で確認する。
- WebView2 Evergreen Runtimeの配布経路はMicrosoft公式のBootstrapper／Standalone導入案内に限定し、Client MSIへの無断同梱は行わない。
