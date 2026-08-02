# P0-002 Hosting Modes PoC

P0-002の独立PoC。製品コードへ自動昇格させず、`poc/hosting-modes`内だけで完結させる。

## 結論

- コンソール、Windows Service相当、手動、トレイの4用途は、`Adm.Server.Host`の同一Host構成を再利用できる。
- Windows Service固有の設定は`Adm.Infrastructure.Windows`の`WindowsServiceAdapter`だけが担当する。
- `Adm.Core`はWindows固有Assemblyを参照しない。
- 起動方式による業務ロジックの分岐は発生しない。
- 実Service登録、トレイUI、製品インストーラーは本PoCの対象外であり、未実装である。

## 構成

```text
Adm.Server.Host
  ├─ Adm.Core
  └─ Adm.Infrastructure.Windows
       └─ Microsoft.Extensions.Hosting.WindowsServices

Adm.Core -X-> Adm.Infrastructure.Windows
```

`Adm.Server.Host`は共通の`CreateServerHost`でHost、`/health`、ライフサイクルを構成する。起動用途の違いはアダプターの設定だけに閉じ込める。

## 実行方法

```powershell
dotnet build .\HostingModes.sln
dotnet run --project .\src\Adm.Server.Host -- --mode console --probe
dotnet run --project .\src\Adm.Server.Host -- --mode service --probe
dotnet run --project .\src\Adm.Server.Host -- --mode manual --probe
dotnet run --project .\src\Adm.Server.Host -- --mode tray --probe
dotnet run --project .\src\Adm.Server.Host -- --mode console
```

`--probe`はHostを構築し、モード、Host実装、ヘルスエンドポイント登録を表示して終了する。通常起動は`http://127.0.0.1:5099`で待機し、`Ctrl+C`で停止する。

## 判定記録

| 確認項目 | 結果 | 証拠 |
|---|---|---|
| コンソール起動・停止 | 合格 | 4モードのprobeとconsoleの停止確認 |
| Windows Service相当起動・停止 | 合格 | `service --probe`と同一Host実装の表示 |
| 手動／トレイ起動境界 | 合格 | `manual`、`tray`の同一Host実装とアダプター表示 |
| 二重起動 | 合格 | 同一ポートで2つ目を起動した場合の明示的なアドレス使用中エラー |
| 同一ヘルスチェック | 合格 | 全モードで`/health`を登録 |
| CoreのWindows固有参照 | 合格 | Coreのプロジェクト参照がFrameworkReferenceのみであること |

`service`はWindows Service登録を行わず、Service lifetime設定を行うアダプターの境界を検証する「Windows Service相当」モードである。正式なService登録と権限差の検証は後続チケットの対象とする。
