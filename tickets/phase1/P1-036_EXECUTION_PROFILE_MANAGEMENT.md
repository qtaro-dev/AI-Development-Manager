# P1-036 実行プロファイル管理

## 目的

Windowsアプリの起動先を、既定の「このPCで利用」と、利用者が明示的に選ぶ「LAN Serverへ接続」に分け、安全に保存・読込できる実行プロファイル機能を実装する。

## 背景

Local modeはServer未導入・停止中でも必ず利用できなければならない。接続先設定の破損やServer障害でWPF起動が妨げられないよう、UI実装より先に設定モデル、優先順位、検証、復旧規則を確定する。

## 前提・依存関係

- P1-034、P1-035完了・承認済み
- `design/50_ADR_019_LOCAL_FIRST_EXECUTION_MODEL.md`
- LAN利用時はHTTPSを正式要件とする既存設計

## 対象範囲

- `Local`と`Server`の実行プロファイルモデル
- Localを常時利用可能な既定値とする規則
- Server URLの保存、検証、選択
- 設定ファイルのschema versionと原子的保存
- 破損・欠落・旧版設定からLocalへ安全に復旧する処理
- 設定取得・更新用Application Use CaseとLocal Channel operation
- 起動引数による開発・診断用の明示的上書き
- 秘密情報を保存しないことの検査

## 対象外

- 設定画面、初回ウィザード、画面遷移
- Server自動探索、自動起動、自動インストール
- Serverへの接続確認、再試行UI
- 認証token、資格情報、証明書秘密鍵の保存
- HTTPS証明書作成・配布・信頼設定
- 複数Serverの同期、フェイルオーバー
- 業務データのLocal／Server間同期

## 対象ファイルまたは対象モジュール

- `src/Adm.Application/`の設定Use CaseとPort
- `src/Adm.Infrastructure.Windows/`またはWPF専用設定Adapter
- `src/Adm.Wpf/`のComposition登録とLocal Channel Handler
- `src/Adm.Web/src/data-access/`の型付き設定操作
- `tests/Adm.Application.Tests/`
- Windows設定Adapter、Channel、Web Adapterのテスト
- `design/`の実行プロファイル契約資料

## 具体的な実装内容

1. `Local`／`Server`を判別するversion付き設定モデルを定義する。
2. 初回、設定なし、読込不能、不正値では必ずLocalを選択し、警告を診断ログへ残す。
3. Server URLは絶対URIとし、LAN接続はHTTPSだけを許可する。開発用途のloopback HTTPは明示的な開発設定に限定する。
4. 設定をユーザー領域へ一時ファイル、flush、置換の順で原子的に保存する。
5. 保存対象からtoken、password、秘密鍵等を除外し、未知フィールドと旧schemaの扱いを定義する。
6. `executionProfile.get`／`executionProfile.update`相当の型付き操作をApplication、Local Channel、DataAccess Portへ追加する。
7. コマンドライン上書きは開発・診断用途として優先順位と有効範囲を明記し、永続設定を無断変更しない。

## テスト内容

- 初回起動、設定欠落、正常Local、正常Server
- 不正JSON、途中書込み、未知schema、不正URL、LAN HTTPの拒否
- Localへの安全なfallbackと利用者向けエラー
- 原子的保存と旧ファイル保持規則
- 秘密語・秘密値が保存されないこと
- 同時更新時に破損しないこと
- Application、Channel、TypeScript型の一致
- Build、Test、Architecture検査

## 成功条件

- Server設定がなくてもLocalで必ず起動できる。
- Server profileは利用者の明示操作でのみ選択・保存される。
- 破損設定がアプリ起動を妨げない。
- LAN URLにHTTPS要件が適用される。
- 設定保存に秘密情報を含めない。

## 完了条件

- 設定契約、保存Adapter、Use Case、型付きChannel操作、テストが完成している。
- 設定ファイル例、保存先、優先順位、復旧手順が設計資料に記録されている。
- UIを実装しておらず、P1-037以降へ着手していない。

## ユーザーが目視確認する内容

- Local／Server設定ファイル例
- 不正設定からLocalへ戻るテスト結果
- LAN HTTPが拒否され、HTTPSが受理される結果
- 保存データに資格情報がないこと

## 想定されるリスク

- Server未接続を起動失敗として扱う。
- 設定ファイルへ認証情報を平文保存する。
- 開発用loopback例外をLANへ拡張する。
- UI都合の状態をApplication契約へ混在させる。
- 設定更新中の異常終了でファイルを失う。

## 完了後に更新すべき設計資料

- `design/00_INDEX.md`
- `design/30_PHASE1_IMPLEMENTATION_PLAN.md`
- `design/50_ADR_019_LOCAL_FIRST_EXECUTION_MODEL.md`
- P1-036 実行プロファイル契約資料
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`
- 本チケット

## 完了時に残す証拠

- 設定schemaとサンプル
- 正常・破損・移行・原子的保存テスト結果
- 秘密情報非保存検査結果
- Build、Test、Architecture検査結果
- `dotnet --version`と`git diff --check`結果

## 状態

実装済み。P1-036完了。

実装範囲：Application実行プロファイル契約、WPFユーザー設定の原子的保存、Local fallback、HTTPS／loopback HTTP検証、Local Channel操作、Web DataAccess型定義、起動引数優先順位、契約テスト。
