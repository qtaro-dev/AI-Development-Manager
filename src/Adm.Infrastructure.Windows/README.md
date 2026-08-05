# Adm.Infrastructure.Windows

Windows固有Adapterの配置です。P1-002ではAdapter実装を作りません。

参照規則: Application Portを実装するWindows Adapterに限り`Adm.Application`と`Adm.Core`を参照できます。`Adm.Server.Host`、`Adm.Wpf`、`poc/`は参照しません。Project関連のファイル処理、path security、atomic save、scan/watchはこの境界へ配置し、WPFへ配置しません。
