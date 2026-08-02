# P0-017 Cookie・APIトークン認証PoC

製品コードとは分離した認証モデルで、Cookieログイン、CSRF、ロール・プロジェクトスコープ、読み取り専用AIトークン、失効、ロックアウト、監査ログ秘匿を検証する。ブラウザとWebView2は同じCookie属性・認証フローを使う前提をモデル化し、UIは実装しない。

```powershell
dotnet build .\poc\auth-token\AuthToken.Poc.sln --configuration Release
dotnet .\poc\auth-token\src\AuthToken.Poc\bin\Release\net10.0\AuthToken.Poc.dll
```

結果は`%TEMP%\AI-Development-Manager\poc\P0-017\<run-id>\result.json`へ保存する。パスワード、Cookie本文、APIトークン本文は結果・ログ・リポジトリへ保存しない。
