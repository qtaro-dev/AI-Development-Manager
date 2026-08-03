# P1-028 ローカルファースト実行モデルADR

## 目的

AI Development Managerの主製品をWindowsアプリとし、Server未導入、Server停止、Server障害の状態でも、Windowsアプリ単体で通常機能を利用できる実行モデルを設計判断として確定する。

Server/APIは、AI連携、LAN共有、通常ブラウザ利用、REST API、外部接続、将来のクラウド連携を提供する任意導入の追加機能として位置付ける。

## 背景

Phase 1の従来設計では、WPFはServerが配信するWeb UIへ接続するClientであり、業務データ操作はServer APIを経由する前提だった。そのため、クリーンWindows 11環境の実機検証で次の問題が発生した。

- Server MSIがService起動時に失敗し、インストールを完了できなかった。
- WindowsアプリがServer接続待ちとなり、Serverなしでは本体機能へ進めなかった。
- 接続失敗画面から終了、設定変更、ローカル利用への切替ができなかった。
- .NET Desktop Runtime未導入環境ではClientを起動できなかった。

この結果を踏まえ、Serverを通常利用の必須条件から外し、Windowsアプリが共通Web UIとApplication層を同一プロセスで利用できる構成へ設計を改訂する。

## 前提・依存関係

- P0-023完了
- P1-001～P1-027の実装結果およびPhase 1正式完了保留記録
- Windows 11 64-bit、.NET 10、WPF、WebView2、React／TypeScript／Viteの採用結果
- Markdown、添付、`.adm-meta`を正本とし、SQLiteを再構築可能なキャッシュとする既存方針
- 原子的保存、ETag、回復ジャーナル、バックアップ、Windows依存隔離の既存契約
- ユーザー承認済みのローカルファースト方針

## 対象範囲

- Windowsアプリ、Server、共通Web UI、Application層の責務再定義
- ローカルモードとServer接続モードの実行境界
- Windowsアプリ内でApplication層を利用する論理構成
- Web UIから実行経路を切り替えるDataAccess Portの位置付け
- Windows固有操作用Platform Bridgeと、業務操作用Local Application Channelの責務分離
- Server未導入、停止、障害時のWindowsアプリの必須動作
- WindowsアプリとServerが同一プロジェクトを扱う場合の所有・競合方針
- Phase 1完了条件と、Server関連未完了事項の移管方針
- 既存設計・ADRのうち、今回の判断で置換または補足が必要な箇所の一覧化

## 対象外

- 製品コード、PoCコード、テストコードの変更
- WebView2へ組み込みWeb UIを読み込む技術検証
- DataAccess Port、Local Application Channel、Application Serviceの実装
- プロジェクト登録、ファイル走査、Markdown解析、チケット、添付、テスト、検索、バックアップ等の業務機能実装
- Server MSI、Windows Service、HTTPS、認証、権限、LAN公開の修正・実装
- P1-029以降のチケット作成
- Native WPFによる業務画面の再実装
- Windowsアプリ内でのKestrelまたは隠れたlocalhost Serverの自動起動

## 対象ファイルまたは対象モジュール

本チケットは設計文書のみを対象とする。

- `design/01_INTEGRATED_BASIC_DESIGN.md`
- `design/02_TECHNOLOGY_AND_ADR.md`
- `design/04_PHASE_PLAN.md`
- `design/30_PHASE1_IMPLEMENTATION_PLAN.md`
- `design/43_WPF_WEBVIEW2_SHELL_CONTRACT.md`
- `design/44_WPF_BRIDGE_CONTRACT.md`
- `design/46_INSTALLER_DISTRIBUTION_CONTRACT.md`
- `design/47_SERVER_INSTALLER_SERVICE_CONTRACT.md`
- `design/48_WPF_CLIENT_INSTALLER_CONTRACT.md`
- `design/49_PHASE1_INTEGRATION_GATE_RESULT.md`
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

`src/`、`tests/`、`installer/`は変更しない。

## 具体的な実装内容（設計作業）

1. `ADR-019 ローカルファースト実行モデル`を追加し、Windowsアプリを主製品、Serverを任意導入の追加コンポーネントとして確定する。
2. 通常機能はServerなしで利用可能とし、Server未導入、停止、接続失敗がWindowsアプリの起動とローカル利用を妨げないことを明記する。
3. ローカルモードではKestrel、localhostポート、HTTP APIを起動せず、WPFプロセス内のApplication層を利用する構成を確定する。
4. ServerモードではHTTP API Adapterから同じApplication層を利用し、ローカルとServerで業務ロジックを複製しないことを確定する。
5. React UIが具体的な通信方式へ直接依存せず、DataAccess Portを介してLocalまたはHTTPの実行経路を選択する方針を記録する。
6. Windows固有操作用Platform Bridgeと、業務操作用Local Application Channelを別契約として扱うことを確定する。
7. Windowsアプリは初回起動時からローカルモードを既定とし、Server接続を必須選択にしない方針を記録する。
8. 同一プロジェクトをWindowsアプリとServerが同時に書き込まない所有方針を記録し、詳細なリース形式と実装は後続チケットへ分離する。
9. P1-025とP1-027の結果を過去の実施記録として保持しつつ、Server MSIの未完了をローカル製品の完了条件から分離する方針を記録する。
10. 従来設計の各記述を、`維持`、`補足`、`置換`、`履歴として保持`のいずれかに分類する。

## Phase 1のLocal Application Channel最小方針

Phase 1ではLocal Application Channelを作り込み過ぎず、単発の要求と応答に必要な最小境界だけを設計対象とする。

- Request
- Response
- Error

RequestとResponseを対応付け、操作名、入力、結果、利用者向けエラーを安全に受け渡せることを最小要件とする。具体的なEnvelope項目、型、検証規則は後続の契約チケットで確定する。

次の機能はPhase 1の最小契約へ実装せず、必要な業務機能が生じた段階で独立チケットとして追加する。

- Progress通知
- Cancel要求
- Streaming
- 分割転送
- 双方向イベント購読
- 長時間処理の汎用ジョブ基盤

将来拡張を理由に、未使用のメッセージ種別、汎用RPC基盤、プラグイン機構を先行実装しない。後方互換を壊さず契約を拡張できる責務境界だけをADRへ記録する。

## テスト内容・レビュー内容

### 設計シナリオレビュー

次の各状態でWindowsアプリがローカルホームへ進めることを、責務図と起動シーケンスで確認する。

1. Serverがインストールされていない。
2. Server Serviceが停止している。
3. Serverのインストールまたは起動に失敗している。
4. 設定済みServerへ一時的に接続できない。
5. ネットワークを利用できない。

### 責務境界レビュー

- ローカル経路にServer Host、Kestrel、HTTP APIが含まれていない。
- ローカルとServerが同じCore/Application契約へ収束している。
- Platform BridgeとLocal Application Channelが混在していない。
- React UIの業務コンポーネントがHTTP Clientへ直接固定されていない。
- Server固有の認証、HTTPS、LAN、Service責務がローカル起動条件へ混入していない。

### MVP範囲レビュー

- Local Application ChannelがRequest、Response、Errorの最小範囲に限定されている。
- Progress、Cancel、Streaming等が対象外として明記されている。
- 後続実装の詳細を根拠なく確定していない。
- P1-029以降の実装またはチケットを先行作成していない。

### 文書検査

- 設計資料間で「Server必須」と「Server任意」が併存していない。
- 旧判断を無断で削除せず、置換理由と履歴が追跡できる。
- Markdownの見出し、表、リンク、Mermaid図を実際に表示して確認する。
- `git diff --check`で文書差分に基本的な形式不良がない。

## 完了条件

- Windowsアプリを主製品、Serverを追加機能とするADRが一意に確定している。
- Server未導入、停止、障害、接続不能の各状態で、Windowsアプリのローカル起動と通常利用を妨げないことが明記されている。
- ローカルモードがServer Host、Kestrel、localhostポート、HTTP APIへ依存しない構成になっている。
- WindowsアプリとServerが同じCore/Application契約を利用し、業務ロジックを複製しない責務図がある。
- DataAccess Port、Platform Bridge、Local Application Channel、HTTP API Adapterの責務が区別されている。
- Phase 1のLocal Application ChannelがRequest、Response、Errorの最小範囲に限定されている。
- Progress、Cancel、Streaming等が後続の独立チケットへ明確に分離されている。
- Server MSIの未完了事項と、Windowsアプリ単体のPhase 1完了条件が分離されている。
- 既存設計の置換対象と履歴保持対象が一覧化され、矛盾する現行仕様が残っていない。
- 製品コード、PoC、P1-029以降のチケットを変更していない。
- ユーザーがADRと設計差分を確認し、承認または修正指示を出せる。

## ユーザーが目視確認する内容

- Windowsアプリ、Local Application Channel、Application層、Server/APIの責務図
- ServerなしのWindowsアプリ起動シーケンス
- Server未導入、停止、障害、接続不能時の期待動作一覧
- Local Application ChannelのPhase 1対象／対象外
- P1-025、P1-027の扱いと新しいPhase 1完了条件
- 既存設計の置換・補足・履歴保持一覧

## 想定されるリスク

- 「Windowsアプリ単体」を、内部でlocalhost Serverを自動起動する構成へ読み替えてしまう。
- Local経路とHTTP経路で業務ロジックやエラー仕様が分岐する。
- Local Application Channelを汎用RPC基盤へ過剰設計する。
- Platform Bridgeへ業務データ操作を混在させる。
- Server関連の過去結果を削除し、実機不具合の経緯を追跡できなくする。
- WindowsアプリとServerが同一プロジェクトへ同時書き込みし、索引、バックアップ、Markdownを競合させる。
- Phase 1の設計変更へ業務機能実装やInstaller修正を混在させる。

## 完了後に更新すべき設計資料

- `design/00_INDEX.md`
- `design/01_INTEGRATED_BASIC_DESIGN.md`
- `design/02_TECHNOLOGY_AND_ADR.md`
- `design/04_PHASE_PLAN.md`
- `design/30_PHASE1_IMPLEMENTATION_PLAN.md`
- `design/43_WPF_WEBVIEW2_SHELL_CONTRACT.md`
- `design/44_WPF_BRIDGE_CONTRACT.md`
- `design/46_INSTALLER_DISTRIBUTION_CONTRACT.md`
- `design/47_SERVER_INSTALLER_SERVICE_CONTRACT.md`
- `design/48_WPF_CLIENT_INSTALLER_CONTRACT.md`
- `design/49_PHASE1_INTEGRATION_GATE_RESULT.md`
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

## 完了時に残す証拠

- ADR差分
- 責務図と起動シーケンス
- 既存設計の置換・補足・履歴保持一覧
- シナリオレビュー結果
- `git diff --check`結果
- ユーザー承認または修正指示

## 状態

実装完了（レビュー待ち）。ADR正本は`design/50_ADR_019_LOCAL_FIRST_EXECUTION_MODEL.md`。指定された設計資料へLocal-firstの責務境界、起動シーケンス、既存判断の分類、Phase 1最小Channelを反映した。`output/pdf/AI_Development_Manager_Local_First_Design_Review_v1.0.pdf`を設計意図の参照資料として確認し、必須初回ウィザードなしのローカルホーム、ローカル利用者ULID、`.adm-meta`所有リース、Self-contained Clientを後続入力として反映した。`src/`、`tests/`、`installer/`、`poc/`、P1-029以降は変更していない。

本チケットはADR・設計資料の実装を完了し、ユーザーレビュー待ちである。P1-029の作成・実施は、P1-028の承認後にユーザー指示を待つ。
