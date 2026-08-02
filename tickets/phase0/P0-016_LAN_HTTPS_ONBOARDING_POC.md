# P0-016 LAN HTTPS初期設定PoC

## 目的

専門知識がない利用者でも、localhost初回起動からLAN内HTTPS接続まで安全に設定できる手順を確定する。

## 前提・依存関係

- P0-002完了
- P0-003完了

## 対象範囲

- LANアドレス・Server名選択
- ローカル認証局とServer証明書
- クライアント信頼用証明書
- 証明書有効期間と更新
- Firewall規則案内
- 失敗時のlocalhost復帰

## 対象外

- 公開認証局
- インターネット公開
- 自動UPnP
- 企業AD証明書の正式連携

## 対象ファイルまたは対象モジュール

- `poc/lan-https`
- 設計候補: `Adm.Infrastructure.Windows.Certificates`、`Firewall`
- 初期設定手順案

## 具体的な実装内容

1. localhost限定起動から設定する最小ウィザードまたは手順PoCを作る。
2. 認証局5年、Server証明書397日の生成・更新を検証する。
3. 証明書SANへServer名／アドレスを設定する。
4. 信頼設定、Firewall変更、UACが必要な箇所を分離する。
5. 設定失敗時に元へ戻す手順を確認する。

## テスト内容

- Server PCと別のWindows 11 PCからのHTTPS接続
- 未信頼、期限切れ、名前不一致
- アドレス変更と証明書再発行
- Firewall閉鎖／開放
- localhostへの復帰

## 受け入れ条件

- ブラウザ警告なしでLAN接続できる再現可能な手順がある。
- 秘密鍵をクライアントへ配布しない。
- 変更対象と戻し方をユーザーへ示せる。

## ユーザーが目視確認する内容

- 初期設定の画面または手順モック。
- 各PCで必要な操作と安全上の説明。
- 接続成功URLと失敗時案内。

## 想定されるリスク

- 証明書信頼登録への管理者権限。
- DHCPによるアドレス変更。
- Server名解決がLAN環境ごとに異なる。

## 完了後に更新すべき設計資料

- LAN構成設計
- HTTPS初期設定手順
- ADR-007
- 配布設計

## 実装・実測結果（2026-08-02）

### 実装

- `poc/lan-https`に.NET 10コンソールPoCを追加した。
- ローカル認証局、Server証明書、SAN、serverAuth EKU、カスタムルート検証、期限切れ拒否、アドレス変更時の再発行を検証する。
- クライアント用CA公開証明書を生成し、秘密鍵を含まないことを確認する。
- Firewall変更、証明書ストア登録、UAC昇格は自動実行せず、setup-planとrollback-planへ分離した。

### 再現コマンド

```powershell
dotnet build .\poc\lan-https\LanHttps.Poc.sln --configuration Release
dotnet .\poc\lan-https\src\LanHttps.Poc\bin\Release\net10.0\LanHttps.Poc.dll
```

### 実測結果

実行ID: `20260802T110312Z-62858e82648a4af4a2672581c638804a`

- CA有効期間5年: 合格
- Server証明書有効期間397日: 合格
- Server名・LANアドレスを含むSAN: 合格
- serverAuth EKU: 合格
- クライアント信頼用証明書の秘密鍵非含有: 合格
- カスタムルートチェーン検証: 合格
- 期限切れ証明書拒否: 合格
- アドレス変更時の再発行要求: 合格
- Firewall/UAC分離: 合格
- localhost復帰ロールバック手順: 合格

SDKは10.0.302、Runtimeは10.0.10である。証明書・秘密鍵・実行時設定は一時領域へ保存し、コミットしていない。

### 制約・レビュー待ち事項

- 別Windows 11 PCからの実LAN HTTPS接続を確認すること。
- 実Firewall規則変更、証明書ストア登録、標準ユーザー／管理者権限差を実機で確認すること。
- DHCP等によるアドレス変更時の再発行・再配布手順を製品採用前に確定すること。
- 公開認証局、UPnP、企業AD証明書連携は対象外である。
