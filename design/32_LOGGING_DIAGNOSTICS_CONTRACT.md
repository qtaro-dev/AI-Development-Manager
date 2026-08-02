# P1-008 ログ・診断契約

## 1. 責務

診断ログはServerの起動、HTTP要求、失敗を調査するための記録とする。業務データの変更履歴、認証イベント、監査証跡は別責務とし、P1-008では実装しない。

## 2. 出力形式と共通項目

`AdmJsonLoggerProvider`が1行1JSONでConsoleへ出力する。Console起動と将来のWindows Service起動は同じProviderと項目契約を利用する。

基本項目は次のとおり。

| 項目 | 内容 |
|---|---|
| `timestamp` | UTCのISO 8601時刻 |
| `level` | Information、Warning、Error等 |
| `category` | logger category |
| `event_id` | イベント識別子 |
| `message` | 秘密値を除去した表示文言 |
| `exception_type` | 例外型のみ。例外本文・Stack Traceは記録しない |
| `properties` | 構造化プロパティとScope |

起動ログには起動モード、環境名、Build情報を含める。要求ログには`trace_id`、HTTP method、path、status、経過時間を含める。

## 3. 追跡ID

要求ヘッダー`X-Request-Id`を入力として検証する。1～64文字の英数字、`.`、`_`、`-`だけを許可し、不正値または未指定時は`adm-`接頭辞の新しいIDを生成する。受け入れたIDまたは生成したIDを同じ応答ヘッダー、ログScope、要求開始・完了ログへ設定する。

追跡IDは利用者入力をそのまま信頼せず、ログ検索用の相関値としてのみ扱う。認証・認可・監査上の本人識別子とは別物である。

## 4. 秘密情報の非記録

次のキーは値を`[REDACTED]`へ置換する。

- Password、Cookie、Authorization、Token、Secret、PrivateKeyを含むキー
- Bearerトークン
- `password=`、`token=`、`secret=`、`api-key=`等の本文・QueryString

HTTP要求ではヘッダー値、QueryStringの生値、要求本文をログへ記録しない。例外発生時は例外型だけを記録し、例外本文とStack Traceは利用者応答・診断ログへ出さない。詳細な内部例外の保存・相関は、保護された運用ログ方式を別途決定する。

## 5. 保持とローテーション

P1-008では出力先をConsoleに限定し、ファイル保持期間、サイズローテーション、外部収集は実装しない。Windows Service・配布方式と合わせて後続チケットで設定境界を確定する。ログへ秘密情報が混入した場合は保持先を増やさず、原因修正と再発防止を優先する。
