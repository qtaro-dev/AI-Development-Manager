# AI Development Manager 設計資料索引

版: 1.8-p0-012-zip-safety
状態: P0-012完了
基準日: 2026-08-02

## 目的

引き渡し資料v0.6とユーザー承認事項を、実装前に参照できる統合設計へ整理する。
本版では技術方針、基本設計、Phase 0のPoC、フェーズ計画を定義する。
詳細な実装チケットはユーザー確認後に別資料として作成する。

## 優先順位

1. ユーザーが明示的に承認・追加した方針
2. 本フォルダーの統合設計資料
3. 引き渡し資料ルートのBaseline文書
4. DesignBook Vol.5 UI Guardrails
5. DesignBook Vol.1～Vol.4

## 収録資料

- `01_INTEGRATED_BASIC_DESIGN.md`: 統合基本設計書
- `02_TECHNOLOGY_AND_ADR.md`: 採用技術案と技術ADR
- `03_PHASE_0_POC_PLAN.md`: Phase 0 PoC一覧、順序、合格条件
- `04_PHASE_PLAN.md`: MVPおよび将来フェーズ計画
- `05_OPEN_DECISIONS.md`: 追加判断事項と推奨初期値
- `06_REPOSITORY_RULES.md`: リポジトリ構成、Build番号、品質ゲート、除外規則
- `../poc/common/`: P0-003共通評価環境、ワークロード、結果テンプレート
- `../poc/fixtures/`: P0-004 Markdown検証コーパスとmanifest
- `07_MARKDOWN_PARSING_CONTRACT.md`: P0-005 Markdown解析契約とPoC結果
- `08_DOCUMENT_CLASSIFICATION_CONTRACT.md`: P0-006 文書種別自動判別契約とPoC結果
- `09_ADM_META_ID_CONTRACT.md`: P0-007 `.adm-meta`、ULID、連番契約とPoC結果
- `10_ATOMIC_SAVE_CONTRACT.md`: P0-008 NTFS原子的保存契約とPoC結果
- `11_ETAG_CONCURRENCY_CONTRACT.md`: P0-009 ETag競合検知契約とPoC結果
- `12_RECOVERY_JOURNAL_CONTRACT.md`: P0-010 保存回復ジャーナル契約とPoC結果
- `13_PATH_SECURITY_CONTRACT.md`: P0-011 パス境界・リンク安全性契約とPoC結果
- `14_ZIP_SAFETY_CONTRACT.md`: P0-012 ZIP安全閲覧契約とPoC結果
- `../tickets/phase0/00_PHASE_0_TICKET_INDEX.md`: Phase 0詳細チケット一覧と依存関係

## 人向け概要

- `../output/pdf/AI_Development_Manager_Overview_v0.6.pdf`

## 本版で確定した前提

- 独立ASP.NET Core Serverを採用する。
- WPFと通常ブラウザは同じWeb UIを利用する。
- Reactの正式採用はPhase 0 PoCの合格後に確定する。
- Server PC上のローカルNTFSをMVPの正式ストレージとする。
- LAN通信もHTTPSを必須とする。
- Markdownと添付を業務データの正本とする。
- 既存文書の補助情報は`.adm-meta`へ保存し、原本を無断変更しない。
- SQLiteは削除して再構築できる索引キャッシュに限定する。
- テスト結果はテストケースと別Markdownに保存する。
- 業務データ操作はAPI経由に統一する。
- WPFブリッジはWindows固有操作だけを担当する。
- 汎用プラグイン、意味検索、AIチャット、Penguin AssistantはMVP対象外とする。
- Serverの正式運用はWindows Serviceとし、コンソール・手動・任意のトレイ起動も同じHost構成で維持する。
- 正式対応OSはWindows 11 64-bitとする。
- Server業務ロジックとWeb UIをWindows固有APIへ直接依存させない。
- 大容量添付は進捗、取消、失敗理由、再試行を提供する。

## 未作成

- UIワイヤーフレームと基準画面
- APIのOpenAPI契約ファイル
- MarkdownスキーマのJSON Schema相当定義
- Phase 1以降のフェーズ別詳細チケット

これらは本レビューの承認後、Phase 0成果物または詳細チケットとして作成する。

## P0-001完了資料

- `../AGENTS.md`: 開発者・AIエージェント向けの作業規約
- `06_REPOSITORY_RULES.md`: P0-001で確定したリポジトリ規約
