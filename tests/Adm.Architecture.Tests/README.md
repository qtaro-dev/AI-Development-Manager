# Adm.Architecture.Tests

P1-003の参照方向・Windows依存境界検査です。P1-004のxUnit/TestServer基盤とは分離し、PowerShellでProjectReference、ビルド済みAssembly参照、禁止Namespace、PoC参照を検査します。

実行方法（Debug成果物を検査）:

```powershell
pwsh -NoProfile -File .\tests\Adm.Architecture.Tests\Invoke-ArchitectureBoundaryTests.ps1 -Configuration Debug
```

実行前に、ルートの固定SDKでSolutionをビルドしてください。fixtureの意図的な違反が検出されることも同じ実行で確認します。
