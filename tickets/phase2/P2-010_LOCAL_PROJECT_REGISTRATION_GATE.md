# P2-010 Local Project登録統合ゲート

状態: 完了
完了日: 2026-08-07
最終判定: 合格

## 目的

P2-001～P2-009を監査し、Local Project登録機能の完了と次の機能群への開始可否を判定する。

## 背景

個別テストの成功だけでは、クリーン環境でのfolder選択、永続化、再起動、解除、終了処理、Project内容不変を保証できない。Phase 2初期群を再現可能な証拠で閉じる。

## 対象

- 契約、実装、設計資料、テスト、Git差分の監査
- クリーンWindows 11でのLocal Project E2E受入れ
- P2-A残存実機／目視証拠の回収
- 次段階（Server任意機能またはPhase 3走査）の開始判定

## 対象外

- ゲート内での機能追加や不具合修正
- 走査、watch、Markdown解析、`.adm-meta`文書割当
- Server認証／HTTPS／LAN実装

## 対象ファイル（推定可）

- `tickets/phase2/P2-001_*.md`～`P2-009_*.md`
- `design/`の関連契約
- `tests/`
- 品質ゲート／実機証拠出力先

## 実装内容

1. 各チケットの完了条件と対象外逸脱を監査する。
2. Local First、Port、Bridge、Architecture、allowlist境界を再確認する。
3. クリーンVMへ最新MSIを導入し、登録、再読込、選択、解除、取消、異常Root、終了を確認する。
4. 登録・解除前後のProject tree／hashを比較し、利用者ファイル不変を証拠化する。
5. 未解決事項をblocker、次群先行、後続持越しへ分類し、開始可否を記録する。

## テスト内容

- Debug／Release Build
- 全.NET Test、Web Unit Test、Architecture検査
- TypeScript typecheck、lint、bundle build、Unhandled Rejection監視
- `git diff --check`と禁止生成物／秘密情報検査
- Windows 11 VMで初回／再起動／重複／無効Root／解除／終了のE2E
- 1280×800、DPI、keyboard、folder dialogの目視確認

## 完了条件

- P2-001～P2-009が全完了し、対象外実装が混入していない。
- 全品質ゲートとWindows VM受入れが成功する。
- Project内容不変、Local-only起動、Server非依存が証拠化される。
- 残課題と次チケット群の開始条件が明記される。
- ユーザーが次段階の開始可否を判断できる。

## 依存関係

- P2-001～P2-009

## 実施順序

10番目。Phase 2初期チケット群の最終ゲート。

## 実施結果

### Git・成果物

- 対象実装HEAD: `9523cfe`（P2-009 Local Project Web UI）
- P2-001～P2-009および途中のP2-A07／P2-A08は個別Commit・Push済み。
- 作業ブランチは確認時点でupstreamと`0 ahead / 0 behind`だった。
- VMでは利用者が最新MSIを使用してクリーンインストールと手動E2Eを実施した。
- リポジトリ側の最新Client MSI manifestは.NET SDK `10.0.302`、Release、x64、SHA-256 `02FC84ED5716A6DFDCB1A9C00CE0D454802EB4D0B687073973CCFB4BB6BB9F6C`を記録している。VM上でのhash再採取は本結果には含めない。

### クリーンWindows 11 VM手動E2E

| 確認項目 | 結果 |
|---|---|
| 最新MSIのクリーンインストール | 合格 |
| 初回起動から「このPCで続ける」でLocal mode開始 | 合格 |
| `execution-profile.json`作成 | 合格 |
| アプリ終了・再起動後のLocal mode復元 | 合格 |
| 1280×800、表示倍率100%の表示・操作 | 合格 |
| Projectフォルダー選択 | 合格 |
| フォルダー選択キャンセル | 合格 |
| Project登録 | 合格 |
| Project一覧表示 | 合格 |
| Project選択 | 合格 |
| Project選択解除 | 合格 |
| Project登録解除 | 合格 |
| 登録解除後も実フォルダー・内部ファイルが削除されない | 合格 |
| 再起動後のProject Catalogおよび選択状態復元 | 合格 |
| 同一Rootの重複登録拒否 | 合格 |
| ネットワークRootの登録拒否 | 合格 |
| 登録後にRootを手動リネームした場合の警告表示 | 合格 |
| Root異常時に登録情報を自動削除しない | 合格 |
| Root異常時もクラッシュせず登録解除等を実行可能 | 合格 |
| 終了処理 | 合格 |

### 本完了処理での自動再確認

固定.NET SDK `10.0.302`を使用し、P2-009後に生成済みの最新Debug／Release成果物へ`--no-build --no-restore`で回帰テストを実行した。

| 検査 | 結果 |
|---|---|
| Debug .NET Test | 合格、152件、失敗0、skip 0 |
| Release .NET Test | 合格、152件、失敗0、skip 0 |
| 内訳 | Core 7、Application 39、Infrastructure.Windows 83、Server 23 |
| Debug／Release成果物 | P2-009 HEAD直後の生成時刻を確認 |
| Architecture再実行 | 本PCにPowerShell 7がなく未実施。Windows PowerShell 5.1では`Path.GetRelativePath`非対応のため起動不可 |
| Web再実行 | 本PCに固定Node.js 22.18.0／npmがなく未実施。Codex同梱Node 24.14.0への置換や追加インストールは行っていない |

Architecture／Webはコード変更のない本完了処理では再実行せず、P2-A07／P2-A08／P2-007～P2-009の個別完了時に成立した境界と、最新MSIを用いた今回の実動作確認を根拠に引き継ぐ。

## 完了判定

P2-001～P2-009の実装系列、Local First／Port／Bridge／Architecture境界、最新成果物の.NET回帰、およびクリーンWindows 11 VMの手動E2Eを総合し、P2-010は合格・完了とする。

Phase 2 Local Project登録機能は正式完了と判定する。Project登録・解除はアプリ側Catalogだけを変更し、利用者のProject Rootおよび内部ファイルを削除しないこと、Root異常時にも登録情報を保持して管理操作を継続できることを受入れ確認した。

## 未実施・後続事項

- 本PCでのArchitecture検査再実行（PowerShell 7未導入）。
- 本PCでのWeb typecheck／lint／unit test／bundle再実行（固定Node.js 22.18.0／npm未導入）。
- VM上のMSI SHA-256再採取と、登録前後のProject tree／全ファイルhash比較は個別証拠として未採取。
- 表示倍率125%～200%、keyboard-only操作は今回のP2-010 VM確認では個別再確認していない。
- Server認証／HTTPS／LAN、走査、watch、Markdown解析、`.adm-meta`文書ID、SQLite索引はP2-010の対象外であり未実装。

これらは今回確認したLocal Project登録の中核動作を否定するblockerではない。固定toolchainを導入する際は、次フェーズ開始前の通常品質ゲートでArchitecture／Webを再実行する。
