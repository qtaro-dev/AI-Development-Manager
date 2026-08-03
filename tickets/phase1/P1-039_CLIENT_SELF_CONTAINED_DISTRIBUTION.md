# P1-039 Client Self-contained配布

## 目的

.NET Desktop Runtime未導入のWindows 11 64-bit環境でもWPF Clientを起動できる、`win-x64` Self-contained配布物を作成し、Clientの正式Runtime方式を確立する。

## 背景

クリーンVMではFramework-dependent ClientがRuntime不足で起動できなかった。Windowsアプリを主製品とするため、利用者へ.NET Runtimeの手動導入を要求しない配布方式へ改める。WebView2 Evergreen RuntimeはWindows側の独立前提として扱う。

## 前提・依存関係

- P1-038完了・承認済み
- P1-024の配布ADRとP1-026のClient MSI実装結果
- リポジトリ直下`global.json`の.NET 10 SDK
- 正式対応OSはWindows 11 64-bit

## 対象範囲

- WPF Clientの`win-x64` Self-contained publish
- Release成果物への組み込みWeb UI資産同梱
- trimmingなし、複数ファイル配布を既定とする安全なpublish設定
- Runtime識別、版数、ハッシュ、SBOM等の成果物情報
- Framework-dependent成果物との混同防止
- WebView2 Runtimeの存在確認と不足時の利用者向け案内境界
- 配布サイズと起動時間の実測

## 対象外

- Server配布方式、Server MSI、Windows Service
- Client MSIのShortcut、アイコン、ARP表示
- WebView2 Fixed Version Runtimeの同梱
- single-file化、trimming、NativeAOT
- 自動更新、コード署名、クラウド配布
- 製品機能やUIレイアウト変更

## 対象ファイルまたは対象モジュール

- `src/Adm.Wpf/`のpublish設定
- `Directory.Build.*`または専用publish profile
- `eng/`、`scripts/`等の既存ビルド入口
- CIのClient publish／成果物検査
- 配布・SBOM・依存関係検査
- `design/46_INSTALLER_DISTRIBUTION_CONTRACT.md`
- P1-039配布方式資料

## 具体的な実装内容

1. `net10.0-windows`、`win-x64`、Self-containedを固定したRelease publish入口を作る。
2. WPF、WebView2、組み込みWeb資産、必要なmanaged/native依存物が欠落しないことを検査する。
3. trimmingとsingle-fileを無効にし、将来変更時は別PoC／ADRを要求する。
4. 成果物へ製品版、Runtime、RID、ファイルハッシュ、SBOMを関連付ける。
5. Framework-dependent Clientを正式成果物として誤配布しない命名・CI検査を追加する。
6. WebView2不足は.NET Runtime不足と区別し、利用者が解決できる案内を設計する。

## テスト内容

- `.NET Desktop Runtime`未導入のクリーンWindows 11で直接起動
- Server未導入・停止、ネットワーク切断でLocal画面表示
- 組み込みWeb UI、Local Channel、設定、終了のスモーク
- 成果物完全性、ハッシュ、SBOM、依存脆弱性検査
- WebView2あり／なしの判定と案内
- 配布サイズ、初回起動、2回目起動の実測
- Debug／Release Build、Test、Architecture、Web Test、Playwright回帰

## 成功条件

- .NET Runtimeを別途導入せずClientが起動する。
- Serverや待受ポートなしでLocal modeを利用できる。
- 正式成果物がSelf-containedであることをCIで検査できる。
- WebView2不足時にRuntime種別を取り違えない案内が出る。
- 配布サイズと起動時間が記録され、重大な退行がない。

## 完了条件

- 再現可能なpublish手順、成果物、検査、クリーン環境スモークが完成している。
- 配布方式ADR／契約とCIが更新されている。
- MSI操作性変更はP1-040へ分離されている。

## ユーザーが目視確認する内容

- .NET Runtime未導入VMでの起動
- Local画面、設定、終了
- WebView2不足時の日本語案内
- 成果物名、版数、サイズ

## 想定されるリスク

- publish物からnative DLLやWeb資産が欠落する。
- trimming等を同時導入して実行時障害を招く。
- .NET RuntimeとWebView2 Runtimeの案内を混同する。
- Server配布方式まで同時変更する。
- CIは成功してもクリーンOSで暗黙の依存が残る。

## 完了後に更新すべき設計資料

- `design/00_INDEX.md`
- `design/30_PHASE1_IMPLEMENTATION_PLAN.md`
- `design/46_INSTALLER_DISTRIBUTION_CONTRACT.md`
- `design/48_WPF_CLIENT_INSTALLER_CONTRACT.md`
- P1-039 Self-contained配布資料
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`
- 本チケット

## 完了時に残す証拠

- publishコマンドと`dotnet --version`
- 成果物一覧、サイズ、ハッシュ、SBOM
- クリーンVM起動結果
- Runtime／WebView2判定結果
- Build、Test、依存関係検査、`git diff --check`結果

## 実装結果

実装済み。`win-x64`／Release／Self-contained／複数ファイル／trimmingなしのWPF Client publish入口を追加し、Client MSIも同じSelf-contained成果物を入力するよう更新した。

- `scripts/installer/Publish-WpfClient.ps1`で固定SDK、RID、Self-contained、single-file無効、trimming無効を検証する。
- publish出力へ`publish-manifest.json`（版数、SDK、RID、全ファイルSHA-256、サイズ）と`sbom.cdx.json`を生成する。
- runtimeconfigを検査し、Framework-dependent publishを拒否する。
- WebView2 Evergreen Runtimeは独立前提として保持し、.NET Runtime不足と混同しない。
- P1-040のShortcut、アイコン、ARP改善には着手していない。

## 状態

実装済み。P1-040以降は未着手。
