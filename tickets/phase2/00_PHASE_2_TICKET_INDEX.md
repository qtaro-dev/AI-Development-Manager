# Phase 2 本体チケット一覧

版: 1.1
状態: Local Project登録初期群完了
対象: Phase 2 Local Project Registration

## 1. Foundation完了判定

P2-A01～P2-A06のコミット済み実装、関連テスト、Architecture検査、および実装担当者から提示された最終結果を照合した。

| 判定対象 | 判定 | 根拠 |
|---|---|---|
| P2-A01 Local Channel lifecycle | 合格 | timeout、caller cancellation、dispose、pending一括終了、late／duplicate response無視の実装とWebテスト |
| P2-A02 WPF／WebView lifecycle | 合格 | Window lifetime cancellation、接続世代管理、イベント解除、Dispose後guard、async例外境界の実装と.NETテスト |
| P2-A03 Server設定復旧 | 合格 | `?settings=1`を限定許可する同一source policyと回帰テスト |
| P2-A04 Platform Bridge検証 | 合格 | 厳密JSON型、16 KiB、深度8、safe error、Web／.NET不正入力テスト |
| P2-A05 Web startup状態 | 合格 | loading／ready／degraded／recovered／error／retryingと再試行テスト |
| P2-A06 Port／Infrastructure境界 | 合格 | Business／Host／Bridge分離、Infrastructure→Application許可、Architecture検査 |
| 提示された品質ゲート | 合格 | Debug／Release Build、Architecture、.NET 88件、Web 49件、typecheck、lint、bundle、`git diff --check`が成功 |

実装面ではPhase 2 Foundation Preparationを完了と判定する。Phase 2本体は開始可とする。ただし、今回の報告にはP2-A02～A05の完了条件に含まれる変更後のWindows実機スモーク、Server接続失敗から設定保存までの実機統合、主要viewport目視の証拠が明記されていない。これらはP2-009開始前の証拠ゲートとし、未確認のままProject UIへ進めない。

## 2. 初期チケット群の範囲

現行のフェーズ計画とLocal First ADRに従い、最初の本体チケット群はLocal modeのProject登録・選択・解除とローカルNTFS境界に限定する。Server認証／HTTPS／LAN、走査、Markdown解析、`.adm-meta`文書ID、SQLite索引は混在させない。

登録はアプリ側カタログへの登録であり、選択したProject配下のMarkdownや`.adm-meta`を変更しない。したがって、このチケット群ではProject所有リースも取得しない。Project配下への初回書込みを開始する後続フェーズでは、所有リースを独立チケットで先行実装する。

## 3. 実施順序

| 順序 | チケット | タイトル | 依存関係 | 状態 |
|---:|---|---|---|---|
| 1 | [P2-001](P2-001_LOCAL_PROJECT_REGISTRATION_CONTRACT.md) | Local Project登録契約・ドメインモデル | P2-A01～P2-A06 | 完了（`ebec8d5`） |
| 2 | [P2-002](P2-002_SAFE_LOCAL_PROJECT_ROOT_VALIDATION.md) | 安全なLocal Project Root検証 | P2-001、P2-A07 | 完了（`2910db8`） |
| 3 | [P2-003](P2-003_REGISTERED_PROJECT_CATALOG_PERSISTENCE.md) | 登録Projectカタログ永続化 | P2-001 | 完了（`85d23e9`） |
| 4 | [P2-004](P2-004_REGISTER_LOCAL_PROJECT_USE_CASE.md) | Local Project登録Use Case | P2-002、P2-003 | 完了（`aafbf5d`） |
| 5 | [P2-005](P2-005_UNREGISTER_LOCAL_PROJECT_USE_CASE.md) | Local Project登録解除Use Case | P2-003、P2-004 | 完了（`243ecb3`） |
| 6 | [P2-006](P2-006_LIST_SELECT_LOCAL_PROJECT_USE_CASE.md) | Local Project一覧・選択Use Case | P2-003、P2-004 | 完了（`b600ed3`） |
| 7 | [P2-007](P2-007_LOCAL_PROJECT_DATA_ACCESS_CHANNEL.md) | Project DataAccess／Local Channel接続 | P2-004～P2-006、P2-A08 | 完了（`01f7cfb`） |
| 8 | [P2-008](P2-008_WINDOWS_PROJECT_FOLDER_PICKER.md) | Windows Projectフォルダー選択Bridge | P2-001、P2-A02、P2-A04、P2-A06 | 完了（`15c840c`） |
| 9 | [P2-009](P2-009_LOCAL_PROJECT_WEB_UI.md) | Local Project登録・選択Web UI | P2-007、P2-008、実機／目視証拠ゲート | 完了（`9523cfe`） |
| 10 | [P2-010](P2-010_LOCAL_PROJECT_REGISTRATION_GATE.md) | Local Project登録統合ゲート | P2-001～P2-009 | 完了（2026-08-07） |

P2-002とP2-003はP2-001完了後に並行可能である。P2-008も契約確定後に独立して進められるが、1チケットずつレビューする場合は表の順序を正とする。

## 4. 共通設計ガード

- Local Firstを維持し、Local modeでKestrel、HTTP、Server自動起動を要求しない。
- 業務操作は`BusinessDataAccessPort`とLocal Application Channelを通す。
- フォルダー選択だけをPlatform Bridgeへ置き、Project登録処理やファイル操作をBridgeへ公開しない。
- operationは明示allowlistへ個別登録し、汎用RPC、自動公開、Reflection公開を採用しない。
- `Core → 外側`、`Application → Infrastructure／WPF`、`Infrastructure → WPF／Server`の逆依存を作らない。
- Project Root配下の既存ファイルを登録・解除時に変更または削除しない。
- UNC／NAS、Reparse Point経由、非NTFSは本初期群の正式対応外とする。

## 5. Phase 2初期群完了ゲート

P2-010で、Local Projectの登録、再起動後の再読込、選択、重複拒否、登録解除、無効Root拒否、取消、終了処理をクリーンWindows 11環境で確認する。Debug／Release Build、全.NET／Webテスト、Architecture、typecheck、lint、bundle、`git diff --check`が合格し、Project配下が登録前後で意図せず変更されていないことを証拠化するまで、走査・watch・Markdown分類へ進まない。

## 6. 完了結果

2026-08-07、最新MSIを用いたクリーンWindows 11 VM手動E2Eが合格した。Local起動、execution-profile保存・復元、Project folder選択／取消、登録、一覧、選択／解除、Catalog再読込、重複／network Root拒否、Root異常警告、登録情報保持、利用者ファイル不削除、終了処理を確認した。

P2-010完了処理では最新Debug／Release成果物に対する.NET Testを再実行し、各152件成功・失敗0を確認した。詳細、再実行できなかった環境依存検査、後続事項は`P2-010_LOCAL_PROJECT_REGISTRATION_GATE.md`および`design/57_PHASE2_LOCAL_PROJECT_REGISTRATION_GATE_RESULT.md`を正本とする。

最終判定は合格。Phase 2 Local Project登録機能を正式完了とする。
