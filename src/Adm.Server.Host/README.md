# Adm.Server.Host

ASP.NET Core Serverの共通Host配置です。`ServerHostFactory`がコンソール、手動、Windows Serviceなどの起動方式から再利用するHost生成境界を提供します。

P1-006では開発用HTTPをlocalhost（127.0.0.1）だけで待ち受け、ルートの基盤確認応答だけを提供します。LAN待受、HTTPS、認証、設定、ログ、業務API、Windows Service登録は後続チケットの対象です。

参照規則: HostからApplication、Windows Adapterを利用する設計余地を持ちますが、P1-002では参照を追加しません。`poc/`は参照しません。
