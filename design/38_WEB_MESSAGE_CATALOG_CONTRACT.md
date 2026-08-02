# P1-015 Web日本語文言カタログ契約

## 決定

MVPの利用者向け表示文言は`src/Adm.Web/src/messages/catalog.ts`を単一参照元とし、コンポーネントは型付き`message` APIで参照する。内部値、API名、エラーコードは通常表示へ直接出さない。

## キーと引数

- キーは`領域.意味`形式とする。
- 文言中の引数は`{{name}}`で表し、引数型を`MessageArguments`で定義する。
- 引数不足や余分な引数はTypeScript検査で検出する。
- `blocked`などの内部値は、利用者向けの一般語へ明示的に変換する。

## 静的検査

```powershell
cd D:\Dev\AI Development Manager\src\Adm.Web
npm run messages:check
```

検査は辞書にない参照、未参照の辞書キー、製品TSX内の日本語直書きを失敗させる。テストfixtureの説明文は製品表示ではないため、直書き検査の対象外とする。検査結果は通常のCI証拠へ保存する。

## 対象外

英語UI、実行時の言語切替、業務画面固有の全メッセージ、Theme、レイアウト、APIエラー契約そのものは後続チケットで扱う。APIエラーコードから表示キーへの予約境界だけを維持する。
