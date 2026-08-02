# P1-001 ツールチェーン・中央ビルド基準

## 目的

Phase 1以降の全製品コードが同じSDK、警告規則、バージョン、依存管理方法で再現ビルドできる基準を作る。

## 背景

Phase 0で.NET SDK 10.0.302と.NET 10を製品基準に確定した。PoCごとの設定を製品へ持ち込まず、製品ソリューション作成前に単一のビルド生成元を確立する必要がある。

## 前提・依存関係

- P0-023完了
- `global.json`、`design/06_REPOSITORY_RULES.md`

## 対象範囲

- .NET SDK、Target Framework、言語・Nullable・警告方針
- Assembly/File/Informational VersionとBuild番号の中央管理
- NuGet中央パッケージ管理の基準
- Node.js/npmの対応版記録とlockfile必須化
- Release/Debugの共通出力・生成物除外

## 対象外

- 製品プロジェクト作成
- CIサービス固有設定
- SDKや採用技術の再選定

## 対象ファイルまたは対象モジュール

- `global.json`
- `Directory.Build.props`
- `Directory.Packages.props`
- `.editorconfig`
- `.gitignore`
- Node版管理ファイルまたは同等の基準資料

## 具体的な実装内容

1. .NET 10とSDK 10.0.302を中央設定へ反映する。
2. Nullable、ImplicitUsings、解析レベル、警告扱いを統一する。
3. Build番号と各Version属性の単一生成元を作る。
4. NuGet依存を中央管理し、浮動版を禁止する。
5. Web依存はlockfileを正本とし、`latest`を製品依存へ残さない規則を定義する。
6. `bin`、`obj`、`node_modules`、秘密情報、実行時データを除外する。

## テスト内容

- `dotnet --version`と`global.json`の一致
- Debug/Releaseの最小ビルド検証
- 不正な浮動パッケージ版と警告違反の検出
- Build番号の重複・巻戻し検査
- 生成物と秘密情報のGit追跡除外確認

## 完了条件

- 固定SDKがない環境ではビルドが暗黙に別SDKへ進まない。
- .NETとNode依存に固定方法があり、クリーン環境で再現できる。
- バージョンとBuild番号の生成元が一つである。
- 共通警告・解析設定が後続全プロジェクトへ自動適用される。
- 規約と実ファイルの内容が一致する。

## ユーザーが目視確認する内容

- 使用SDK、Node、依存固定方式の一覧
- VersionとBuild番号の生成元
- 追跡対象外一覧

## 想定されるリスク

- 厳格な警告設定で外部生成コードまで失敗する
- SDK servicing更新時に文書と設定がずれる
- npm lockfileとpackage.jsonの不一致

## 完了後に更新すべき設計資料

- `design/06_REPOSITORY_RULES.md`
- `design/02_TECHNOLOGY_AND_ADR.md`
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

## 実施結果（2026-08-03）

以下を追加・確定した。

- `Directory.Build.props`: Build番号、Version、`net10.0`、Nullable、ImplicitUsings、分析器、警告エラー化、決定的ビルド、共通出力先。
- `Directory.Packages.props`: NuGet中央管理、推移依存固定、VersionOverride禁止。
- `.editorconfig`: C#、JSON、YAML、Markdownの共通形式と警告レベル。
- `.node-version`: Node.js 22.18.0。
- `.gitignore`: `bin`、`obj`、`artifacts`、`node_modules`、キャッシュ、実行時データ、秘密情報、証明書、生成物の除外。
- `design/31_P1_TOOLCHAIN_BUILD_BASELINE.md`: 固定値、運用規則、実測値、P1-002への引継ぎ。

実測値は`.NET SDK 10.0.302`、`Node.js v22.18.0`、`npm 10.9.3`である。P1-002が製品ソリューション作成を担当するため、P1-001では製品プロジェクトを作成せず、Debug/Releaseの製品ビルドは実行対象外とした。PoCコード・PoCのlockfileは参照・コピーしていない。

## 受入確認

- 固定SDK: `dotnet --version`と`global.json`が一致。
- Node基準: `.node-version`と`node --version`が一致。
- 中央設定: Version、Build番号、警告、NuGet管理の生成元を各1箇所に限定。
- 追跡除外: `.gitignore`に規約上の生成物、秘密情報、実行時データを反映。
- 製品コードとPoCの分離: 製品`src/`、`tests/`、ソリューションは未作成であり、P1-002以降へ作業を拡張していない。
