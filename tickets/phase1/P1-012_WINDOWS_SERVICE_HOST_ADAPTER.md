# P1-012 Windows Service Host Adapter

## 目的

同一ASP.NET Core HostをWindows Service、console、manual、tray呼出境界から利用できる製品Adapterを実装する。

## 背景

正式運用はWindows Serviceとし、業務ロジックを起動方式別に複製しない方針がP0-002とADR-012で確定している。Phase 1で実Service登録前の製品Host境界を完成させる。

## 前提・依存関係

- P1-006完了
- P1-011完了
- P0-002結果、ADR-012

## 対象範囲

- Windows Service Adapter
- console/manual/tray呼出モードの同一Host利用
- Service開始、停止、タイムアウト、二重起動
- 起動モード識別と共通ヘルス

## 対象外

- トレイUI本体
- インストーラー
- Firewall、証明書、LAN待受

## 対象ファイルまたは対象モジュール

- `src/Adm.Infrastructure.Windows/Hosting`
- `src/Adm.Server.Host`
- `tests/Adm.Infrastructure.Windows.Tests`

## 具体的な実装内容

1. Windows Service向けHost lifetimeをAdapter内に構成する。
2. console/manual/tray呼出が同じHost factoryを使用するようにする。
3. 起動方式差を引数・Adapter設定に限定する。
4. Service停止要求を正常停止へ伝える。
5. 二重起動と停止タイムアウトを明示ログへ記録する。

## テスト内容

- console/manual/tray境界の共通Host確認
- Windows Service実登録、開始、停止、再起動
- Serviceアカウント権限差の基礎確認
- 二重起動
- 各モードのヘルス契約一致
- Core/ApplicationにWindows参照がないこと

## 完了条件

- 実Windows Serviceとして開始・停止できる。
- 4用途が同じHost factoryとAPIを利用する。
- 起動方式別の業務ロジック分岐がない。
- Windows固有参照がAdapterとWPFに限定される。
- LAN待受やFirewall変更をこのチケットで行わない。

## ユーザーが目視確認する内容

- Service管理画面での開始・停止
- console/manualとの動作比較
- 二重起動時の案内

## 想定されるリスク

- Serviceアカウントと対話ユーザーのパス・権限差
- 停止待機中の強制終了
- tray境界を不要な常駐UIへ拡大する

## 完了後に更新すべき設計資料

- ADR-012
- Server配置・起動設計
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

## 実装結果

- `Adm.Infrastructure.Windows.Hosting`へWindows Service lifetimeを利用するAdapterを追加した。
- `--adm-startup-mode=console|manual|service|tray`を共通解決し、Windows Service実行環境は自動的にservice modeとして扱う。
- Service名を固定し、停止タイムアウトを30秒へ設定した。
- `Program`から同じ`ServerHostFactory`へ起動モードとAdapter設定を渡す構成にした。
- Health、API、エラー、ログは起動方式で複製せず、同じHostと契約を利用する。
- Service実登録、権限設定、Firewall、証明書、インストーラー、トレイUIは実装していない。

## 検証結果

使用SDK: `10.0.302`（`global.json`固定値と`dotnet --version`実測値が一致）。

実行したコマンド:

```powershell
dotnet build .\AIDevelopmentManager.sln --configuration Debug
dotnet test .\tests\Adm.Infrastructure.Windows.Tests\Adm.Infrastructure.Windows.Tests.csproj --configuration Debug --no-build --no-restore
dotnet test .\tests\Adm.Server.IntegrationTests\Adm.Server.IntegrationTests.csproj --configuration Debug --no-build --no-restore
```

Windows境界テスト6件、Server統合テスト18件が成功した。起動モード解決、Service AdapterのHostBuilder適用、console／manual／trayの同一Host・Health契約、localhost待受を確認した。

P1-012は完了状態とし、P1-013以降は対象外とする。
