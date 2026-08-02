# P1-001 製品ツールチェーン・中央ビルド基準

## 固定値

| 項目 | 基準 |
|---|---|
| .NET SDK | 10.0.302（ルート`global.json`、`rollForward: disable`） |
| .NET target framework | `net10.0`（製品プロジェクトは中央設定を継承） |
| Node.js | 22.18.0（ルート`.node-version`） |
| npm | 10.9.3以上の固定版方針はWeb製品チケットでlockfileとともに確認 |
| Build番号 | `Directory.Build.props`の`BuildNumber`。既定値1、成果物生成時に単調増加させる |
| Version | `ProductVersion`、`AssemblyVersion`、`FileVersion`、`InformationalVersion`を同ファイルから生成 |
| NuGet | `Directory.Packages.props`による中央管理。VersionOverride禁止、浮動版禁止 |
| npm | `package-lock.json`を正本とし、`npm ci`を使用。製品Web基盤はP1-013で導入 |
| 警告 | Nullable、ImplicitUsings、分析器、Code Styleを有効化。警告はエラー扱い |
| 出力 | `artifacts/bin`、`artifacts/obj`。いずれもGit追跡対象外 |

## 運用規則

- 製品プロジェクトはルートの中央設定を上書きしない。
- Build番号をチケット番号やAssemblyVersionの代用にしない。
- NuGetの依存バージョンを各`.csproj`へ記述しない。新規パッケージ導入チケットで中央ファイルへ固定値を追加する。
- Web依存は`latest`や範囲指定を使わず、lockfileをコミットする。PoCの`package.json`やlockfileは製品基盤へコピーしない。
- 生成物、実行時データ、秘密情報、証明書は`.gitignore`で除外する。除外確認だけでは安全性を保証しないため、コミット前に内容を確認する。

## P1-001の確認結果

- `dotnet --version`: `10.0.302`
- `node --version`: `v22.18.0`
- `npm --version`: `10.9.3`
- 製品ソリューション・プロジェクトはP1-002の対象のため未作成。
- したがってP1-001では製品コードのDebug/Releaseビルドは実行せず、中央設定のXML、固定値、除外規則、SDK/Node実測値を確認した。

## 次チケットへの引継ぎ

P1-002ではこの中央設定を参照する製品ソリューションとCore/Application/Host/Windows/WPFの空プロジェクトを作成する。PoCプロジェクトは参照しない。
