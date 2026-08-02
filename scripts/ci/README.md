# CI quality gates

`Invoke-QualityGates.ps1` is the local and GitHub Actions entry point for P1-005. It runs fixed-SDK restore, Debug/Release build, tests, P1-003 Architecture checks, NuGet/npm vulnerability checks, license evidence, CycloneDX SBOM generation, and tracked-file inspection.

```powershell
pwsh -NoProfile -File .\scripts\ci\Invoke-QualityGates.ps1
```

All command logs, TRX test results, dependency evidence, and failure diagnostics are written below `artifacts/ci-evidence`. The GitHub Actions workflow uploads this directory with `if: always()`, including when a gate fails. `src/Adm.Web` is explicitly recorded as not yet present until P1-013; once its `package.json` and lockfile exist, npm gates run automatically.
