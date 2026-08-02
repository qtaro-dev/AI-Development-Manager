# P1-021 WPFブリッジ許可境界

## 目的

Web UIとWPF間のメッセージ契約をAllowlist化し、Windows固有操作だけを安全に公開する基盤を作る。

## 背景

WPFブリッジはファイル／フォルダー選択、Explorer、OS通知、Server制御、アプリ設定に限定し、Markdown・添付・状態更新をAPI経由に統一する方針が確定している。

## 前提・依存関係

- P1-020完了
- ADR-010
- P0-011パス安全性方針

## 対象範囲

- WebMessageの版付きEnvelope
- 操作名Allowlistと引数検証
- request/response/cancel/error
- origin検証と追跡ID
- Phase 1で安全に確認できる非業務サンプル操作

## 対象外

- Markdown、添付、状態、テスト結果の読取・更新
- Explorer起動等の業務対象実装
- 任意PowerShell・任意パス操作

## 対象ファイルまたは対象モジュール

- `src/Adm.Wpf/Bridge`
- `src/Adm.Web/src/platform-bridge`
- Bridge契約テスト

## 具体的な実装内容

1. 版、操作、request ID、payloadを持つ契約を定義する。
2. 許可操作を列挙し、未知操作・未知項目を拒否する。
3. Web UIのoriginとtop-level frameを検証する。
4. payloadの型・長さ・文字列を検証する。
5. Phase 1では`getHostInfo`等の非業務サンプルだけを実装する。
6. 業務データ操作を禁止するArchitecture/contract testを追加する。

## テスト内容

- 正常request/response
- 未知操作、版不一致、不正payload拒否
- 許可外origin/frame拒否
- timeout/cancel
- 追跡ID付きエラー
- 業務API名がBridgeへ追加されていないこと

## 完了条件

- Allowlist外の操作を実行できない。
- Bridge契約が版管理され、Web/WPF双方で型検証される。
- 業務データの読取・更新操作が存在しない。
- 将来Windows操作を一件ずつ追加・レビューできる。

## ユーザーが目視確認する内容

- WebView2内でのHost情報表示例
- 許可外メッセージ拒否結果
- Bridge許可操作一覧

## 想定されるリスク

- 任意文字列を操作名やパスとして実行する
- origin検証を開発時だけ無効化する
- BridgeがAPIの抜け道になる

## 完了後に更新すべき設計資料

- WPF Bridge契約
- ADR-010
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

## 実装結果

version 1のWebMessage Envelopeと`getHostInfo`のみの許可リストをWPF/Web双方へ実装した。WPFはServer originとSourceを検証し、未知フィールド、未知操作、不正payload、異常な要求IDを拒否する。Web UIには許可操作一覧、Host情報確認、通常ブラウザでの非対応状態を表示する。任意コード、任意コマンド、自由なファイルアクセス、業務データ操作は実装していない。
