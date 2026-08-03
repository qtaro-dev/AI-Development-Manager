# AI Development Manager フェーズ計画

版: 0.7-repository-rules

## Phase 0 設計確定・技術PoC

目的: 高リスク技術とUI基準を、製品実装前に確定する。

## Phase 0ゲート結果（P0-023）

既存P0-001～P0-026の結果を採用、条件付き採用、保留、不採用へ整理した。Phase 0の設計確定ゲートは完了したが、Phase 1の開始はユーザー承認待ちとする。Phase 1では、検索品質・性能、Windows実機境界、実データ互換、ブラウザ互換性、添付容量、SQLite依存更新の配布確認を条件付き採用の確認事項として先に扱う。判定の詳細は`29_PHASE0_DESIGN_GATE_DECISION.md`を参照する。

成果:

- リポジトリ規約、Build番号運用、品質ゲート
- Markdown／`.adm-meta`／ID仕様
- API設計方針
- 技術PoC結果とADR
- LAN HTTPS初期設定方式
- React採否
- ワイヤーフレームと基準画面
- DevTicketManager互換PoC結果（サンプル提供後）

P0-001完了時点の確定資料:

- `../AGENTS.md`
- `06_REPOSITORY_RULES.md`
- `00_INDEX.md`の版・状態・収録資料

終了条件: Gate Cを通過し、未決事項が実装を妨げない状態。

## Phase 1 実行基盤

目的: 機能を載せる前にServer、Web、WPF、共通品質基盤を作る。

範囲:

- ASP.NET Core Server骨格
- 共通Web UI骨格
- WPF WebView2シェル
- OpenAPI、共通エラー、ログ、設定
- Theme、文言辞書、共通Component
- CI、単体／統合／UIテスト基盤

対象外: プロジェクト走査やチケット機能。

## Phase 2 LAN・認証・プロジェクト登録

目的: 安全なLAN利用と管理対象の登録を成立させる。

範囲:

- 初回管理者
- Cookie認証とAPIトークン
- ロールとプロジェクト権限
- HTTPS初期設定とFirewall案内
- プロジェクト登録・解除
- ローカルNTFS境界検証

## Phase 3 ファイル走査・Markdown判別・索引

目的: 既存資料を変更せず、安全に認識する。

範囲:

- 初回走査、監視、定期再走査、手動再走査
- Front Matter解析
- 文書種別判別と信頼度
- 判別不能表示と`.adm-meta`手動分類
- ULID割当と相対パス追跡
- SQLite索引キャッシュと再構築

## Phase 4 チケット・資料・添付閲覧

目的: DevTicketManagerの主要な閲覧用途を統合する。

範囲:

- Markdown一覧と詳細
- 名前、作成日、更新日、状態の並べ替え
- キーワード絞り込み
- 利用者ごとの確認状態
- 画像、PDF、ログ、動画、ZIPの安全な閲覧
- ブラウザのパス表示・コピー・ダウンロード
- WPFの元ファイル／フォルダー操作

## Phase 5 テストケース表示

目的: Markdownテストケースを説明書なしで確認できる表として表示する。

範囲:

- 大項目、中項目、小項目、内容、手順、期待結果
- 固定見出し、主要列、長文展開
- 絞り込みと並べ替え
- 読取エラーと欠落項目の表示

対象外: 結果保存。

## Phase 6 テスト結果入力・保存

目的: 複数回・複数環境の実施結果を安全に記録する。

範囲:

- `passed`、`warning`、`failed`、`not_tested`、`blocked`
- 備考と添付
- 実施者、環境、`execution_id`
- 別Markdownへの保存
- ETag競合、差分確認、再読込
- 原子的保存、回復ジャーナル、監査

## Phase 7 検索・AIコンテキスト出力

目的: 人とAIが根拠資料へ到達できるようにする。

範囲:

- キーワード、エラーコード、属性検索
- 根拠ファイルと一致箇所
- チケット、テスト、結果、添付一覧の選択
- コンテキストのMarkdown／ZIP出力
- 読み取り専用API
- 欠落資料の明示

対象外: 意味検索とAI回答生成。

## Phase 8 バックアップ・復元・MVP統合

目的: 実運用に必要な復旧性とMVP全体品質を確定する。

範囲:

- 保存前・移行前・AI書込前バックアップ
- 保持設定と容量管理
- 復元プレビューと復元後検証
- 索引再構築
- DevTicketManager互換／移行
- LAN複数ユーザー総合試験
- セキュリティ、性能、UI回帰、復元演習

終了条件: 統合基本設計書のMVP完了条件を満たす。

## Phase 9 AI Workbench連携

目的: 単体で成立しているコンテキスト出力をAI Workbenchから利用可能にする。

範囲:

- 接続設定
- 読み取りAPI連携
- 参照元表示
- 接続失敗時の単体利用継続

## Phase 10 将来拡張

候補:

- 意味検索、類似不具合検索
- AIチャット
- GitHub連携
- 通知
- UNC／NAS条件付き対応
- 高度な分析
- Penguin Assistant
- 利用実績に基づくプラグイン方式

Penguin Hub／Penguin OSは別製品計画として扱い、本計画へ混在させない。

## フェーズ依存関係

```text
Phase 0
  -> Phase 1
      -> Phase 2
          -> Phase 3
              -> Phase 4
                  -> Phase 5
                      -> Phase 6
                          -> Phase 7
                              -> Phase 8 (MVP)
                                  -> Phase 9
                                      -> Phase 10
```

一部の内部作業は並行可能だが、ユーザーが一件ずつ実装・レビュー・実機確認できるよう、詳細チケットでは依存関係を崩さず小さく分割する。

## P1-028による実行モデル更新

Phase 1の主製品はWindowsアプリとし、Server/APIは任意導入の追加機能とする。P1-028のADR-019レビュー後に、Local Application Channel、DataAccess Port、初回起動導線を独立チケットとして扱う。認証、HTTPS、権限、LAN公開はServer modeおよび後続フェーズの境界として維持し、P1-028では実装しない。
