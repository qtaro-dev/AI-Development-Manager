# P1-008 構造化ログ・追跡ID基盤

## 目的

Serverの起動、要求、失敗を追跡IDで関連付けられる構造化JSONログ基盤を作る。

## 背景

利用者向けエラーには追跡IDを表示し、管理者は秘密情報を含まないログから原因を調査できる必要がある。業務監査ログとは責務を分離する。

## 前提・依存関係

- P1-006完了
- P1-007完了
- ログはMicrosoft.Extensions.Loggingを採用済み

## 対象範囲

- JSONログ形式
- 要求・操作の追跡ID
- 起動モード、環境、Build情報
- ログレベル、ローテーション境界、保持設定予約
- 秘密・個人情報のマスキング規則

## 対象外

- 業務データ変更の監査ログ
- 外部ログ収集サービス
- 認証イベント

## 対象ファイルまたは対象モジュール

- `src/Adm.Server.Host/Logging`
- `src/Adm.Application/Diagnostics`
- `tests/Adm.Server.IntegrationTests`

## 具体的な実装内容

1. 構造化JSONログProviderと共通項目を定義する。
2. 入力追跡IDの検証と、未指定時の安全な生成を実装する。
3. HTTP応答とログへ同じ追跡IDを関連付ける。
4. パスワード、Cookie、Authorization、トークン本文を記録しないFilterを実装する。
5. consoleとWindows Serviceで同じログ契約を利用できるようにする。

## テスト内容

- JSONとして解析可能なログ
- 複数要求の追跡ID分離
- 応答とログの追跡ID一致
- 秘密ヘッダー・値の非記録
- 例外時の内部詳細と利用者向け情報の分離

## 完了条件

- 一つの要求を追跡IDで開始から失敗まで追える。
- パスワード、Cookie、APIトークン本文がログへ出ない。
- console/Serviceでログ項目が同じである。
- 業務監査ログと診断ログの責務が明記されている。

## ユーザーが目視確認する内容

- 正常・失敗要求のJSONログ例
- 追跡IDからログを特定する流れ
- マスキング確認結果

## 想定されるリスク

- 例外オブジェクトやScopeから秘密値が漏れる
- 高頻度ログでディスクを圧迫する
- 追跡IDを利用者入力のまま信頼する

## 完了後に更新すべき設計資料

- ログ・診断設計
- `design/01_INTEGRATED_BASIC_DESIGN.md`
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

## 実装結果

- `src/Adm.Server.Host/Logging`へ`AdmJsonLoggerProvider`、`RequestTracingMiddleware`、`TraceId`、`LogRedaction`を追加した。
- ログを1行1JSONでConsoleへ出力し、起動モード、環境名、Build情報、要求method／path／status／経過時間を構造化項目として記録する。
- `X-Request-Id`を検証し、応答ヘッダー、ログScope、要求開始・完了ログへ同一IDを設定する。不正または未指定時は安全なIDを生成する。
- Password、Cookie、Authorization、Token、Secret、PrivateKey、Bearerトークン、機密QueryStringをマスキングする。要求本文と例外本文・Stack Traceは記録しない。
- 業務監査ログ、認証イベント、外部収集、ファイル保持・ローテーションは対象外とした。

## 検証結果

使用SDK: `10.0.302`（`global.json`固定値と`dotnet --version`実測値が一致）。

実行したコマンド:

```powershell
dotnet build .\AIDevelopmentManager.sln --configuration Debug
dotnet build .\AIDevelopmentManager.sln --configuration Release --no-restore
dotnet test .\tests\Adm.Server.IntegrationTests\Adm.Server.IntegrationTests.csproj --configuration Debug --no-build --no-restore
dotnet test .\tests\Adm.Server.IntegrationTests\Adm.Server.IntegrationTests.csproj --configuration Release --no-build --no-restore
```

確認項目:

- JSONログを`JsonDocument`で解析できる
- 複数要求の追跡IDを分離できる
- 応答ヘッダーとログScopeの追跡IDが一致する
- Authorization／Bearer／QueryStringの秘密値がログへ出ない
- 例外型だけを記録し、例外本文を記録しない
- 実コンソール起動でJSONログとlocalhost応答を確認した

P1-008は完了状態とし、P1-009以降は対象外とする。
