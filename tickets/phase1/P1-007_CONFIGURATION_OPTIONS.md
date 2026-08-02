# P1-007 構成・Options検証基盤

## 目的

Server設定の生成元、優先順位、型、検証、再起動要否、秘密情報境界を統一する。

## 背景

Phase 2では証明書、認証、プロジェクト登録など安全性に関わる設定を追加する。機能ごとに独自読込を作る前に共通Options境界が必要である。

## 前提・依存関係

- P1-006完了
- `design/01_INTEGRATED_BASIC_DESIGN.md`第4.2節

## 対象範囲

- JSON、環境変数、コマンドラインの優先順位
- 型付きOptionsと起動時検証
- 通常設定と秘密値参照の分離
- 既定値、管理変更可否、再起動要否のメタデータ

## 対象外

- ユーザー・トークン・証明書秘密鍵の実保存
- 管理画面
- LAN設定

## 対象ファイルまたは対象モジュール

- `src/Adm.Server.Host/Configuration`
- `src/Adm.Application/Configuration`
- `tests/Adm.Server.IntegrationTests`

## 具体的な実装内容

1. 設定ソースの優先順位を固定する。
2. 型付きOptionsと起動時ValidateOnStartを導入する。
3. 秘密値を通常設定JSONへ直接保存しない契約を作る。
4. 設定キー、既定値、範囲、再起動要否の一覧を生成可能にする。
5. 不正値を利用者向け案内と内部コードへ分離する。

## テスト内容

- 設定ソース優先順位
- 欠落・範囲外・不正形式での起動拒否
- 既定値適用
- 秘密値のログ・エラー非露出
- 設定一覧と実Optionsの一致

## 完了条件

- 不正な安全関連設定でServerが曖昧に起動しない。
- 設定の生成元と最終値を秘密を除いて説明できる。
- 後続機能が独自の設定読込処理を作らずに済む。
- Phase 2の秘密保存実装を差し込める抽象境界がある。

## ユーザーが目視確認する内容

- 設定一覧、既定値、変更可否、再起動要否
- 不正設定時の分かりやすい案内

## 想定されるリスク

- 設定ダンプから秘密値が漏れる
- 開発用既定値が正式運用で有効になる
- 動的変更可能な設定と再起動必須設定が混在する

## 完了後に更新すべき設計資料

- `design/01_INTEGRATED_BASIC_DESIGN.md`第4.2節
- 設定仕様
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

## 実装結果

- `src/Adm.Server.Host/Configuration`へ`ServerOptions`、`SecretReferenceOptions`、`ConfigurationCatalog`、`ServerConfiguration`を追加した。
- `WebApplication.CreateBuilder`のJSON、環境変数、コマンドライン構成を型付きOptionsへバインドし、後勝ちの優先順位を維持した。
- `ValidateOnStart`で`Server:BindAddress`を`127.0.0.1`／`localhost`に限定し、`Server:Port`を0～65535へ限定した。
- `Secrets`ではAPIトークンや証明書等の実値を直接受け付けず、参照名の境界だけを定義した。検証エラーは秘密値を含めない。
- 設定カタログへキー、秘密を含まない既定値、変更可否、再起動要否、秘密参照区分を登録した。
- P1-006の`ServerHostFactory`、127.0.0.1限定待受、実Kestrel起動・停止・ポート競合、TestServer構成は維持した。

## 検証結果

使用SDK: `10.0.302`（`global.json`固定値と`dotnet --version`実測値が一致）。

実行したコマンド:

```powershell
dotnet build .\AIDevelopmentManager.sln --configuration Debug
dotnet build .\AIDevelopmentManager.sln --configuration Release --no-restore
dotnet test .\tests\Adm.Server.IntegrationTests\Adm.Server.IntegrationTests.csproj --configuration Debug --no-build --no-restore
dotnet test .\tests\Adm.Server.IntegrationTests\Adm.Server.IntegrationTests.csproj --configuration Release --no-build --no-restore
```

結果: Debug／Release build成功（警告0、エラー0）、Server統合テストは各7件成功。設定ソース優先順位、既定値、loopback以外の起動拒否、秘密値の直接指定拒否、秘密非露出、設定カタログを確認した。

P1-007は完了状態とし、P1-008以降は対象外とする。
