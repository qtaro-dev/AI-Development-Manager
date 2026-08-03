# P1-034 WPF Local Composition Root

## 責務

`Adm.Wpf.Composition.LocalCompositionRoot`は、Local modeだけのComposition Rootである。WPFプロセス内に`GetFoundationStatusUseCase`とLocal Application Channelの明示的なoperation registryを構成し、WebView2のRequestを同一プロセスの`Adm.Application`へ渡す。Server Host、Kestrel、HTTP Client、localhost待受はこの経路に含めない。

## 許可operation

P1-034で製品registryへ登録するoperationは`getFoundationStatus`だけである。Handlerは`GetFoundationStatusUseCase`へ固定登録し、Reflection、型探索、DI全自動公開、汎用RPCは利用しない。`getHostInfo`はPlatform Bridge専用であり、Local Channelからは実行できない。

ResponseはP1-033のv1契約に従い、`state`、`apiVersion`、`contractVersion`、`serverTimeUtc`に加えて製品名、製品版、`executionMode=local`を返す。JSONはcamelCaseでシリアライズする。

## Web側Adapter

`createLocalDataAccess`がP1-031のDataAccess Portを実装し、`getFoundationStatus`をLocal Channel Requestへ変換する。Responseの構造検証に失敗した場合やChannelが利用できない場合は、例外本文を露出せず、DataAccessの安全なFailureへ変換する。Local modeの固定originでのみWebView2 Local Transportを選択し、通常ブラウザとServer modeは既存HTTP Adapterを利用する。

## 終了処理

WPF終了時にLocalCompositionRootをDisposeし、保留処理へCancellationTokenを伝播する。キャンセル後の要求は`channel_unavailable`へ変換し、WebView2やServerプロセスを起動・停止する処理は行わない。

## 検証

Application Use Case、Local Adapter、Composition Rootの正常系・例外秘匿・終了後拒否をテストする。Architecture検査ではWPFの許可参照を`Adm.Application`に限定し、`Adm.Server.Host`、ASP.NET Core、HTTP Clientへの参照を追加しないことを確認する。
