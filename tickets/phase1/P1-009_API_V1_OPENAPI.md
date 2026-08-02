# P1-009 API v1・OpenAPI基盤

## 目的

`/api/v1`とOpenAPI 3.xをAPI契約の正本として運用する基盤を作る。

## 背景

WPFとブラウザ、将来のAIクライアントは同じAPIを利用する。業務API追加前に、版、命名、JSON、ページング予約、契約差分の検査方法を固定する。

## 前提・依存関係

- P1-006完了
- `design/01_INTEGRATED_BASIC_DESIGN.md`第7節

## 対象範囲

- `/api/v1`ルーティング
- JSONシリアライズ共通設定
- OpenAPI生成と静的成果物
- API契約差分検査
- 基盤確認用の最小エンドポイント

## 対象外

- 認証・権限
- プロジェクト・文書等の業務API
- 破壊的変更の自動移行

## 対象ファイルまたは対象モジュール

- `src/Adm.Server.Host/Api`
- `src/Adm.Application/Contracts`
- `tests/Adm.Server.IntegrationTests`
- OpenAPI成果物

## 具体的な実装内容

1. `/api/v1`のRoute groupと命名規則を作る。
2. UTC時刻、enum、null、ULID予約を含むJSON規則を定義する。
3. OpenAPIをビルド時または検証コマンドで生成する。
4. 未承認の破壊的契約差分を検出する基盤を作る。
5. 業務データを持たないversion/info契約で疎通確認する。

## テスト内容

- `/api/v1`以外へ業務APIを登録できない規則
- JSON規則の契約テスト
- OpenAPI生成と構文検証
- 破壊的差分検出の模擬テスト
- Web UIルートとの競合防止

## 完了条件

- OpenAPI成果物を一意に生成できる。
- Server実装と契約の差異を自動検出できる。
- API版とWeb UIルートが分離されている。
- Phase 1に業務APIが混入していない。

## ユーザーが目視確認する内容

- OpenAPIの基盤エンドポイント
- APIベースパスと版管理規則
- 契約差分検出例

## 想定されるリスク

- OpenAPI生成物と実行時挙動がずれる
- UI用fallbackがAPI 404を奪う
- 内部型をそのまま公開契約にする

## 完了後に更新すべき設計資料

- API基本契約
- `design/01_INTEGRATED_BASIC_DESIGN.md`第7節
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

## 実装結果

- `/api/v1`のRoute groupと`GET /api/v1/version`を追加した。
- camelCase、UTC時刻、enum文字列、null省略のJSON設定をHostへ追加した。
- `Microsoft.AspNetCore.OpenApi`で`/openapi/v1.json`を生成し、静的正本を`design/openapi/adm-v1.openapi.json`へ保存した。
- `scripts/api/Validate-OpenApiContract.ps1`でOpenAPI 3.x、必須操作、path／operation削除を検査し、CI品質ゲートへ組み込んだ。
- rootのHost確認用ルートはOpenAPIから除外し、未定義のAPIルートは404となることを確認した。
- 認証、Problem Details、業務APIは実装していない。

## 検証結果

使用SDK: `10.0.302`（`global.json`固定値と`dotnet --version`実測値が一致）。

実行したコマンド:

```powershell
dotnet restore .\AIDevelopmentManager.sln
dotnet build .\AIDevelopmentManager.sln --configuration Debug
dotnet test .\tests\Adm.Server.IntegrationTests\Adm.Server.IntegrationTests.csproj --configuration Debug --no-build --no-restore
pwsh -NoProfile -File .\scripts\api\Validate-OpenApiContract.ps1
```

確認項目:

- `/api/v1/version`の版、UTC、enum、null省略
- `/openapi/v1.json`のOpenAPI 3.x構文と基盤endpoint
- `/`とのルート分離、未定義APIの404
- 実装と静的契約の必須path／operation差分検査

P1-009は完了状態とし、P1-010以降は対象外とする。
