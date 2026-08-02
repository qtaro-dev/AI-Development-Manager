# P1-006 ASP.NET CoreコンソールHost

## 目的

全起動方式が共有するASP.NET Core Hostを、開発・デバッグ用コンソールとしてlocalhost限定で起動できる製品基盤を作る。

## 背景

ADR-001/012で独立Serverと同一Host共有を採用した。Phase 1では認証・HTTPS前のため、LANへ公開せず安全なローカルHostから開始する。

## 前提・依存関係

- P1-002完了
- P1-004完了
- P0-002結果、ADR-001、ADR-012

## 対象範囲

- ASP.NET Core 10 Host生成
- Kestrelのlocalhost限定HTTP開発エンドポイント
- 開始、正常停止、異常終了、二重起動
- Web/APIを追加できるDIとMiddleware境界

## 対象外

- LAN待受、HTTPS、認証
- Windows Service登録
- 業務API、SQLite、Markdown

## 対象ファイルまたは対象モジュール

- `src/Adm.Server.Host`
- `tests/Adm.Server.IntegrationTests`

## 具体的な実装内容

1. 再利用可能なHost factoryを実装する。
2. 開発コンソール起動とCtrl+C正常停止を実装する。
3. 既定待受を`127.0.0.1`/localhostだけに限定する。
4. 二重起動とポート競合を利用者向けエラーへ変換できる境界を作る。
5. 空のルートまたは基盤確認用応答だけを提供する。

## テスト内容

- console起動・正常停止
- localhost接続成功
- LANアドレス・`0.0.0.0`へ待受しないこと
- 二重起動時の明示失敗
- TestServerと実Kestrelの基本スモーク

## 完了条件

- 同一Host factoryから繰り返し起動できる。
- 認証前のServerがLANへ公開されない。
- 正常停止で処理を取りこぼさず終了できる基盤がある。
- 業務機能やWindows固有処理がHostへ混在していない。

## ユーザーが目視確認する内容

- 起動・停止ログ
- localhost接続結果
- LANから接続できない確認
- 二重起動時の案内

## 想定されるリスク

- 開発設定が正式設定へ流用されLAN公開される
- 起動時例外が内部情報を露出する
- Host factoryが起動モードごとに分岐する

## 完了後に更新すべき設計資料

- `design/01_INTEGRATED_BASIC_DESIGN.md`
- ADR-001、ADR-012
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

## 実装結果

- `Adm.Server.Host`を実行可能なASP.NET CoreコンソールHostへ変更した。
- `ServerHostFactory`を追加し、コンソール等の起動方式が同じHost生成処理を再利用できる境界を作った。
- KestrelはIPv4 loopback（`127.0.0.1`）だけを待ち受ける。ポート0指定時は空きポートを使用できる。
- ルートに基盤確認用の最小応答だけを追加した。LAN待受、HTTPS、認証、業務API、Windows Service登録は実装していない。
- ポート競合時はコンソールへ利用者向けメッセージを出して異常終了する。

## 検証結果

使用SDK: `10.0.302`（`global.json`固定値と`dotnet --version`実測値が一致）。

実行したテスト:

```powershell
dotnet build .\AIDevelopmentManager.sln --configuration Debug
dotnet build .\AIDevelopmentManager.sln --configuration Release --no-restore
dotnet test .\tests\Adm.Server.IntegrationTests\Adm.Server.IntegrationTests.csproj --configuration Debug --no-build --no-restore
```

確認項目:

- TestServerが実ポートを占有せず起動・停止する
- 実Kestrelがlocalhostへ接続できる
- `0.0.0.0`を待受しない
- 正常停止できる
- 同一ポートの二重起動が`IOException`で明示的に失敗する

P1-006は完了状態とし、P1-007以降は対象外とする。
