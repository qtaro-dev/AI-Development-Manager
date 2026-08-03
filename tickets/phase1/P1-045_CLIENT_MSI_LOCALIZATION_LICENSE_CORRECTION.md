# P1-045 Client MSI日本語化・正式License差し替え

## 目的

P1-041のP1-043修正版クリーンVM再試験で確認された、Client MSIの日本語化不足を是正する。正式なLicense本文は未決定のため、本チケットでは変更せず、正式公開時の差し替え事項として記録する。

## 対象

- MSIの製品名、ダイアログ、ボタン、エラー案内の日本語化
- 利用者向けインストール・修復・削除文言の統一
- License本文の正式公開時差し替え方針の記録（本文の変更は対象外）
- MSI、必要なローカライズリソース、配布記録の整合性確認

## 対象外

- Server MSI、Windows Service、LAN接続
- Clientの業務機能・画面レイアウト
- P1-041試験結果の書き換え

## 完了条件

- Client MSIの利用者向け表示が日本語になる。
- install、repair、update、uninstallで使用される標準UIはWiXのja-JPローカライズを使用し、製品固有の案内も日本語になる。
- 正式なLicense本文は未決定であること、現行のダミー／既定本文を変更せず正式公開時に差し替えることをインストーラー記録へ明記する。
- Build、MSI build、ICE検査、License／SBOM／依存監査を実施する。
- 製品固有の旧英語案内が配布物へ残っていないことを確認する。正式License本文の欠如は未解決事項として扱い、今回の不具合とはしない。

## License本文の扱い

正式なLicense本文は未決定である。`WixUILicenseRtf`は今回指定せず、現行のダミー／WiX既定表示を維持する。正式公開時に承認済み本文へ差し替え、配布物と配布記録の一致を確認する。

## 状態

実装完了。正式License本文の確定・差し替えは正式公開時対応。

## 実装・検証結果

- `installer/wpf-client/Client.ja-JP.wxl`を追加し、WiX標準UIを`ja-JP`でビルドする設定を追加した。
- 製品固有のサマリー、機能説明、WebView2 Runtime不足案内、ダウングレード案内をLocalization文字列経由に統一した。
- MSIの`ProductLanguage=1041`および標準ダイアログ・ボタンの日本語表示をMSIテーブルで確認した。
- MSI出力: `artifacts/packages/client/ja-JP/AI-Development-Manager-Client-0.1.0-1-x64.msi`
- Web資産のPublish成果物・WiX Fragment・MSI File table一致検証に合格した。WiXビルドは0警告・0エラーで完了した。
- License本文は変更していない。正式公開時に承認済み本文へ差し替える。
