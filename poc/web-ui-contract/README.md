# P0-018 Web UI mock API contract

P0-018のReact UIは製品APIへ接続せず、`poc/web-ui-react/src/mockApi.ts`の同一モック契約を利用する。検索・認証・保存は実装せず、主要表示状態と操作の意味を確認する。

| 操作 | モック状態 | 製品接続時の境界 |
|---|---|---|
| チケット一覧 | 4件の文書・状態・更新日時 | GET documents |
| テスト表 | 4行、実際の結果と判定 | GET/PUT test result |
| 保存 | saved / dirty / conflict | ETag付きPUT |
| 認証 | P0-017のCookie前提 | HTTPS session cookie |
| 競合 | 最新版と自分の変更を表示 | 409 conflict response |
