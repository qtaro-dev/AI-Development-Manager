# P2-A06 Phase 2向けPort・Infrastructure境界

## 目的

Phase 2のProject操作、ファイル処理、走査を追加する前に、業務DataAccess、WPF実行プロファイル／Host設定、Platform Bridge、Windows Infrastructureの責務と依存方向を固定する。

## PortとBridgeの責務

| 境界 | 責務 | 許可する内容 | 配置しない内容 |
|---|---|---|---|
| 業務DataAccess Port | 業務データの取得・更新をApplicationの契約へ接続する | 将来のProject等のユースケース単位のPort | WPF設定、WebView2 API、Platform Bridge、汎用RPC |
| Host設定Port | WPF固有の実行プロファイルとHost設定を扱う | `executionProfile.get`、`executionProfile.update`と同等の既存契約 | Project、文書、添付、走査、Repository |
| Platform Bridge | Windows固有のHost操作をUIへ公開する | 明示allowlistのWindows操作 | 業務DataAccess、Project操作、ファイル処理、自動公開 |

Web UIの既存契約では、`BusinessDataAccessPort`と`HostSettingsPort`を別インターフェースとして定義する。現行のUI Compositionは後方互換のため両者を合成した`DataAccessPort`を受け取るが、将来の業務操作はBusiness側へ、execution-profileはHostSettings側へ追加する。execution-profileを業務DataAccess Portへ追加しない。

## Adapter選択とComposition

```text
React UI
  ├─ BusinessDataAccessPort
  │    ├─ Local Application Channel Adapter -> Adm.Application
  │    └─ HTTP Adapter -> Server API -> Adm.Application
  ├─ HostSettingsPort -> Local Host settings boundary
  └─ Platform Bridge -> WPF Host
```

Local modeでは業務DataAccessをLocal Application Channel Adapterへ接続し、Server modeではHTTP Adapterへ接続する。両方とも同じApplication契約へ収束させる。Host設定は業務DataAccessから分離し、WPFのHost境界で扱う。Platform Bridgeは業務Channelと別の型、dispatcher、allowlistを維持する。

Server接続を伴うWPFのhybrid compositionでは、業務DataAccessだけをHTTP Adapterへ接続し、Host設定PortはWPF側へ接続する。WPFからServer Hostを参照せず、Server HostもWPFを参照しない。

## モジュール依存行列

| モジュール | 許可参照 | 禁止参照 |
|---|---|---|
| `Adm.Core` | なし | Application、Infrastructure、Server Host、WPF、Windows固有Namespace |
| `Adm.Application` | `Adm.Core` | Infrastructure、Server Host、WPF、Windows固有Namespace |
| `Adm.Infrastructure.Windows` | `Adm.Application`, `Adm.Core`（Application Portの公開契約にCore型を含む場合） | Server Host、WPF、PoC |
| `Adm.Server.Host` | `Adm.Application`、`Adm.Infrastructure.Windows` | WPF、PoC |
| `Adm.Wpf` | `Adm.Application` | Server Host、PoC |

`Adm.Infrastructure.Windows -> Adm.Application`は、Application Portを実装するWindows Adapterに限定して許可する。Application Portの公開契約が`Adm.Core`のドメイン型を使用する場合に限り、`Adm.Infrastructure.Windows -> Adm.Core`も許可する。この補正はP2-001のCoreドメイン型を実装Adapterが扱うための内向き依存であり、Infrastructureへ業務ルールを配置する許可ではない。InfrastructureからServer HostまたはWPFへの参照は常に禁止する。Core／Applicationから上位層への逆参照も禁止する。

## ファイル処理の配置規則

Project関連のパス境界、ファイル保存、原子的保存、走査、watch、Markdown分類、RepositoryはWPFへ配置しない。Phase 2以降で必要なApplication Portを`Adm.Application`へ定義し、Windows固有の実体処理は`Adm.Infrastructure.Windows` Adapterとして実装する。具体的なProject型、Repository、保存実装は本チケットの対象外であり、専用チケットで定義する。

## 検査

Architecture検査はProjectReferenceとBuild済みAssemblyの両方について、許可集合・必須集合・禁止依存を検査する。意図的違反fixtureではInfrastructure→WPFとInfrastructure→Serverを検出し、実プロジェクトではInfrastructure→ApplicationおよびApplication Portの公開契約で必要となるInfrastructure→Coreを必須参照として確認する。

P2-A06では具体的Project型が未確定だったためInfrastructure→Coreを許可行列へ含めていなかった。P2-A07で、P2-001のCoreドメインモデルを移動・複製せず、Windows Adapterに限定してこの不足を補正した。

Local First、独立Composition Root、operation allowlist、固定origin、WebView2専用UserDataFolder、Navigation／Resource境界は変更しない。
