# P1-009 API v1・OpenAPI契約

## 1. 公開境界

Phase 1のAPIは`/api/v1`を基底パスとする。P1-009で公開する業務データを持たない確認用操作は次の1件だけである。

| Method | Path | Purpose |
|---|---|---|
| GET | `/api/v1/version` | API版、契約版、UTC時刻、準備状態を返す |

Web UI用のルートやホスト確認用の`/`はAPI契約へ含めない。未定義の`/api/v1/*`は404とし、API版とWebルートのfallbackを混在させない。

## 2. JSON規則

- プロパティ名はcamelCase。
- 時刻はUTCのISO 8601 `date-time`。
- enumはcamelCase文字列。
- null値は応答JSONから省略する。
- 将来のULIDはJSON文字列として扱う。

`ApiVersionResponse`の`resourceId`は将来契約の予約項目であり、P1-009では値を持たないため応答から省略する。

## 3. OpenAPI正本と差分検査

実行時ドキュメントはASP.NET Core OpenAPIで生成し、`/openapi/v1.json`から取得する。リポジトリへ保存する静的正本は`design/openapi/adm-v1.openapi.json`である。ビルド・CIでは次を実行する。

```powershell
pwsh -NoProfile -File .\scripts\api\Validate-OpenApiContract.ps1
```

検査はOpenAPI 3.x、`info.version`、`GET /api/v1/version`、構文を確認し、基準ドキュメントに存在するpath／operationの削除を破壊的差分として失敗させる。追加操作はP1-009の範囲外であり、業務APIを追加しない。

## 4. 対象外

認証・認可、Problem Details、プロジェクト・文書・添付等の業務API、破壊的変更の自動移行は後続チケットへ分離する。P1-008の追跡IDと秘密情報非記録は共通Hostミドルウェアとして維持する。
