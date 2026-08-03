# P1-027 Phase 1統合ゲート結果

## 1. 判定

P1-027の自動検証と非管理者セッションで可能な統合スモークは合格した。一方、per-machine Server MSIのUACを伴うinstall／update／repair／uninstallとWindows Service実機ライフサイクル、およびClient MSIの実インストール後ライフサイクルを完了証拠として取得できなかったため、Phase 1は正式完了保留とする。

未確認事項を成功扱いにせず、P1-025／P1-026の実機確認待ちをPhase 1開始条件として維持する。

## 2. 環境

| 項目 | 実測値 |
|---|---|
| OS | Windows NT 10.0.26200.0 |
| Architecture | AMD64 |
| .NET SDK | 10.0.302 |
| Node.js | v22.18.0 |
| npm | 10.9.3 |
| Edge | 151.0.4129.59 |
| Chrome | 150.0.7871.187 |
| WebView2 Runtime | 150.0.4078.105 |
| 実行権限 | 非管理者 |

## 3. 統合結果

| 確認項目 | 結果 | 証拠・補足 |
|---|---|---|
| Debug／Release build | 合格 | `artifacts/ci-evidence/p1-027-quality-gates-final/` |
| Debug／Release .NET test | 合格、各43件 | Core 1、Application 1、Windows 21、Server 20 |
| Architecture検査 | 合格 | Debug／Release、5製品プロジェクト、意図的違反fixture含む |
| OpenAPI契約 | 合格 | 品質ゲート証拠 |
| NuGet脆弱性監査 | 合格 | High／Criticalなし |
| npm監査・静的検査・Web build | 合格 | npm audit脆弱性0、各検査成功 |
| SBOM・ライセンス・禁止追跡ファイル | 合格 | 品質ゲート証拠 |
| Playwright E2E | 合格、9件 | console／HTTP重大エラーなし |
| Edge／Chrome互換・DPI相当 | 合格、24件 | 100／125／150／200%をdeviceScaleFactorで確認 |
| WebView2起動スモーク | 合格 | Runtime検出、WPFプロセス継続を確認 |
| Server console起動・停止 | 合格 | 127.0.0.1:5198、live／ready 200、停止確認 |
| localhost限定 | 合格 | ループバック200、非ループバック接続不可 |
| Server Service登録・開始・停止 | 未確認 | per-machine UACが必要。管理者セッションで実施要 |
| Server MSI install/update/repair/uninstall | 未完了 | 管理者UAC・Windows Installer実機確認待ち |
| Server Config／Logs／Data保持 | 未完了 | MSI実機install後の保持確認待ち |
| Client MSI install/update/repair/uninstall | 未完了 | 実インストール後のライフサイクル確認待ち |
| 標準ユーザー導入 | 未完了 | Client MSIの実機確認待ち |
| WebView2 Runtimeあり／なしのMSI分岐 | パッケージ検証合格、実機未確認 | HKLM／HKCU検査と日本語LaunchConditionをMSIへ含むことを検証 |
| 日本語IME | 未確認 | P1-027実機確認待ち |
| OS表示倍率100～200% | 相当条件合格、実OS未確認 | Playwright deviceScaleFactor。OS設定変更は未実施 |
| Installer停止原因 | 原因確定せず | 非管理者・UAC待ちでmsiexecが完了せず、成功扱いにしていない |

## 4. P1-027で行った検査修正

- Architecture検査が複数PropertyGroupを誤ってTargetFrameworkとして扱う不具合を修正した。
- UI互換検査がWebView2レジストリの任意キーに`name`／`pv`がない場合に停止する不具合を修正した。
- `src/Adm.Web/index.html`をPrettier標準へ整形し、品質ゲートを成立させた。

いずれもP1-027の検査基盤修正であり、Phase 2機能や業務製品機能は追加していない。

## 5. Phase 2引継ぎ

Phase 2の認証、HTTPS、権限、LAN公開、プロジェクト登録は未実装であり、Phase 1のlocalhost限定・認証前LAN非公開境界を維持する。Phase 2開始前に次を完了条件とする。

1. 管理者セッションまたはクリーンWindows 11 VMでServer MSIのinstall／update／repair／uninstallとService状態を取得する。
2. 同じ環境でConfig／Logs／Dataの保持、失敗時復元、Installerログを取得する。
3. 標準ユーザーでClient MSIのinstall／update／repair／uninstall、Runtimeあり／なし、UserData保持を取得する。
4. 実OS DPI 100～200%、日本語IME、WPF内WebView2操作を確認する。

## 6. 証拠保存先

- 品質ゲート: `artifacts/ci-evidence/p1-027-quality-gates-final/`
- UI互換: `artifacts/ci-evidence/p1-027-ui-runtime-compatibility/`
- Server console／MSI検証: `artifacts/p1-027/`

`artifacts/`配下は生成証拠であり、Git管理対象へ追加しない。

## P1-028による判定境界の更新

P1-028 ADR-019により、Phase 1のWindowsアプリ単体Local modeと、任意導入Server modeの完了条件を分離する。P1-025／P1-027のInstaller・Service実機未完了は履歴として残し、Local modeの設計判断を妨げない。ただし、Local Application Channel、初回導線、実際のローカル業務機能は未実装であり、本ADRだけでPhase 1全体を正式完了扱いにしない。
