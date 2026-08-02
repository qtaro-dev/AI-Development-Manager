export type DocumentItem = { id: string; title: string; type: string; typeLabel: string; updated: string; status: 'in-progress' | 'done'; statusLabel: string }
export type TestRow = { item: string; label: string; expected: string; actual: string; result: 'pass' | 'fail' }

export const mockApi = {
  listDocuments(): DocumentItem[] {
    return [
      { id: 'ADM-024', title: '共通Web UI・React採否PoC', type: 'design', typeLabel: 'DESIGN', updated: '今日 11:42', status: 'in-progress', statusLabel: '進行中' },
      { id: 'ADM-023', title: 'Phase 0設計確定ゲート', type: 'ticket', typeLabel: 'TICKET', updated: '昨日 16:20', status: 'in-progress', statusLabel: '進行中' },
      { id: 'ADM-022', title: 'DevTicketManager互換PoC', type: 'ticket', typeLabel: 'TICKET', updated: '7月31日', status: 'in-progress', statusLabel: '入力待ち' },
      { id: 'ADM-017', title: 'Cookie・APIトークン認証PoC', type: 'test', typeLabel: 'TEST', updated: '7月30日', status: 'done', statusLabel: '完了' },
    ]
  },
  getTestRows(): TestRow[] {
    return [
      { item: 'UI-001', label: '一覧と詳細', expected: '選択したチケットの詳細が表示される', actual: '一覧から詳細へ遷移できる', result: 'pass' },
      { item: 'UI-002', label: '保存状態', expected: '未保存・保存済みを明示する', actual: 'ラベルと色で表示', result: 'pass' },
      { item: 'UI-003', label: '競合ダイアログ', expected: '最新版と自分の変更を比較できる', actual: '競合内容を表示', result: 'pass' },
      { item: 'UI-004', label: '日本語入力', expected: 'IME入力とTab移動を妨げない', actual: 'キーボード操作を確認', result: 'pass' },
    ]
  },
}
