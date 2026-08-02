# P1-020 WPF WebView2シェル

## 目的

ASP.NET Core Serverが配信する共通Web UIをWebView2で表示し、接続状態とRuntime不足を案内できる最小WPF Clientを作る。

## 背景

WPFと通常ブラウザで同じWeb UIを利用し、WPFはWindows固有操作の補助に限定する方針が確定している。

## 前提・依存関係

- P1-002完了
- P1-019完了
- WebView2 Evergreen Runtime採用
- ADR-003、ADR-010

## 対象範囲

- .NET 10 WPFウィンドウ
- WebView2初期化とServer URL表示
- Runtime存在・版確認
- Server起動待ち、接続失敗、再試行
- 安全な外部Navigation制御

## 対象外

- 業務データ操作
- Explorer起動等のブリッジ
- 認証Cookie・HTTPS実運用
- WPF独自業務画面

## 対象ファイルまたは対象モジュール

- `src/Adm.Wpf`
- `tests/Adm.Infrastructure.Windows.Tests`またはWPF確認プロジェクト

## 具体的な実装内容

1. WebView2をホストする最小WPF shellを作る。
2. localhost Serverのreadiness確認後に共通UIへ遷移する。
3. Runtime不足、Server停止、読込失敗を日本語で案内する。
4. 許可Server origin以外へのNavigationを外部ブラウザまたは拒否へ分離する。
5. WebView2 user data folderの配置・清掃・権限境界を定義する。

## テスト内容

- Runtimeあり/なし
- Server起動前・起動後・停止後
- 共通Web UI表示と再読込
- 許可外Navigation
- WPF終了後もServerが継続すること
- 複数WPF起動時の動作

## 完了条件

- WebView2でP1-019の同一Web UIを表示できる。
- WPF終了がServer終了を意味しない。
- Runtime不足と接続失敗の次の操作を日本語で示す。
- WPF独自の業務データアクセスがない。

## ユーザーが目視確認する内容

- WPF内の共通Web UI
- Runtime不足・Server停止時の案内
- WPF終了後のブラウザ利用継続

## 想定されるリスク

- WebView2 user data権限・ロック
- 許可外URLをWPF内で開く
- WPFがServerプロセス寿命を所有する設計へ戻る

## 完了後に更新すべき設計資料

- WPF Client構成設計
- ADR-001、ADR-003、ADR-010
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

## 実装結果

- `Adm.Wpf`を起動可能なWinExeへ変更し、Microsoft.Web.WebView2 1.0.3967.48を中央管理で追加した。
- P1-019のServer URLをWebView2で表示し、`/health/ready`確認後に同一URLへ遷移するShellを実装した。
- WebView2 Runtime不足、Server未起動、読込失敗を日本語メッセージと再試行操作で案内する。
- 設定Server origin以外のNavigationと新規WindowをWebView2内で開かない。
- WPF終了時はWebView2だけを終了し、Serverプロセスを停止しない。
- Navigation方針と不正Server URLの単体テストを追加した。

### 画面確認

1. Serverを起動する。

   ```powershell
   dotnet run --project .\src\Adm.Server.Host -- --Server:Port=5181
   ```

2. 別のPowerShellでWPF Shellを起動する。

   ```powershell
   dotnet run --project .\src\Adm.Wpf -- --server-url=http://127.0.0.1:5181/
   ```

3. WPF内にAppShell、共通状態部品、ダークテーマが表示されることを確認する。Server停止後に再試行案内が表示され、WPF終了後もServerが継続することを確認する。

WebView2 Runtime未導入環境では、Runtime不足の案内が表示される。P1-021以降の業務データBridgeは本チケットに含めない。
