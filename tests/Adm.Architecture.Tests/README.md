# Adm.Architecture.Tests

P1-003／P1-030の参照方向・Windows依存境界検査です。P1-004のxUnit/TestServer基盤とは分離し、PowerShellで5製品プロジェクトのProjectReference、ビルド済みAssembly参照、禁止Namespace、PoC参照を検査します。

## P1-030許可参照行列

| プロジェクト | 許可する製品参照 | 必須参照 |
| --- | --- | --- |
| `Adm.Core` | なし | なし |
| `Adm.Application` | `Adm.Core` | `Adm.Core` |
| `Adm.Infrastructure.Windows` | `Adm.Application` | `Adm.Application` |
| `Adm.Server.Host` | `Adm.Application`, `Adm.Infrastructure.Windows` | 同左 |
| `Adm.Wpf` | `Adm.Application` | `Adm.Application` |

`Adm.Wpf`と`Adm.Server.Host`はそれぞれLocal mode／Server modeの独立Composition Rootであり、相互参照しません。`Adm.Infrastructure.Windows`はWindows固有Adapterとして、Application Portを実装する場合に限り`Adm.Application`を参照できます。Project関連のファイル処理、path security、atomic save、scan/watchはWPFへ配置せず、将来のWindows AdapterとしてInfrastructure境界へ配置します。Core／ApplicationはWindows固有Namespaceを参照せず、全製品プロジェクトからPoC参照を禁止します。ProjectReferenceでは許可集合と必須集合を、Build済みAssemblyでは許可集合と禁止依存を検査します。

`fixtures/`には、Core→WPF、WPF→Server、Server→WPF、Windows Infrastructure→WPF、Windows Infrastructure→Serverの意図的違反を置き、検査が禁止参照を検出できることを確認します。Infrastructure→Applicationは許可参照として実プロジェクトとBuild済みAssemblyの両方で確認します。

実行方法（Debug成果物を検査）:

```powershell
pwsh -NoProfile -File .\tests\Adm.Architecture.Tests\Invoke-ArchitectureBoundaryTests.ps1 -Configuration Debug
```

実行前に、ルートの固定SDKでSolutionをビルドしてください。fixtureの意図的な違反が検出されることも同じ実行で確認します。
