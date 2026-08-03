# P1-043 Client Web資産収録・探索パス是正

## 目的

P1-041クリーンVM試験で確認された「Web UIの配布物がありません。」を原因分析し、P1-040 Client MSIのWeb資産収録・インストール配置・WPF起動時探索パスの不一致を是正する。

本チケットはP1-041の試験結果を受けた是正チケットであり、P1-041の試験中に製品コードを変更しない。P1-042のPhase判定も本チケット完了まで確定しない。

## P1-041で確認された事象

- MSI実行時にセットアップウィザードが表示されず、開始状態が利用者に分かりにくい。
- インストール後、WebView2は起動するが、WPFが`index.html`を見つけられず「Web UIの配布物がありません。」を表示する。

## 調査対象

1. MSI内部に`WebAssets/index.html`、`assets/*`、その他のReact production資産が実際に含まれているかを、生成前publishフォルダー、生成`ClientFiles.wxs`、MSI内部のFile／Mediaテーブル、インストール後ファイルで突合する。
2. MSIの実インストール先を確認する。想定は`%LOCALAPPDATA%\AI Development Manager\Client\WebAssets\index.html`であり、実測パスと相対構造を記録する。
3. `Adm.Wpf.Shell.WebAssetResolver`、`AppContext.BaseDirectory`、`WebAssets`相対パス、WPF出力ディレクトリの起動時探索パスを記録する。
4. `Publish-WpfClient.ps1`の出力、`Build-WpfClientInstaller.ps1`の収録元、WiX生成Fragment、MSI、インストール先のファイル一覧を同一ハッシュまたは同一相対パスで比較する。
5. Self-contained publish、MSI収録、WebView2初期化の責務を分離し、WebView2 Runtimeの有無とWeb資産欠落を混同しない。
6. セットアップウィザードが表示されない理由を、サイレント実行、MSI UIシーケンス、LaunchCondition、ユーザー権限、起動コマンドの観点で切り分ける。P1-041の通常試験手順ではサイレントオプションを使用しない。

## 是正方針

- Web資産の正本はP1-039のproduction publish出力とし、MSIへ収録する相対パスを固定する。
- WPFの探索パスとMSIのインストール配置を同じ契約へ揃える。
- MSI生成時に`WebAssets/index.html`の存在、主要asset、相対パス一致を自動検査し、不一致ならMSIを生成しない。
- インストール後のsmokeで`Client\WebAssets\index.html`、WPF起動、Local画面表示を確認する。
- セットアップUIは標準ユーザーが開始・進捗・完了を認識できる既定UIを使用する。サイレント導入は別手順として明示する。
- Server、Windows Service、P1-041試験手順の合否を本チケットへ混在させない。

## 成功条件

- MSIの内部とインストール先に、期待するReact Web資産が同一相対構造で存在する。
- `%LOCALAPPDATA%\AI Development Manager\Client\WebAssets\index.html`をWPF起動時に解決できる。
- .NET Runtime未導入、Server未導入、localhost待受なしでLocal画面を表示できる。
- MSI生成時にWeb資産欠落を自動検出できる。
- 通常のMSI実行でセットアップ開始・完了が利用者に認識できる。
- 修正後にP1-041をクリーンVMで再実施し、結果を別証拠として保存する。

## 完了条件

- 原因、影響範囲、修正対象、再現コマンド、MSI内外のファイル一覧が記録されている。
- Debug／Release Build、Test、Architecture検査、Web検査、MSI静的検査が成功している。
- 修正済みMSIの版数・SHA-256を記録している。
- P1-041を修正版MSIで再実施し、Web資産・Local画面・Shortcut・uninstall保持結果が記録されている。
- P1-043の修正とP1-041の実機結果が別Commitとして追跡できる。

## 対象外

- P1-041試験中の直接修正
- Server MSI、Windows Service、LAN、認証、業務機能
- P1-042ゲート内での修正
- Web UI機能・レイアウト変更

## 状態

実装・自動検証完了。P1-041クリーンVM再試験実施済み。後続是正はP1-044／P1-045で管理する。

## 実装結果

- `Adm.Wpf`のPublish完了後にproduction WebAssetsを`$(PublishDir)WebAssets`へコピーし、Self-contained publish成果物へ確実に含めるよう修正した。
- `WebAssetResolver`の探索契約を`AppContext.BaseDirectory\WebAssets`へ明示的に固定した。
- MSI生成前に`WebAssets/index.html`を必須検査し、Publish成果物とWiX Fragmentの相対パス・SHA-256を突合する検査を追加した。
- MSI生成後にWindows InstallerのMSI Fileテーブルを検査し、WebAssets全4ファイルがMSI内部へ収録されていることを確認するようにした。
- セットアップウィザード未表示の原因は、PackageへWiX標準UIを参照していなかったことと切り分けた。`WixUI_Minimal`、UTF-8コードページ、UI拡張を追加し、MSIのDialog 18行／InstallUISequence 18行を確認した。サイレント実行時にUIが出ない挙動は仕様として維持する。

### 自動検証結果

- Publish: `artifacts/package-input/wpf-client/WebAssets/index.html`、`assets/*`を確認。
- WiX Fragment: `artifacts/installer-generated/ClientFiles.wxs`とPublishのWebAssets 4ファイルが一致。
- MSI: `artifacts/packages/client/AI-Development-Manager-Client-0.1.0-1-x64.msi`のFileテーブルと一致。
- MSI SHA-256: `F819AC2EA98D8CD6F868A28B5502256AD740556AA4307B98FA3F2B3B23AEC48A`。
- P1-041の実機再試験およびインストール後実ファイルのVM確認は、P1-041の試験結果を変更せず、別証拠として後続に実施する。
