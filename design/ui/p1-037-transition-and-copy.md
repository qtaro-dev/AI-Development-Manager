# P1-037 状態遷移・文言・操作契約

## 状態遷移表

| 状態 | 到達条件 | 利用者向け主文 | 主操作 | 補助操作 |
|---|---|---|---|---|
| 初回・Local準備 | 初回起動、または初回設定の未完了 | 初回設定: このPCで利用を開始します | このPCで続ける | 接続先を設定、終了 |
| 通常・Local起動 | 初回設定済み、Local profile | ローカルUIを表示しています | 本体を利用 | 設定 |
| Local表示 | Local準備成功 | ローカルUIを表示しています | 本体を利用 | 設定 |
| Server接続中 | Server profileを選択 | サーバーへの接続を確認しています | 待機 | 取消、終了 |
| Server接続成功 | readiness成功 | 共通画面を表示しています | 本体を利用 | 設定 |
| Server接続失敗 | readiness timeoutまたはHTTP失敗 | サーバーに接続できません | このPCで続ける | 接続先を設定、もう一度試す、終了 |
| 設定 | 「接続先を設定」を選択 | 利用方法を選択 | 保存 | 取消 |
| Web UI読込失敗 | 埋込資産または初期化失敗 | 画面を読み込めません | もう一度読み込む | 設定を確認、終了 |

P2-A05ではWeb startupの状態正本を`loading`、`ready`、`degraded`、`recovered`、`error`、`retrying`として管理する。foundation取得とexecution-profile取得の両方が成功するまで`ready`を表示せず、profile fallbackは`degraded`としてLocal継続と設定確認を案内する。再試行で復旧した場合は`recovered`とし、古い要求やUnmount後の応答は表示へ反映しない。

## 文言辞書キー案

| Key | 表示文言 | 用途 |
|---|---|---|
| `startup.local.title` | 初回設定: このPCで利用を開始します | 初回Local設定 |
| `startup.local.description` | この画面は初回のみ表示します。次回からはLocalホームへ直接進みます。 | 初回Local説明 |
| `startup.local.continue` | このPCで続ける | Local主操作 |
| `startup.server.settings` | 接続先を設定 | 設定導線 |
| `startup.exit` | 終了 | 終了導線 |
| `connection.checking` | サーバーへの接続を確認しています | 接続中 |
| `connection.failed.title` | サーバーに接続できません | 接続失敗 |
| `connection.failed.continueLocal` | このPCで続ける | Local復帰 |
| `connection.retry` | もう一度試す | 再試行 |
| `connection.failed.description` | このPCだけで続けるか、接続先を確認してから再試行できます。 | 接続失敗説明 |
| `profile.local.title` | このPCで利用 | Profile選択 |
| `profile.server.title` | LAN Serverへ接続 | Profile選択 |
| `profile.save` | 保存 | Profile更新 |
| `profile.cancel` | 取消 | Profile破棄 |
| `profile.serverUrl.disabled` | Local選択中は入力できません（Server選択時に表示） | Local選択時のURL欄 |
| `webview.failed.title` | 画面を読み込めません | 埋込UI失敗 |
| `webview.retry` | もう一度読み込む | 埋込UI再試行 |
| `webview.settings` | 設定を確認 | 埋込UI設定導線 |

画面主文には`blocked`、`origin`、`fallback`、`runtime`、例外本文、秘密情報を表示しない。診断情報は別領域へ分離する。

## キーボード・フォーカス

1. 初期フォーカスは状態説明の後の主操作。
2. Tab順は主操作、設定、再試行、終了の順で、DOM／表示順を一致させる。
3. Enterはフォーカス中の操作を実行する。
4. Escapeは設定パネルまたはダイアログを閉じ、呼出元へフォーカスを戻す。
5. 主要操作は色だけで区別せず、文言とフォーカスリングを併用する。

## レビュー用チェックリスト

- [ ] Local開始にServer通信を要求していない
- [ ] 初回設定画面は毎回表示せず、設定済みならLocalホームへ直接進む
- [ ] Local選択時はServer URL入力が無効または非表示である
- [ ] Server失敗からLocal、設定、再試行、終了へ到達できる
- [ ] Server選択は明示操作である
- [ ] Web UI読込失敗とServer接続失敗を別状態としている
- [ ] 1440／820／320px相当で主要操作が隠れない
- [ ] 100～200% DPIで文字切れ・重なりがない設計である
- [ ] Tab、Enter、Escape、フォーカス復帰を確認できる
- [ ] 専門語を主文に出していない
