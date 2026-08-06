# P2-010 Phase 2 Local Project登録統合ゲート結果

版: 1.0
判定日: 2026-08-07
状態: 合格・正式完了

## 1. 最終判定

P2-001～P2-009の個別実装、P2-A07／P2-A08のArchitecture補正、最新成果物の.NET回帰、および最新MSIを用いたクリーンWindows 11 VM手動E2Eを総合し、P2-010を合格とする。

Phase 2 Local Project登録機能は正式完了した。Windowsアプリ単体のLocal modeで、Project登録、一覧、選択、選択解除、登録解除、Catalog永続化、Root異常時の安全な管理を実行できる。登録・解除は利用者のProject Rootと内部ファイルを削除しない。

## 2. 対象実装

| チケット | Commit | 結果 |
|---|---|---|
| P2-001 | `ebec8d5` | Project契約・Coreドメイン |
| P2-A07 | `94292bd` | Infrastructure→Core境界補正 |
| P2-002 | `2910db8` | Local NTFS Root検証 |
| P2-003 | `85d23e9` | Project Catalog永続化 |
| P2-004 | `aafbf5d` | Project登録Use Case |
| P2-005 | `243ecb3` | Project登録解除Use Case |
| P2-006 | `b600ed3` | Project一覧・選択Use Case |
| P2-A08 | `2aa23a8` | WPF Composition境界補正 |
| P2-007 | `01f7cfb` | Project DataAccess／Local Channel |
| P2-008 | `15c840c` | Windows folder picker Bridge |
| P2-009 | `9523cfe` | Local Project Web UI |

## 3. 確認環境

| 項目 | 内容 |
|---|---|
| 手動E2E | クリーンWindows 11 VM |
| 表示条件 | 1280×800、100% |
| 配布物 | 最新x64 Release Client MSI |
| Local runtime | Self-contained .NET、WebView2 Evergreen Runtime前提 |
| .NET再確認 | SDK 10.0.302、P2-009後の既存Debug／Release成果物 |
| ブランチ | `agent/p2-a06-port-infrastructure-boundary` |
| E2E対象HEAD | `9523cfe` |

リポジトリ側のClient MSI manifestはSHA-256 `02FC84ED5716A6DFDCB1A9C00CE0D454802EB4D0B687073973CCFB4BB6BB9F6C`を記録している。VMへ投入したMSIのhashをVM側で再採取した証拠は今回提供されていないため、同一hashであるとの独立検証までは主張しない。

## 4. 手動E2E結果

| 分類 | 確認内容 | 結果 |
|---|---|---|
| Install | 最新MSIのクリーンインストール | 合格 |
| Startup | 初回起動、「このPCで続ける」、Local mode開始 | 合格 |
| Profile | `execution-profile.json`作成、終了・再起動後のLocal復元 | 合格 |
| Viewport | 1280×800、100%で表示・操作 | 合格 |
| Folder picker | 選択成功、キャンセル | 合格 |
| Project | 登録、一覧、選択、選択解除、登録解除 | 合格 |
| Persistence | 再起動後のCatalog／選択状態復元 | 合格 |
| Validation | 同一Root重複拒否、network Root拒否 | 合格 |
| Root anomaly | 手動rename後の警告、登録情報維持、管理操作継続 | 合格 |
| Data safety | 登録解除後も実フォルダー・内部ファイルを削除しない | 合格 |
| Shutdown | 終了処理、クラッシュ・残留問題なし | 合格 |

## 5. 自動回帰確認

2026-08-07、コード変更を行わず、P2-009後に生成済みの成果物へ次を実行した。

```powershell
dotnet test AIDevelopmentManager.sln --configuration Debug --no-build --no-restore --verbosity minimal
dotnet test AIDevelopmentManager.sln --configuration Release --no-build --no-restore --verbosity minimal
```

| 構成 | Core | Application | Infrastructure.Windows | Server | 合計 | 失敗 | Skip |
|---|---:|---:|---:|---:|---:|---:|---:|
| Debug | 7 | 39 | 83 | 23 | 152 | 0 | 0 |
| Release | 7 | 39 | 83 | 23 | 152 | 0 | 0 |

最新のDebug／Release WPF成果物、Release publish、Web bundle、Client MSIがP2-009 HEAD直後に生成されていることも確認した。

## 6. 維持された設計境界

- Local modeはServer、Kestrel、HTTP、localhost待受を必須にしない。
- Project業務操作はBusinessDataAccess PortとLocal Application Channelを通る。
- folder pickerだけをPlatform Bridgeへ置き、Project登録処理をBridgeへ置かない。
- Local Channel／Bridge operationは明示allowlist方式を維持する。
- WPFはComposition Rootに限定してInfrastructureを構成し、業務ルールを保持しない。
- Project登録・解除はアプリ側Catalogだけを変更し、Project Root配下へ書き込まない。
- Root異常時に登録情報を暗黙削除しない。

## 7. 未実施事項・証拠制約

- 本PCにはPowerShell 7がなく、Architecture検査は本完了処理では再実行していない。Windows PowerShell 5.1では検査内の`Path.GetRelativePath`を実行できなかった。
- 本PCにはリポジトリ固定Node.js 22.18.0／npmがなく、Web typecheck、lint、unit test、bundle検査は本完了処理では再実行していない。Codex同梱Node 24.14.0への置換や環境変更は行っていない。
- VM上のMSI SHA-256再採取、登録前後のProject tree／全ファイルhash比較は個別証拠として未採取。
- 表示倍率125%～200%およびkeyboard-only操作は今回のVM E2Eで個別再確認していない。
- UNC／NAS正式対応、Server認証／HTTPS／LAN、走査、watch、Markdown解析、`.adm-meta`文書ID、SQLite索引は対象外であり未実装。

Architecture／WebはP2-A07／P2-A08／P2-007～P2-009の個別完了時の境界を引き継ぎ、固定toolchainが利用可能な環境で次フェーズ開始前の通常品質ゲートとして再実行する。上記は今回合格したLocal Project登録の中核動作に対するblockerとはしない。

## 8. 次工程

次はPhase 3「ファイル走査・Markdown判別・索引」の開始レビューと詳細チケット作成を行う。Project Root配下への書込みを開始する前に、`.adm-meta`所有リース、Project identity、原子的保存の適用順を設計ゲートとして確定する。走査、watch、Markdown解析、文書分類、ULID割当、SQLite索引を一つのチケットへ混在させず、依存順に分割する。
