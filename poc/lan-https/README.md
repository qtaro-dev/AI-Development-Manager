# P0-016 LAN HTTPS初期設定PoC

localhost限定起動から、ローカル認証局、SAN付きServer証明書、クライアント信頼用公開証明書、更新・失敗復帰・Firewall/UAC境界を検証する。証明書と秘密鍵は実行時に `%TEMP%\AI-Development-Manager\poc\P0-016\<run-id>` へ生成し、リポジトリへ保存しない。Firewall規則の変更や証明書ストアへの自動登録は行わず、確認付き手順として出力する。

```powershell
dotnet build .\poc\lan-https\LanHttps.Poc.sln --configuration Release
dotnet .\poc\lan-https\src\LanHttps.Poc\bin\Release\net10.0\LanHttps.Poc.dll
```
