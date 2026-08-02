# P1-011 ヘルス・Ready契約

## 1. エンドポイント

| Method | Path | 成功 | 失敗 |
|---|---|---:|---:|
| GET | `/health/live` | 200 | 停止処理中は503 |
| GET | `/health/ready` | 200 | 準備未完了・Contributor失敗・停止処理中は503 |

どちらも認証前のプロセス確認用エンドポイントとし、認証情報、ローカルパス、接続文字列、内部例外、走査件数などの業務情報を返さない。

## 2. 応答

共通項目は`status`、`buildVersion`、`startupMode`、`serverTimeUtc`、`failedContributors`とする。時刻はUTCのISO 8601、配列は失敗がなければ空配列とする。Contributor失敗は名前と安定した失敗コードだけを返し、例外本文は返さない。

`live`の`healthy`はプロセスが要求へ応答可能であることを示す。`ready`の`ready`はApplicationStarted後で、登録済みContributorが全て成功したことを示す。停止処理中は`stopping`または`not_ready`として503を返す。

## 3. Contributor拡張点

後続機能は`Adm.Application.Health.IHealthContributor`を実装してDIへ登録する。Contributorの失敗はreadinessだけを失敗させ、livenessへ伝播させない。Contributorの実行例外はサーバーログへ型情報を含む診断イベントとして記録するが、HTTP応答へは固定コード`dependency_unavailable`だけを返す。

走査、索引、外部監視、認証済み管理診断は後続チケットで追加する。
