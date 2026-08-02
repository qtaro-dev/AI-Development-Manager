# P0-026 SQLite依存更新・安全性PoC

現行構成と候補構成を別プロジェクトで比較する。製品コードは変更しない。

| 構成 | 依存 |
|---|---|
| baseline | `Microsoft.Data.Sqlite 10.0.10`（推移依存を確認） |
| candidate | `Microsoft.Data.Sqlite.Core 10.0.10` + `SQLitePCLRaw.bundle_e_sqlite3 3.0.3` |

```powershell
dotnet restore .\poc\sqlite-dependency-security\SqliteDependencySecurity.sln
dotnet build .\poc\sqlite-dependency-security\SqliteDependencySecurity.sln --configuration Release --no-restore
dotnet .\poc\sqlite-dependency-security\src\Baseline.Poc\bin\Release\net10.0\Baseline.Poc.dll
dotnet .\poc\sqlite-dependency-security\src\Candidate.Poc\bin\Release\net10.0\Candidate.Poc.dll
dotnet list .\poc\sqlite-dependency-security\src\Baseline.Poc\Baseline.Poc.csproj package --include-transitive
dotnet list .\poc\sqlite-dependency-security\src\Baseline.Poc\Baseline.Poc.csproj package --vulnerable --include-transitive
dotnet list .\poc\sqlite-dependency-security\src\Candidate.Poc\Candidate.Poc.csproj package --include-transitive
dotnet list .\poc\sqlite-dependency-security\src\Candidate.Poc\Candidate.Poc.csproj package --vulnerable --include-transitive
dotnet publish .\poc\sqlite-dependency-security\src\Candidate.Poc\Candidate.Poc.csproj -c Release --self-contained false -r win-x64 -o <temp-publish>
```

PoC結果は`%TEMP%\AI-Development-Manager\poc\P0-026\<run-id>\result.json`へ保存する。publish成果物とSBOM相当の依存一覧は一時領域へ保存し、`bin/`、`obj/`、DB、native DLLはコミットしない。
