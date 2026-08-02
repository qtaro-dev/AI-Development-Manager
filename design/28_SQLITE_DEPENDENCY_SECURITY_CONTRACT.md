# P0-026 SQLite依存更新・安全性契約

版: 1.0-p0-026
状態: 測定完了、候補構成を正式採用候補として保留
基準日: 2026-08-03

## 比較結果

| 項目 | Baseline | Candidate |
|---|---|---|
| 依存 | Microsoft.Data.Sqlite 10.0.10 | Microsoft.Data.Sqlite.Core 10.0.10 + SQLitePCLRaw.bundle_e_sqlite3 3.0.3 |
| 解決SQLite | 3.49.1 | 3.50.4 |
| NU1903 | High 1件 | 0件 |
| FTS5/unicode61/trigram | 合格 | 合格 |
| win-x64 native DLL | e_sqlite3.dll | e_sqlite3.dll 1件 |
| SDK/Runtime | 10.0.302 / 10.0.10 | 10.0.302 / 10.0.10 |

Candidateはclean restore、Release build、Framework-dependent publish、win-x64 publish、実行時SQLiteバージョン、FTS5、unicode61、trigram、native DLLロードを確認した。BaselineはNU1903を再現し、Candidateの脆弱性監査は該当なしだった。P0-014相当の簡易FTS回帰は両構成で合格した。

## 採用候補と制約

`Microsoft.Data.Sqlite.Core 10.0.10`と`SQLitePCLRaw.bundle_e_sqlite3 3.0.3`の明示参照を第一候補として保持する。`win-x64` publishではx64 native DLLだけを配置できた。Framework-dependent publishは複数RIDのruntime資産を含むため、製品配布ではwin-x64 publishを優先候補とする。

正式採用前にWindows Service、コンソール、WPF相当Hostの共通ライブラリ境界、実機配置、CIライセンス・SBOM保存、将来のservicing更新を確認する。製品コードへの採用はP0-023で判断し、P0-026では製品コードを変更しない。
