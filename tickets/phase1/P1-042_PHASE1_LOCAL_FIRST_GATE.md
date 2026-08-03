# P1-042 Phase 1ローカルファースト再判定ゲート

## 目的

P1-028～P1-041で改訂したローカルファースト設計・実装・配布・実機証拠を監査し、Phase 1の正式完了とPhase 2開始可否を判定する。

## 背景

旧P1-027後の実機検証により、Server前提とFramework-dependent Clientでは主製品の利用を開始できないことが判明した。本ゲートはWindowsアプリ単体運用をPhase 1の正式基準とし、任意機能であるServer MSI課題をLocal modeの完了条件から分離して最終判定する。

## 前提・依存関係

- P1-001～P1-041の必要成果が完了していること
- 特にP1-034～P1-041が完了・承認済みであること
- P1-041のクリーンVM結果が利用可能であること
- `design/49_PHASE1_INTEGRATION_GATE_RESULT.md`
- `design/50_ADR_019_LOCAL_FIRST_EXECUTION_MODEL.md`

## 対象範囲

- Local-first責務、依存方向、Channel境界の監査
- Local／HTTP契約一致の監査
- 実行プロファイルと起動UXの監査
- Self-contained Client、MSI、クリーンVM証拠の監査
- 全品質ゲートと未解決事項の確認
- Server MSI課題の任意Serverトラックへの引継ぎ
- Phase 1完了／条件付き完了／未完了の判定
- Phase 2入力、禁止事項、先行是正チケットの整理

## 対象外

- 新規製品機能、コード、UI、MSIの修正
- Server MSI／Serviceの修正実装
- Phase 2詳細チケットの一括作成
- 認証、LAN HTTPS、プロジェクト登録等の先行実装
- 不合格条件の緩和や証拠の推測補完

## 対象ファイルまたは対象モジュール

- P1-001～P1-041のチケット、設計、テスト、実機証拠
- `design/00_INDEX.md`
- `design/30_PHASE1_IMPLEMENTATION_PLAN.md`
- `design/49_PHASE1_INTEGRATION_GATE_RESULT.md`
- `design/50_ADR_019_LOCAL_FIRST_EXECUTION_MODEL.md`
- Phase 2計画資料
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`
- 本チケット

## 具体的な実装内容

1. 各チケットの目的、対象外、完了条件、証拠、ユーザー承認を追跡表で監査する。
2. WPFからServer Hostへの参照禁止、Serverなし起動、localhost待受なしを再確認する。
3. Local／HTTPの意味的一致、ChannelとPlatform Bridgeの分離、最小契約維持を確認する。
4. Self-contained成果物、MSI、クリーンVM単体運用結果を正式配布基準と照合する。
5. Build、Test、Architecture、Web、Playwright、依存脆弱性、SBOM、MSI検査を再実行または最新証拠で確認する。
6. Server MSI／Serviceの未解決事項を任意Server機能の後続へ移し、Local modeの合否と混同しない。
7. 不合格があればゲート内で修正せず、原因・影響・依存を持つ是正チケット候補を作る。
8. Phase 2へ送る範囲、前提、未決事項、開始条件を確定する。

## テスト内容

- 固定SDKによるDebug／Release clean build
- .NET、Architecture、Web unit、型検査、Playwright
- 依存脆弱性、ライセンス、SBOM
- Local／HTTP契約一致
- Serverなし・.NET RuntimeなしのクリーンVM結果
- Client MSI install／repair／upgrade／uninstall
- UI基準画像・Vol.5ガードレール確認
- 文書リンク、版数、状態、証拠パスの整合性検査

## 成功条件

- Windows 11 64-bitでServerと.NET Runtimeなしに主製品を導入・起動・Local利用できる。
- Server障害・未導入・停止がLocal利用を妨げない。
- LocalとHTTPが同じApplication意味を持ち、Host間参照がない。
- 全必須品質ゲートが正常終了し、重大・高リスク未解決がない。
- Phase 2の入力とServer任意機能の残課題が明確に分離されている。

## 完了条件

- 監査表、正式判定、根拠、例外、未解決事項、Phase 2引継ぎが署名可能な形で保存されている。
- ユーザーがPhase 1最終判定を確認・承認している。
- ゲート内で実装修正やPhase 2実装を行っていない。

## P1-041再試験反映後の判定

### 判定

Phase 1の正式完了は保留とする。Local-first配布・起動基盤の再試験結果はP1-041へ反映したが、利用者導線とClient MSI表示に未解決事項が残っているため、現時点で「全必須品質ゲートが正常終了し、重大・高リスク未解決がない」という成功条件を満たしたとは判定しない。

### 是正チケット

| チケット | 内容 | Phase 1ゲートへの扱い |
|---|---|---|
| P1-044 | 初回設定の保存・再読込・保存後画面遷移の是正 | Local-first起動設定の受入れ前提。実施時期・優先順位はPhase 2開始時に判断 |
| P1-045 | Client MSIの日本語化および正式なLicense内容への差し替え | 配布品質の是正候補。実施時期・優先順位はPhase 2開始時に判断 |

上記チケットは作成のみであり、本ゲートでは実装しない。P1-041の初回試験結果は書き換えず、P1-043修正版の再試験結果を追記した。Server、Windows Service、LAN接続は今回の判定対象へ追加していない。

## ユーザーが目視確認する内容

- Phase 1合否サマリーと根拠
- クリーンVM単体運用の証拠
- 必須品質ゲート一覧
- Server任意機能へ送る課題
- Phase 2開始条件と未決事項

## 想定されるリスク

- Server MSI不具合とLocal主製品の合否を再び混同する。
- テスト未実行を過去結果や推測で合格扱いする。
- ゲート内で修正し、レビュー可能な変更単位を失う。
- Playwright等の異常終了をテスト合格だけで見逃す。
- Phase 2の詳細実装を先行確定する。

## 完了後に更新すべき設計資料

- `design/00_INDEX.md`
- `design/30_PHASE1_IMPLEMENTATION_PLAN.md`
- `design/49_PHASE1_INTEGRATION_GATE_RESULT.md`
- `design/50_ADR_019_LOCAL_FIRST_EXECUTION_MODEL.md`
- Phase 2計画・未決事項資料
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`
- 本チケット

## 完了時に残す証拠

- P1-001～P1-041監査表
- 全品質ゲート結果と`dotnet --version`
- クリーンVM、MSI、Local単体運用証拠索引
- 未解決事項・是正候補・Phase 2引継ぎ一覧
- ユーザー最終承認記録
- `git diff --check`結果

## 状態

再判定実施。正式完了保留（P1-044／P1-045の是正判断待ち）。
