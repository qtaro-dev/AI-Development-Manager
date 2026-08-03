# P1-036 実行プロファイル契約

## 保存スキーマ

`%LOCALAPPDATA%\AI Development Manager\Config\execution-profile.json` に次のJSONを保存する。`schemaVersion`は必須で、現在値は`1`とする。

```json
{
  "schemaVersion": 1,
  "mode": "local",
  "serverUri": null
}
```

`mode`は`local`または`server`。Serverの場合は絶対URIを要求し、LAN接続はHTTPSのみ許可する。`http`は`--allow-loopback-http`を付けた開発・診断時にloopbackへ限定して許可する。token、password、証明書秘密鍵などはモデルに存在せず、保存しない。

## 起動時の優先順位

1. `--server-url=<uri>` が明示された場合は診断用の一時上書きとして使用する。
2. 引数がない場合は保存済みプロファイルを検証して使用する。
3. 欠落、読込不能、JSON破損、未知フィールド、未知schema、不正URIの場合は警告コードを診断結果に残し、Localへ復旧する。
4. コマンドライン上書きは永続設定を変更しない。

## 保存と復旧

保存はユーザー領域の同一ディレクトリに一意な一時ファイルを作成し、書込み・flush後に置換する。既存ファイルの置換に失敗した場合、一時ファイルを削除し、旧ファイルを保持する。設定更新はLocal Channelの`executionProfile.update`、読込は`executionProfile.get`で行う。

## UI境界

P1-036は契約、保存、検証、起動優先順位のみを対象とする。設定画面、初回ウィザード、再試行導線はP1-037以降で扱い、Local modeの既定動作を変更しない。
