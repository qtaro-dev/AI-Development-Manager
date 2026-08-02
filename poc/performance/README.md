# P0-015 性能測定PoC

P0-003の負荷プロファイル（10,000文書、5人の利用者、2 AIクライアント）を固定シード `15015` で再現する。測定対象は初回走査、候補一覧、P0-014 SQLite FTS5検索、並行更新とETag競合、CPU・メモリ・ディスクである。

```powershell
dotnet build .\poc\performance\Performance.Poc.sln --configuration Release
dotnet .\poc\performance\src\Performance.Poc\bin\Release\net10.0\Performance.Poc.dll
```

出力は `%TEMP%\AI-Development-Manager\poc\P0-015\<run-id>\` に保存し、`result.json` と `run.log` を監査・再現用とする。生成コーパスとSQLiteデータベースはリポジトリへコミットしない。
