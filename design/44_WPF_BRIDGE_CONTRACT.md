# WPF Bridge契約（P1-021）

## 目的

WebView2上の製品Web UIとWPFホストの連携を、バージョン付きEnvelopeと明示的な許可リストで制限する。P1-021では業務機能を実装せず、接続確認用の`getHostInfo`だけを公開する。

## Envelope

要求は`version`、`messageType`、`operation`、`requestId`、`payload`を必須とする。現在の`version`は`1`、`messageType`は`request`または`cancel`、`operation`は`getHostInfo`のみ、payloadは空のJSON objectのみを受け付ける。未知フィールド、未知操作、長すぎる要求ID、不正なJSONは拒否する。

応答は`messageType=response`、要求ID、操作、`status=ok|error|cancelled`を返す。エラーは固定のコード、利用者向けメッセージ、要求IDをtraceIdとして返し、例外本文や秘密情報を返さない。

## セキュリティ境界

WPFはWebView2のトップレベル文書からのWebMessageだけを扱い、メッセージのSourceを設定済みServer origin（scheme、host、port一致）と照合する。Navigation拒否と同じloopback境界を使用する。通常のブラウザではWebView2 Bridgeを利用できない。

許可リストは`getHostInfo`だけであり、任意コード実行、任意コマンド実行、PowerShell、自由なパスの読書き、Markdown・添付・状態・テスト結果の業務操作は公開しない。cancelは契約上の応答形式を持つが、P1-021の即時処理に長時間処理は存在しない。

## 将来拡張

ファイル／フォルダ選択、Explorer起動、通知、Server制御などは、操作ごとに入力形式、権限、origin、監査、キャンセル、失敗時の復元案内を別途設計・レビューしてから追加する。

## P1-028 Local Application Channelとの分離

ADR-019のLocal Application Channelは業務操作のRequest、Response、Errorを扱う別契約であり、WPF Bridgeへ追加しない。Platform Bridgeは引き続きWindows固有の限定操作だけを扱い、任意コード、任意コマンド、自由なファイルアクセス、業務データ操作を公開しない。React UIはDataAccess Portを介してLocal Application ChannelまたはServer modeのHTTP API Adapterを選択する。

## P1-033 Local Channel v1との実装分離

Local Application Channelは`Adm.Wpf.LocalChannel`と`src/Adm.Web/src/data-access/local/`に閉じた別契約であり、Platform Bridgeの型・Event Handler・operation registryを共有しない。Local Channelの製品registryはP1-033では空で、テスト専用operationだけが別テストregistryへ登録される。従って`getHostInfo`はLocal Channelから実行できない。

## P1-038 終了・fallback境界

P1-038の終了、再試行、設定確認、Local継続はWPFのネイティブfallbackおよびWeb UIの表示導線で提供する。Platform Bridgeへ終了操作や任意のホスト操作は追加せず、`getHostInfo`のみの許可リストを維持する。
