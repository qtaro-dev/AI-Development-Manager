# P0-017 Cookie・APIトークン認証契約

版: 1.0-p0-017
状態: PoC完了、製品採用は後続設計ゲートで判断
基準日: 2026-08-02

## 方針

- ブラウザとWebView2の人向け認証は同じセッションCookieフローを使用する。
- Cookieは`Secure`、`HttpOnly`、`SameSite=Strict`、`Path=/`とする。
- 書込み要求はセッションとCSRFトークンの一致を必須とする。
- AIクライアントは人のセッションCookieと分離したBearer APIトークンを使用する。
- APIトークンはハッシュだけを保存し、発行時の本文を再表示しない。
- 標準AIトークンは対象プロジェクトの読み取り専用とする。
- 認証されていない要求は401、認証済みだが権限不足の要求は403とする。
- パスワード失敗3回で1分間ロックし、ログアウト・失効・期限切れを即時反映する。
- 監査イベントへパスワード、Cookie本文、APIトークン本文を記録しない。

## PoC結果

- SDK: .NET 10.0.302（`global.json`固定、`dotnet --version`実測一致）
- Runtime: .NET 10.0.10
- 実行ID: `20260802T111723Z-45a5a67635144cfb8ebc9f9cfa7ce080`
- 18項目すべてPASS
- 結果: `%TEMP%\AI-Development-Manager\poc\P0-017\<run-id>\result.json`

## 対象外・保留

Active Directory、Windows統合認証、OAuth外部プロバイダー、文書単位ACL、ユーザー管理画面は対象外とする。WebView2の実ランタイム、HTTPS上の実ブラウザ接続、永続ストレージ・鍵管理、ログイン試行制限の製品運用値は製品採用前に追加確認する。
