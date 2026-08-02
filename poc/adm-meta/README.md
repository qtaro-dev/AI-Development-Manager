# P0-007 `.adm-meta`・ULID・連番仕様PoC

既存Markdownを変更せず、プロジェクト管理メタデータと利用者ごとの確認状態をサイドカーへ保存する検証コードです。製品コードではありません。

## 構成

- `.adm-meta/project.json`: スキーマ、プロジェクトID、種別別連番の正本
- `.adm-meta/documents/<ULID>.json`: 文書ID、相対パス、内容SHA-256、連番
- `.adm-meta/users/<user>/documents/<ULID>.json`: 利用者固有の確認状態・手動分類
- `.adm-meta/documents.lock`: 連番採番の排他ロック

文書メタデータと利用者状態は別ファイルです。手動分類は元Markdownを書き換えず、利用者状態の`classification_override`として保存します。

## 実行

```powershell
dotnet restore .\poc\adm-meta\AdmMeta.sln
dotnet build .\poc\adm-meta\AdmMeta.sln --configuration Release
dotnet .\poc\adm-meta\src\AdmMeta.Poc\bin\Release\net10.0\AdmMeta.Poc.dll
```

SDKはリポジトリ直下の`global.json`で固定します。実行時に一時ディレクトリへ検証データを作成し、入力Markdown、サイドカー、採番結果、改名候補、利用者分離、原子的保存を検証します。
