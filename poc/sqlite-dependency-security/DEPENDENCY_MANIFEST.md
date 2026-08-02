# P0-026 依存・ライセンス・SBOM記録

基準日: 2026-08-03

## Baseline

`Microsoft.Data.Sqlite 10.0.10`から次を解決した。

| Package | Version | License | Audit |
|---|---:|---|---|
| Microsoft.Data.Sqlite | 10.0.10 | MIT | direct |
| Microsoft.Data.Sqlite.Core | 10.0.10 | MIT | transitive |
| SQLitePCLRaw.bundle_e_sqlite3 | 2.1.11 | Apache-2.0 | vulnerable |
| SQLitePCLRaw.core | 2.1.11 | Apache-2.0 | transitive |
| SQLitePCLRaw.lib.e_sqlite3 | 2.1.11 | Apache-2.0 | NU1903 High |
| SQLitePCLRaw.provider.e_sqlite3 | 2.1.11 | Apache-2.0 | transitive |

## Candidate

| Package | Version | License | Audit |
|---|---:|---|---|
| Microsoft.Data.Sqlite.Core | 10.0.10 | MIT | direct |
| SQLitePCLRaw.bundle_e_sqlite3 | 3.0.3 | Apache-2.0 | direct |
| SQLitePCLRaw.config.e_sqlite3 | 3.0.3 | Apache-2.0 | transitive |
| SQLitePCLRaw.core | 3.0.3 | Apache-2.0 | transitive |
| SQLitePCLRaw.provider.e_sqlite3 | 3.0.3 | Apache-2.0 | transitive |
| SourceGear.sqlite3 | 3.50.4.5 | package LICENSE.txt | transitive native SQLite |

`dotnet list package --vulnerable --include-transitive`はBaselineで`SQLitePCLRaw.lib.e_sqlite3 2.1.11`をHighとして検出し、Candidateでは脆弱なパッケージなしと報告した。上表とこのコマンド出力をP0-026の簡易SBOM・監査証跡とする。正式運用ではCIで同コマンド、ライセンス一覧、publishファイル一覧を保存する。

## 更新・ロールバック

更新は候補依存をロックファイルとpublish成果物へ反映し、restore・監査・FTS回帰・実機起動を通過した成果物だけを段階配置する。失敗時は直前のpublish成果物と依存ロックへ戻し、SQLiteキャッシュは削除後にMarkdownから再構築する。SQLiteは正本ではないため、DBファイルをバックアップ正本として扱わない。
