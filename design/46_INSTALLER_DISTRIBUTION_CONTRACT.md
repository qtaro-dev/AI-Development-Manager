# P1-024 配布・インストーラー方式ADR

## 1. 判定

P1-024では、ServerとWPF Clientを別パッケージとして配布し、両方のパッケージ形式にWiX Toolset v4のWindows Installer（MSI）を採用する。Serverはper-machine、WPF Clientはper-userとして構成する。

インストーラーの対象はWindows 11 64-bit、x64、.NET 10製品成果物とする。公開配布、正式コード署名証明書の購入、HTTPS証明書、Firewall変更、自動更新Serverは本ADRの対象外である。

## 2. 採用理由

| 候補 | 評価 | 判定 |
|---|---|---|
| WiX Toolset v4 / MSI | Windows Service登録、per-machine／per-user、upgrade、repair、uninstall、silent実行、CIでの再現可能な成果物化を一つのWindows Installer契約にまとめられる | 採用 |
| MSIX | WPFのper-user配布と更新には適するが、Server Service、保存先アクセス、repairの運用境界が異なり、Serverの主パッケージには適さない | 主方式として不採用 |
| Visual Studio Installer Project | 小規模な試作には利用できるが、Visual Studio拡張に依存し、CIでの明示的な再現性・検査境界を固定しにくい | 不採用 |
| Inno Setup等の独自ブートストラップ | 柔軟だが、Windows Installerのrepair、upgrade、管理ツール連携を別設計する必要がある | 不採用 |

MSIXを全面採用せず、ServerとWPFで異なる権限モデルを優先する。WPFの標準ユーザー配布はper-user MSIで実現可能かをP1-026のクリーン環境検証で確認し、成立しない場合のみ別方式のADR変更を行う。

## 3. パッケージ境界

### Server package

- `Adm.Server.Host`、Web静的成果物、必要な製品依存ファイルを含む。
- per-machine、既定のインストール先は管理者が管理するProgram Files配下とする。
- Windows Serviceの登録・更新・停止・削除はP1-025のWiX ServiceInstall境界で実装する。
- Serverデータ、設定、ログ、秘密情報をパッケージへ含めない。
- アンインストール時は利用者データと構成を既定で削除せず、明示的な削除手順へ分離する。

### WPF Client package

- `Adm.Wpf`と必要な製品WPF依存ファイルを含む。
- per-user、管理者権限を要求しないインストールを目標とし、ユーザー領域へ配置する。
- Windows Service、Firewall、証明書ストア、任意の共有フォルダーを変更しない。
- WebView2 SDKのランタイムを製品パッケージへ無断同梱しない。Evergreen Runtimeの有無を検査し、未導入時は分かりやすい案内または承認済みの前提条件処理へ分岐する。
- WPF ClientのアンインストールでServerデータを削除しない。

成果物の予定配置は次のとおりとする。

```text
artifacts/packages/<version>/<build>/
├─server/AI-Development-Manager-Server-<version>-<build>-x64.msi
├─client/AI-Development-Manager-Client-<version>-<build>-x64.msi
└─manifest.json
```

`artifacts/`は生成物の保存場所であり、通常はGit管理対象にしない。

## 4. install / update / repair / uninstall

| 操作 | Server | WPF Client |
|---|---|---|
| install | 管理者UAC、Service登録はP1-025 | 標準ユーザーのper-user MSIをP1-026で確認 |
| update | major upgrade、停止→置換→起動、失敗時ロールバック | 同一ProductCode系統のupgrade、起動中プロセスを安全に扱う |
| repair | MSI repairでバイナリを復元し、データ・設定は保持 | MSI repairでクライアントファイルを復元し、Serverデータは変更しない |
| uninstall | Service停止・登録解除、製品ファイル削除、データは保持 | クライアントファイル削除、ユーザーデータ・Serverデータは保持 |

ダウングレードは既定で拒否する。明示的な管理者操作で実施する場合も、対象バージョン、バックアップ、復元可否を事前に確認する。更新中のServer起動失敗は旧バイナリまたは直前のインストール状態へ戻し、データを自動削除しない。

## 5. 署名・版・CI成果物

- MSI、実行ファイル、ブートストラップ成果物は署名対象とし、署名処理はCIの秘密ストアから証明書を受け取る差込境界に分離する。
- 証明書秘密鍵、PFX、トークン、署名済み本番成果物はリポジトリへ保存しない。
- 開発・検証では署名なしまたはCI専用テスト証明書を使用し、正式配布の信頼性判定と混同しない。
- File Version、Informational Version、Build番号は`Directory.Build.props`の単一生成元を使用し、パッケージファイル名・manifest・証拠へ同じ値を記録する。
- CIではclean restore、publish、MSI生成、パッケージ内容検査、署名状態検査、SHA-256 manifest生成を行う。生成物は`artifacts/ci-evidence`へ保存し、Gitへ追加しない。

## 6. 安全境界

- インストーラーはHTTPS証明書、Windows Firewall、認証情報、プロジェクトデータを自動作成・変更しない。
- Serverはlocalhost限定の既存Host契約を維持し、LAN公開をインストール操作で有効化しない。
- Service登録は管理者権限を必要とするServer packageだけに限定する。
- WPF Bridgeの許可操作、任意コマンド実行、自由なファイルアクセスをインストーラーへ追加しない。
- 前提条件不足、権限不足、使用中ファイル、WebView2 Runtime不足は内部エラーと利用者向け案内を分離して表示する。

## 7. P1-025/P1-026への入力

- P1-025はWiX v4 Server MSI、per-machine、ServiceInstall、upgrade、repair、uninstall、データ保持、管理者／標準ユーザー境界を実装・検証する。
- P1-026はWiX v4 WPF per-user MSI、標準ユーザー、WebView2 Runtime前提案内、upgrade、repair、uninstallを実装・検証する。
- 両チケットは実機またはクリーンVMで、通常・silent・失敗・中断・ロールバックを確認する。
- P1-026でper-user MSIのOS制約が完了条件を満たさない場合、実装を拡張せずP1-024 ADRの再審議事項として記録する。

## 7a. P1-025実装結果

`installer/server`へWiX v4 MSIプロジェクトを追加し、Server publish成果物をper-machineパッケージへまとめる構成を実装した。`AIDevelopmentManagerServer`をLocalService・自動開始で登録し、ServerのService操作をServer MSIへ限定した。Config／Logs／DataはProgramData配下へ分離し、アンインストール時に無条件削除しない。

P1-025の生成手順とService運用契約は`design/47_SERVER_INSTALLER_SERVICE_CONTRACT.md`に記録した。WPF Client、WebView2 Runtime、Firewall、HTTPS証明書、LAN公開は対象外であり、P1-026以降へ着手していない。

## 7b. P1-026実装結果

`installer/wpf-client`へWiX v4のper-user MSIを追加し、WPF publish成果物をLocalAppData配下へ配置する構成を実装した。WebView2 Evergreen RuntimeをHKLM／HKCUのregistryから検査し、未導入時は日本語の導入案内を表示する。Runtimeの無断インストール、Server、Service、Firewall、証明書、業務データの変更は行わない。

WPF配布・保持契約は`design/48_WPF_CLIENT_INSTALLER_CONTRACT.md`に記録した。P1-027の統合・実機確認には着手していない。

## 8. 参照

- [Create a Windows Service installer - .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service-with-installer)
- [Distribute your app and the WebView2 Runtime](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution)
- [Evergreen vs. fixed version of the WebView2 Runtime](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/evergreen-vs-fixed-version)

## 9. P1-028による配布境界補足

Windowsアプリを主製品とするADR-019により、WPF Client MSIは通常利用の主パッケージ、Server MSIは任意導入の追加パッケージとして扱う。Client MSIの導入・起動はServer MSI、Windows Service、LAN、HTTPSを必須条件にしない。

既存のServer MSIのper-machine／Service契約とClient MSIのper-user／WebView2契約は、Server modeおよび追加機能の配布契約として維持する。P1-025／P1-027で未完了のServer Service実機確認とInstaller Runtime確認は履歴・残課題として保持し、Local modeの通常起動条件から分離する。P1-028ではInstaller実装やRuntime方式を変更しない。

## 10. P1-039 Client Self-contained配布

WPF Clientの正式publish入口は`./scripts/installer/Publish-WpfClient.ps1`とし、Release、`win-x64`、Self-contained、複数ファイル、trimmingなしを固定する。`src/Adm.Wpf/Properties/PublishProfiles/WinX64SelfContained.pubxml`は同じ境界を宣言的に保持する。

Self-contained成果物には.NET Desktop Runtimeを同梱し、`publish-manifest.json`へSDK、TargetFramework、RID、Self-contained、版数、全ファイルSHA-256、合計サイズを記録する。`sbom.cdx.json`は成果物ファイルとSDK／RIDの検証情報を記録する。WebView2 Evergreen Runtimeは引き続き独立したWindows前提であり、.NET Runtime不足とは別の案内境界とする。

Framework-dependent publish、single-file、trimmingはClientの正式配布入口として使用せず、publish検査で検出する。WPF Client MSIはP1-039のSelf-contained publish出力を入力とし、P1-040のShortcut・ARP改善は変更しない。

## 11. P1-040 Client MSI操作性

Client MSIはP1-039のSelf-contained複数ファイル出力を収録し、per-userの`ProgramMenuFolder`配下へ「AI Development Manager」ショートカットを作成する。デスクトップショートカットは作成しない。ショートカット、WPF実行ファイル、MSIのARP表示は、同一の決定的な製品アイコンと製品名を使用する。

ARPには発行元、製品説明、About／Help URLを設定し、修復・更新・削除を許可する。Downgradeは`MajorUpgrade`で拒否する。通常uninstallはClient配布ファイルと空の予約フォルダーだけを対象にし、`UserData`、`.adm-meta`、業務データ、WebView2 UserData、Serverデータを削除しない。MSIログは`msiexec /l*v`で取得し、秘密情報を引数や独自ログへ渡さない。

Runtime前提はWebView2 Evergreen Runtimeだけとし、.NET Desktop RuntimeのLaunchConditionは持たない。WebView2不足時は日本語でRuntime名、導入後の再実行を案内する。
