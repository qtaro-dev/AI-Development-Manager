# P1-030 製品モジュール境界改訂

## 目的

WindowsアプリのLocal modeと任意導入Server modeが、同じ`Adm.Application`／`Adm.Core`を利用しながら、互いのHostへ依存しない製品モジュール境界を確立する。

`Adm.Wpf`をLocal modeのComposition Root、`Adm.Server.Host`をServer modeのComposition Rootとして位置付け、参照方向をProjectReferenceとArchitecture検査で固定する。

## 背景

P1-028／ADR-019で、Windowsアプリを主製品、Server/APIを任意の追加機能とするローカルファースト実行モデルを承認した。P1-029では、Server、Kestrel、HTTP API、localhost待受を使用せず、仮想HTTPS originから組み込みReact UIをWebView2へ表示できることを条件付き採用として確認した。

現在、`Adm.Server.Host`は`Adm.Application`を参照している一方、`Adm.Wpf`は`Adm.Application`を参照していない。このままLocal modeを実装すると、WPFからServer Hostを呼ぶ、WPF内へ業務ロジックを複製する、またはPlatform Bridgeへ業務処理を混在させる危険がある。

DataAccess PortやLocal Application Channelを実装する前に、既存5プロジェクトの責務と許可参照を最小変更で確定し、自動検査で後退を防止する。

## 前提・依存関係

- P1-028承認済み
- P1-029条件付き採用として承認済み
- `design/50_ADR_019_LOCAL_FIRST_EXECUTION_MODEL.md`
- `poc/p1-029-webview2-offline-ui/results/P1-029_RESULT.md`
- P1-002の製品ソリューションと既存5プロジェクト
- P1-003のArchitecture検査
- P1-004／P1-005のBuild・Test・CI基盤
- ルート`global.json`の.NET SDK 10.0.302固定

## 対象範囲

- 既存5プロジェクトの責務表と許可参照行列
- `Adm.Wpf`から`Adm.Application`へのProjectReference
- `Adm.Wpf`と`Adm.Server.Host`を独立したComposition Rootとする境界
- `Adm.Core`と`Adm.Application`のWindows非依存維持
- WPFとServer Host間の直接参照禁止
- 製品プロジェクトからPoCへの参照禁止
- ProjectReference、Build済みAssembly、禁止NamespaceのArchitecture検査更新
- 新しい許可参照・禁止参照を検出する意図的違反fixture
- Debug／ReleaseのBuild、Test、Architecture検査
- P1-029の条件付き採用事項を後続実装の入力として記録すること

## 対象外

- 新しい製品プロジェクトまたは汎用Infrastructureプロジェクトの追加
- `Adm.Core`／`Adm.Application`への業務エンティティ、ユースケース、保存Port追加
- DataAccess PortのTypeScript実装
- Local Application ChannelのRequest、Response、Error実装
- Platform Bridgeの変更または業務操作追加
- WPF Composition RootのDI登録・起動処理実装
- 組み込みReact UIの製品WPFへの取込み
- WebView2の仮想origin、UserDataFolder、Navigation処理の製品実装
- プロジェクト登録、Markdown、チケット、添付、テスト、検索等の業務機能
- Server、API、Service、Installerの修正
- UI、画面、文言、ワイヤーフレームの変更
- P1-031以降のチケット作成または着手

## 対象ファイルまたは対象モジュール

- `src/Adm.Core/Adm.Core.csproj`
- `src/Adm.Application/Adm.Application.csproj`
- `src/Adm.Infrastructure.Windows/Adm.Infrastructure.Windows.csproj`
- `src/Adm.Server.Host/Adm.Server.Host.csproj`
- `src/Adm.Wpf/Adm.Wpf.csproj`
- `tests/Adm.Architecture.Tests/Invoke-ArchitectureBoundaryTests.ps1`
- `tests/Adm.Architecture.Tests/fixtures/`
- `tests/Adm.Architecture.Tests/README.md`
- `design/01_INTEGRATED_BASIC_DESIGN.md`
- P1-030のモジュール境界契約資料
- `tickets/phase1/P1-030_PRODUCT_MODULE_BOUNDARY_REVISION.md`
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

`poc/`はP1-029結果の読み取りだけを許可し、変更しない。`src/Adm.Web`、WPF画面コード、Server実行コード、`installer/`は変更しない。

## 製品モジュール責務

| モジュール | 責務 | 本チケット後の直接参照 |
|---|---|---|
| `Adm.Core` | ドメイン規則と値。Framework、Host、Windows、UIを知らない | なし |
| `Adm.Application` | ユースケースとPort。実行方式、UI、HTTP、Windowsを知らない | `Adm.Core` |
| `Adm.Infrastructure.Windows` | Windows Service等のWindows固有Adapter | 本チケットでは追加しない |
| `Adm.Server.Host` | Server modeのComposition Root、HTTP／Host境界 | `Adm.Application`、`Adm.Infrastructure.Windows` |
| `Adm.Wpf` | Local modeのComposition Root、WPF／WebView2／Windows UI境界 | `Adm.Application` |

`Adm.Wpf`と`Adm.Server.Host`は同じApplication契約を利用するが、相互参照しない。Application／Coreの同一インスタンス、プロセス、DI Containerを共有する意味ではなく、それぞれのプロセス内で同じ型とユースケース実装を構成する。

## 許可・禁止参照

### 必須参照

- `Adm.Application -> Adm.Core`
- `Adm.Server.Host -> Adm.Application`
- `Adm.Server.Host -> Adm.Infrastructure.Windows`
- `Adm.Wpf -> Adm.Application`

### 禁止参照

- `Adm.Core -> Adm.Application／Infrastructure.Windows／Server.Host／Wpf`
- `Adm.Application -> Infrastructure.Windows／Server.Host／Wpf`
- `Adm.Wpf -> Adm.Server.Host`
- `Adm.Server.Host -> Adm.Wpf`
- `Adm.Infrastructure.Windows -> Adm.Server.Host／Adm.Wpf`
- すべての製品プロジェクトから`poc/`またはPoC Assemblyへの参照
- `Adm.Core`／`Adm.Application`でのWPF、WebView2、Windows Service等のWindows固有Namespace使用

将来、Windows AdapterがApplication Portを実装する必要が生じた場合の`Adm.Infrastructure.Windows -> Adm.Application`は、本チケットで先行追加しない。必要な機能チケットで責務、実装、テストを確認してから許可行列を更新する。

## 具体的な実装内容

1. リポジトリ直下で`dotnet --version`を実行し、10.0.302と一致することを確認する。不一致の場合は.NETコードを変更しない。
2. 既存5プロジェクトの責務と許可・禁止参照を設計資料へ記録する。
3. `Adm.Wpf.csproj`へ`Adm.Application`のProjectReferenceを追加する。
4. `Adm.Core`、`Adm.Application`、`Adm.Infrastructure.Windows`、`Adm.Server.Host`のProjectReferenceは、上記行列と一致することを確認する。不要な参照整理が必要な場合は、理由を記録して本チケット範囲内で最小変更する。
5. Architecture検査を、Core／Applicationだけの部分的な禁止表から、5製品プロジェクトの必須・許可・禁止参照行列を検査する方式へ更新する。
6. `Adm.Wpf -> Adm.Server.Host`、`Adm.Server.Host -> Adm.Wpf`等の禁止参照を意図的違反fixtureで検出できることを確認する。
7. Build済みAssemblyについても、WPFからServer Host、Server HostからWPF、Core／Applicationから上位層への参照がないことを検査する。
8. `Adm.Core`と`Adm.Application`のWindows固有Namespace禁止検査を維持する。
9. Architecture検査のREADMEへ新しい参照行列、実行方法、fixtureの目的を記録する。
10. P1-029の条件付き採用事項であるWebView2 Evergreen Runtime、専用UserDataFolder、固定仮想HTTPS origin、Navigation／Resource境界、配布後実機回帰を、後続WPF実装の入力として設計資料へ記録する。本チケットでは実装しない。
11. Debug／ReleaseのBuild、Test、Architecture検査を実行し、再現コマンドと結果をチケットへ記録する。

## テスト内容

### SDK・Build

- `dotnet --version`が10.0.302
- `dotnet build AIDevelopmentManager.sln --configuration Debug`
- `dotnet build AIDevelopmentManager.sln --configuration Release`
- Debug／Releaseで警告・エラーを記録

### 自動テスト

- Debugの全.NETテスト
- Releaseの全.NETテスト
- DebugのArchitecture検査
- ReleaseのArchitecture検査
- 既存CI品質ゲートまたは同等の参照境界検査

### 必須参照の検査

- `Adm.Application -> Adm.Core`が存在する
- `Adm.Server.Host -> Adm.Application`が存在する
- `Adm.Server.Host -> Adm.Infrastructure.Windows`が存在する
- `Adm.Wpf -> Adm.Application`が存在する

### 禁止参照の検査

- `Adm.Core`／`Adm.Application`から上位層へ参照できない
- `Adm.Wpf -> Adm.Server.Host`を検出できる
- `Adm.Server.Host -> Adm.Wpf`を検出できる
- `Adm.Infrastructure.Windows -> Adm.Server.Host／Adm.Wpf`を検出できる
- 製品プロジェクトからPoC参照を検出できる
- Core／ApplicationのWindows固有Namespaceを検出できる
- Build済みAssemblyにも禁止依存がない

### 差分・回帰

- WPF起動ロジック、Server Host、Web UI、Installerに機能差分がない
- P1-029のPoCコード・結果を製品Solutionへ追加していない
- `AIDevelopmentManager.sln`へ新しい製品プロジェクトを追加していない
- `git diff --check`が合格する

## 完了条件

- 既存5プロジェクトの責務と参照行列が設計正本に記録されている。
- `Adm.Wpf`が`Adm.Application`を直接参照し、`Adm.Server.Host`を参照していない。
- `Adm.Server.Host`が既存どおり`Adm.Application`を利用し、`Adm.Wpf`を参照していない。
- `Adm.Core`／`Adm.Application`がHost、UI、Windows固有実装へ依存していない。
- 5製品プロジェクトの必須・許可・禁止ProjectReferenceをArchitecture検査で自動判定できる。
- 主要な禁止参照を意図的違反fixtureで検出できる。
- Debug／ReleaseのBuild、Test、Architecture検査が合格する。
- P1-029の条件付き採用事項が後続WPF実装の入力として記録されている。
- DataAccess Port、Local Application Channel、Composition RootのDI登録、組み込みUIを実装していない。
- 新しい製品プロジェクトを追加していない。
- P1-031以降を作成・着手していない。
- ユーザーがモジュール図、参照行列、検査結果を確認し、次のチケット作成可否を判断できる。

## ユーザーが目視確認する内容

- 5製品モジュールの責務図
- 必須・許可・禁止参照行列
- `Adm.Wpf -> Adm.Application`と、`Adm.Wpf -X-> Adm.Server.Host`
- `Adm.Server.Host -> Adm.Application`と、`Adm.Server.Host -X-> Adm.Wpf`
- 意図的違反fixtureが検出された結果
- Debug／ReleaseのBuild、Test、Architecture検査結果
- P1-029条件付き採用事項の引継ぎ一覧
- 製品画面と実行動作を変更していないこと

## 想定されるリスク

- WPFがApplicationではなくServer Hostを参照し、Server必須構成へ戻る。
- ApplicationへWPF、WebView2、Windows Service等のHost依存を混入させる。
- Architecture検査が禁止参照だけを確認し、必要な参照欠落を見逃す。
- ProjectReferenceだけを検査し、Build済みAssemblyやNamespaceの違反を見逃す。
- 将来必要になりそうな汎用Infrastructure、Transport、Pluginプロジェクトを先行作成する。
- P1-029のPoCコードを製品WPFへコピーする。
- 参照境界変更とLocal Application Channel、DI、UI実装を同じチケットへ混在させる。
- 既存のServer modeを削除または破壊する。

## 完了後に更新すべき設計資料

- `design/00_INDEX.md`
- `design/01_INTEGRATED_BASIC_DESIGN.md`
- `design/30_PHASE1_IMPLEMENTATION_PLAN.md`
- P1-030の製品モジュール境界契約資料
- `tests/Adm.Architecture.Tests/README.md`
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`
- `tickets/phase1/P1-030_PRODUCT_MODULE_BOUNDARY_REVISION.md`

## 完了時に残す証拠

- `global.json`の固定SDK値と`dotnet --version`実測値
- 変更前後のProjectReference一覧
- 製品モジュール責務図と参照行列
- Debug／Release Build結果
- Debug／Release Test結果
- Debug／Release Architecture検査結果
- 意図的違反fixtureの検出結果
- P1-029条件付き採用事項の引継ぎ確認
- 変更ファイル一覧と`git diff --check`結果

## 状態

実施済み（レビュー待ち）。

実装、設計記録、Debug／Release Build、Debug／Release Test、Debug／Release Architecture検査を完了した。`Adm.Wpf -> Adm.Application`を追加し、WPFとServer Hostの相互参照を禁止する5プロジェクト行列をProjectReference・Build済みAssembly検査へ反映した。意図的違反fixtureはCore→WPF、WPF→Server、Server→WPF、Infrastructure→WPFを検出した。P1-031以降は作成・実施していない。

### 実行結果

- SDK: `dotnet --version` = `10.0.302`
- Debug Build: 成功、警告0、エラー0
- Release Build: 成功、警告0、エラー0
- Debug Test: 成功、合格43、失敗0、スキップ0
- Release Test: 成功、合格43、失敗0、スキップ0
- Debug Architecture: `Architecture boundary tests passed: 5 product projects (Debug).`
- Release Architecture: `Architecture boundary tests passed: 5 product projects (Release).`
- `git diff --check`: 合格

P1-030の対象ファイル以外の既存未コミット変更、P1-029結果差分、生成物はコミット対象へ含めていない。
