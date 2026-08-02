# P0-026 SQLite依存更新・安全性PoC

## 目的

.NET 10製品基準を維持しながらSQLiteネイティブ依存のNU1903警告を解消し、FTS5、配布、実行、脆弱性管理を安全に継続できる正式依存構成を確定する。

## 背景

P0-014およびP0-015では、`Microsoft.Data.Sqlite 10.0.10`の推移依存として`SQLitePCLRaw.lib.e_sqlite3 2.1.11`が解決され、既知の高重大度脆弱性に対するNU1903警告が発生した。

P0-015は測定完了として扱うが、既知のHigh/Critical脆弱性を残した構成は製品へ採用できない。一方で、製品基準を.NET 11へ変更する、Windows付属SQLiteへ無条件に切り替える、暗号化機能を目的なく追加するといった範囲拡大も避ける必要がある。

本チケットでは、`Microsoft.Data.Sqlite.Core 10.0.10`とSQLitePCLRaw 3.xバンドルの明示参照を第一候補として検証し、安全で再現可能な依存固定方法を確定する。

## 前提・依存関係

- P0-003完了
- P0-014測定完了、正式採用保留
- P0-015測定完了、正式採用保留
- `design/16_SQLITE_FTS_JA_CONTRACT.md`
- `design/18_P0_015_PERFORMANCE_REVIEW.md`
- リポジトリ直下の`global.json`で固定された.NET SDK 10.0.302が導入済みであること

P0-024およびP0-025とは独立して実施できる。ただし、製品採用判断は3件の結果をP0-023で統合して行う。

## 対象

- `Microsoft.Data.Sqlite.Core 10.0.10`
- `SQLitePCLRaw.bundle_e_sqlite3` 3.xの明示参照
- 実施時点で利用可能な.NET 10互換の安全な servicing 更新
- restore、build、test、publish時の推移依存
- 実行時にロードされるSQLiteネイティブライブラリ
- `select sqlite_version()`による実バージョン確認
- FTS5の有効性とP0-014検索契約
- Framework-dependent配布と`win-x64`配布
- Windows Service、コンソール、WPFホストで共有可能なデータアクセス層
- 脆弱性監査、依存固定、ライセンス、SBOM

## 対象外

- 製品基準の.NET 11以降への変更
- SQLite以外のデータベースへの全面移行
- データベース暗号化機能の製品採用
- FTS検索クエリと索引構造の性能最適化
- 製品Server、WPF、Web UIへの本実装
- Windows付属SQLiteを正式採用すること

## 対象ファイルまたは対象モジュール

- 新規PoC: `poc/sqlite-dependency-security`
- 比較元: `poc/sqlite-fts-ja`
- 参照候補: `poc/performance`
- 設計資料: `design/16_SQLITE_FTS_JA_CONTRACT.md`
- レビュー記録: `design/18_P0_015_PERFORMANCE_REVIEW.md`
- ルート`global.json`

## PoC内容

1. `dotnet --version`を実行し、固定SDK 10.0.302との一致を開始条件として確認する。
2. 現行構成の直接依存、推移依存、NU1903警告、実SQLiteバージョンを基準値として記録する。
3. 第一候補として次の組み合わせを独立PoCへ設定する。
   - `Microsoft.Data.Sqlite.Core 10.0.10`
   - `SQLitePCLRaw.bundle_e_sqlite3` 3.xの明示参照
4. 実施時点で安全な.NET 10 servicing版が存在する場合は、同一評価項目で代替候補として比較する。
5. clean restore、Debug/Release build、自動テストを実行する。
6. Framework-dependentおよび`win-x64`の必要な配布候補をpublishし、配置ファイルを一覧化する。
7. 実行時にロードされたネイティブSQLiteのファイル、バージョン、アーキテクチャを確認する。
8. `select sqlite_version()`を記録し、既知脆弱性の修正を含む版であることを確認する。
9. FTS5テーブル作成、unicode61、trigram、MATCH、BM25、snippet、更新、削除、再構築を実行する。
10. Windows Service相当Host、コンソールHost、WPFから共通ライブラリを参照できる依存境界を確認する。Windows固有処理をSQLiteアクセス層へ混入させない。
11. NuGet監査結果、ロック可能な依存バージョン、ライセンス一覧、SBOM出力方法を記録する。
12. 候補ごとの利点、制約、更新方法、ロールバック方法を比較する。

## テスト内容

- clean環境相当のrestore
- Debug/Release build
- Framework-dependent publish
- `win-x64` publish
- 実SQLiteバージョン確認
- x64ネイティブDLLのロード確認
- FTS5、unicode61、trigramの利用確認
- P0-014の日本語、英数字、エラーコード検索回帰
- SQLite削除後の再構築
- Windows Service相当HostとコンソールHostからの起動
- WPF参照を想定した共通ライブラリ境界
- NuGet脆弱性監査
- 直接依存・推移依存一覧
- ライセンスおよびSBOM出力

## 成功条件

- ルート`global.json`の.NET SDK 10.0.302でrestore、build、test、publishが成功する。
- `dotnet --version`の実測値とRuntimeを結果へ記録する。
- NU1903が0件であり、High/Criticalの既知脆弱性警告が0件である。
- 実行時SQLiteバージョンが対象アドバイザリの修正版以降である。
- Framework-dependentおよび必要な`win-x64`配布で、意図したx64ネイティブSQLiteだけがロードされる。
- FTS5、unicode61、trigram、BM25、snippetが利用できる。
- P0-014の必須検索結果と再構築性を維持する。
- Windows Service、コンソール、WPFで共有できるデータアクセス層の依存構成を説明できる。
- 依存バージョンを再現可能に固定し、直接依存と推移依存を一覧化できる。
- ライセンスとSBOMを生成または記録できる。
- 安全な更新手順と、更新失敗時のロールバック手順がある。

成功条件を満たさない場合は現行依存を正式採用せず、利用可能なservicing更新待ちまたは別バンドル検証を未決事項として残す。

## 製品採用判断基準

次をすべて満たす依存構成をMVP採用候補とする。

- .NET 10製品基準と互換性がある。
- High/Criticalの既知脆弱性を含まない。
- FTS5とP0-014の検索契約を維持する。
- Windows 11 64-bitへ再現可能に配布できる。
- Windows Service、コンソール、WPFで同一のデータアクセスライブラリを使用できる。
- ネイティブSQLiteのバージョンとロード元を製品側で統制できる。
- 依存更新、脆弱性監査、ライセンス、SBOMの運用手順を定義できる。
- SQLiteを再構築可能なキャッシュとして扱う設計を変更しない。
- 製品に不要な暗号化機能、別DB、OS依存を追加しない。

第一候補が成功条件を満たす場合は、`Microsoft.Data.Sqlite.Core`と安全な`SQLitePCLRaw.bundle_e_sqlite3` 3.xの明示参照を採用する。複数候補が合格する場合は、Microsoft.Data.Sqliteとの互換性、保守状況、配布の単純さ、更新経路を優先する。

## ユーザーが目視確認する内容

- 変更前後の依存関係と警告件数
- 実際にロードされたSQLiteバージョン
- FTS5回帰テスト結果
- publish成果物のネイティブDLL一覧
- 採用候補、代替案、更新・ロールバック手順

## 想定されるリスク

- Microsoft.Data.Sqlite.CoreとSQLitePCLRaw 3.xの組み合わせ互換性
- publish方式によるネイティブDLL配置差
- 実施後に新しい脆弱性情報が公開される
- 推移依存の下限指定により意図しない版が解決される
- PoCでは成功してもWindows Service配置先でDLLロードが異なる

## 完了後に更新すべき設計資料

- `design/16_SQLITE_FTS_JA_CONTRACT.md`
- `design/17_PERFORMANCE_CONTRACT.md`
- `design/18_P0_015_PERFORMANCE_REVIEW.md`
- `design/02_TECHNOLOGY_AND_ADR.md`のSQLite依存ADR
- 依存パッケージおよびSBOM運用方針
- `tickets/phase0/00_PHASE_0_TICKET_INDEX.md`

## 実施結果（2026-08-03）

`poc/sqlite-dependency-security`でBaselineとCandidateを比較した。SDK 10.0.302、Runtime 10.0.10。Baseline（`Microsoft.Data.Sqlite 10.0.10`）はSQLite 3.49.1、推移依存`SQLitePCLRaw.lib.e_sqlite3 2.1.11`、NU1903 Highを再現した。Candidate（`Microsoft.Data.Sqlite.Core 10.0.10` + `SQLitePCLRaw.bundle_e_sqlite3 3.0.3`）はSQLite 3.50.4、脆弱性監査該当なし、FTS5・unicode61・trigram、Framework-dependent publish、win-x64 publish、x64 native DLLロードに合格した。Candidateを正式採用候補として保持し、Windows Service／コンソール／WPF相当Hostの実機境界、CIでのSBOM・ライセンス保存、正式採用はP0-023のレビュー事項とする。P0-027以降には着手していない。
