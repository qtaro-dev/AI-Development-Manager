# P0-016 LAN HTTPS初期設定契約

版: 1.0-p0-016
状態: PoC完了、P0-023で最終設計確認
基準日: 2026-08-02

## 初期設定フロー

1. Serverはlocalhost限定で起動する。
2. 管理者がServer名とLANアドレスを選択する。
3. 5年有効のローカル認証局と、397日有効のServer証明書を生成する。
4. Server証明書には`localhost`、Server名、`127.0.0.1`、LANアドレスをSANとして設定する。
5. クライアントへは秘密鍵を含まないCA公開証明書だけを渡す。
6. クライアント側の信頼設定とHTTPS接続を確認した後、LAN待受とFirewall変更を確認付きで有効化する。

## PoC結果

- SDK: .NET 10.0.302（`global.json`固定、`dotnet --version`実測一致）
- Runtime: .NET 10.0.10
- 実行ID: `20260802T110312Z-62858e82648a4af4a2672581c638804a`
- CA 5年、Server証明書397日、SAN、serverAuth EKU、カスタムルートチェーン検証に合格
- 期限切れ証明書を拒否し、LANアドレス変更時に旧証明書を継続利用しないことを確認
- クライアント信頼用証明書に秘密鍵が含まれないことを確認
- Firewall規則変更、証明書ストア登録、UAC昇格は実行せず、確認付き手順として分離
- 失敗時はLAN待受を停止し、Firewall規則を限定削除してlocalhost限定へ戻すロールバック手順を出力

証明書・秘密鍵・実行時設定は`%TEMP%\AI-Development-Manager\poc\P0-016\<run-id>`へ保存し、リポジトリには含めない。別Windows 11 PCからの実LAN接続、実Firewall変更、証明書ストア権限差は製品採用前のWindows実機確認事項とする。
