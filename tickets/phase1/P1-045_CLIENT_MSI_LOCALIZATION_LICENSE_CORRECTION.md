# P1-045 Client MSI日本語化・正式License差し替え

## 目的

P1-041のP1-043修正版クリーンVM再試験で確認された、Client MSIの日本語化不足および正式なLicense内容への差し替えを行う。

## 対象

- MSIの製品名、ダイアログ、ボタン、エラー案内の日本語化
- 利用者向けインストール・修復・削除文言の統一
- 正式なLicense内容への差し替え
- MSI、必要なローカライズリソース、配布記録の整合性確認

## 対象外

- Server MSI、Windows Service、LAN接続
- Clientの業務機能・画面レイアウト
- P1-041試験結果の書き換え

## 完了条件

- Client MSIの利用者向け表示が正式な日本語文言になる。
- 正式承認済みLicense内容がインストーラーおよび配布記録へ反映される。
- install、repair、update、uninstallの表示をWindows実機で確認する。
- Build、MSI build、ICE検査、License／SBOM／依存監査を実施する。
- 旧文言や仮Licenseが配布物へ残っていないことを確認する。

## 実施時期・優先順位

Phase 2開始時に判断する。本チケットでは実装しない。

## 状態

作成済み。未着手。
