import { StrictMode, useEffect, useMemo, useState } from 'react'
import { createRoot } from 'react-dom/client'
import { mockApi, type DocumentItem, type TestRow } from './mockApi'
import './styles.css'

type Theme = 'light' | 'dark'

function App() {
  const [documents, setDocuments] = useState<DocumentItem[]>([])
  const [selectedId, setSelectedId] = useState('ADM-024')
  const [theme, setTheme] = useState<Theme>('light')
  const [query, setQuery] = useState('')
  const [rows, setRows] = useState<TestRow[]>([])
  const [saveState, setSaveState] = useState<'saved' | 'dirty' | 'conflict'>('saved')
  const [conflictOpen, setConflictOpen] = useState(false)
  const [toast, setToast] = useState('')
  const [menuOpen, setMenuOpen] = useState(false)
  const selected = documents.find((document) => document.id === selectedId) ?? documents[0]

  useEffect(() => {
    setDocuments(mockApi.listDocuments())
    setRows(mockApi.getTestRows())
  }, [])

  useEffect(() => {
    document.documentElement.dataset.theme = theme
  }, [theme])

  useEffect(() => {
    const save = (event: KeyboardEvent) => {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 's') {
        event.preventDefault()
        saveDocument()
      }
      if (event.key === 'Escape') setConflictOpen(false)
    }
    window.addEventListener('keydown', save)
    return () => window.removeEventListener('keydown', save)
  })

  const filteredDocuments = useMemo(() => documents.filter((document) => `${document.title} ${document.id} ${document.type}`.toLowerCase().includes(query.toLowerCase())), [documents, query])

  function updateRow(index: number, value: string) {
    setRows((current) => current.map((row, rowIndex) => rowIndex === index ? { ...row, actual: value } : row))
    setSaveState('dirty')
  }

  function saveDocument() {
    if (saveState === 'dirty') {
      setSaveState('saved')
      setToast('変更を保存しました')
      window.setTimeout(() => setToast(''), 2400)
    }
  }

  function openConflict() {
    setSaveState('conflict')
    setConflictOpen(true)
  }

  return <div className="app-shell">
    <aside className="sidebar" aria-label="メインナビゲーション">
      <div className="brand"><div className="brand-mark">A</div><div><strong>AI Development</strong><span>Manager</span></div></div>
      <div className="workspace-label">ワークスペース</div>
      <button className="workspace-switcher"><span className="workspace-dot" /> Product Core <span className="chevron">⌄</span></button>
      <nav className="nav-group">
        <button className="nav-item active"><span>▦</span> チケット</button>
        <button className="nav-item"><span>✓</span> テストケース</button>
        <button className="nav-item"><span>⌕</span> 検索</button>
        <button className="nav-item"><span>◫</span> ナレッジ</button>
      </nav>
      <div className="sidebar-spacer" />
      <div className="connection"><span className="status-dot" /> Server接続中 <small>HTTPS</small></div>
      <button className="nav-item"><span>⚙</span> 設定</button>
      <div className="profile"><div className="avatar">QT</div><div><strong>qtaro</strong><span>管理者</span></div><button aria-label="アカウントメニュー" onClick={() => setMenuOpen(!menuOpen)}>•••</button></div>
      {menuOpen && <div className="profile-menu"><button onClick={() => setToast('ログアウトはモックです')}>ログアウト</button></div>}
    </aside>

    <main className="main-content">
      <header className="topbar"><div className="breadcrumbs"><span>Product Core</span><b>/</b><strong>チケット</strong></div><div className="top-actions"><button className="icon-button" onClick={() => setTheme(theme === 'light' ? 'dark' : 'light')} aria-label="テーマ切替">{theme === 'light' ? '☾' : '☀'}</button><button className="help-button">?</button><div className="avatar small">QT</div></div></header>
      <div className="content-wrap">
        <section className="page-heading"><div><div className="eyebrow">DOCUMENTS / TICKETS</div><h1>チケット</h1><p>開発の決定事項と進行状況をひとつの場所で管理します。</p></div><button className="primary-button" onClick={() => setToast('新規チケット作成はモックです')}><span>＋</span> 新規チケット</button></section>
        <div className="workspace-grid">
          <section className="list-panel panel">
            <div className="panel-heading"><div><h2>チケット一覧</h2><span className="muted">{documents.length} 件</span></div><button className="filter-button">フィルター <span>⌄</span></button></div>
            <label className="search-box"><span>⌕</span><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="タイトル・IDで検索" aria-label="チケット検索" /><kbd>/</kbd></label>
            <div className="list-tabs"><button className="tab active">すべて <b>{documents.length}</b></button><button className="tab">進行中 <b>3</b></button><button className="tab">完了 <b>1</b></button></div>
            <div className="document-list">{filteredDocuments.map((document) => <button className={`document-row ${selected?.id === document.id ? 'selected' : ''}`} key={document.id} onClick={() => { setSelectedId(document.id); setSaveState('saved') }}><div className="doc-type">{document.typeLabel}</div><div className="doc-main"><strong>{document.id}</strong><span>{document.title}</span><small>{document.updated}</small></div><span className={`status-badge ${document.status}`}>{document.statusLabel}</span></button>)}</div>
          </section>

          <section className="detail-panel panel">
            <div className="detail-top"><div><div className="detail-id"><span className="ticket-icon">◆</span> {selected?.id} <span className="status-badge in-progress">進行中</span></div><h2>{selected?.title}</h2><div className="meta-line"><span>最終更新 {selected?.updated}</span><span>担当 <b>qtaro</b></span><span>ETag <code>"9f82c1…"</code></span></div></div><button className="more-button" aria-label="その他の操作">•••</button></div>
            <div className="detail-content"><div className="description"><h3>概要</h3><p>Web UI技術の選定に向けて、主要な操作と表示状態を同一のモックAPI契約で確認する。認証、保存状態、競合時の案内を、通常ブラウザとWebView2で同じ意味に保つ。</p></div><div className="test-section"><div className="section-heading"><div><h3>テスト結果</h3><span className="muted">キーボードでセルを移動できます</span></div><span className={`save-state ${saveState}`}><i />{saveState === 'saved' ? '保存済み' : saveState === 'dirty' ? '未保存の変更' : '競合を確認'}</span></div><div className="table-wrap"><table><thead><tr><th>項目</th><th>期待結果</th><th>実際の結果</th><th>判定</th></tr></thead><tbody>{rows.map((row, index) => <tr key={row.item}><td><strong>{row.item}</strong><small>{row.label}</small></td><td>{row.expected}</td><td><input value={row.actual} onChange={(event) => updateRow(index, event.target.value)} aria-label={`${row.item} 実際の結果`} /></td><td><button className={`result-chip ${row.result}`} onClick={() => setRows((current) => current.map((item, rowIndex) => rowIndex === index ? { ...item, result: item.result === 'pass' ? 'fail' : 'pass' } : item))}>{row.result === 'pass' ? '✓ 合格' : '× 要確認'}</button></td></tr>)}</tbody></table></div></div></div>
            <div className="detail-footer"><div className="attachment"><span>↗</span><div><strong>poc-result.json</strong><small>12 KB · 添付ファイル</small></div></div><div className="footer-actions"><button className="secondary-button" onClick={openConflict}>競合を確認</button><button className="primary-button" disabled={saveState !== 'dirty'} onClick={saveDocument}>保存 <kbd>Ctrl S</kbd></button></div></div>
          </section>
        </div>
      </div>
    </main>
    {conflictOpen && <div className="modal-backdrop" role="presentation"><div className="conflict-modal" role="dialog" aria-modal="true" aria-labelledby="conflict-title"><div className="modal-icon">!</div><div className="modal-header"><div><div className="eyebrow danger">保存できません</div><h2 id="conflict-title">他の変更と競合しました</h2></div><button className="close-button" onClick={() => setConflictOpen(false)} aria-label="閉じる">×</button></div><p className="modal-copy">このチケットは別の場所で更新されています。内容を確認してから、保存方法を選択してください。</p><div className="compare-grid"><div><span className="compare-label">最新版（Server）</span><div className="compare-card">認証フローの検証結果を更新しました。<mark>WebView2実機確認</mark>を追加。</div></div><div><span className="compare-label">あなたの変更</span><div className="compare-card">認証フローの検証結果を更新しました。<mark>Cookie属性確認</mark>を追加。</div></div></div><div className="diff-note"><span>＋</span><div><strong>差分を表示</strong><small>変更箇所を比較して手動で統合できます</small></div><span>›</span></div><div className="modal-actions"><button className="secondary-button" onClick={() => { setConflictOpen(false); setSaveState('saved') }}>最新版を使う</button><button className="primary-button" onClick={() => { setConflictOpen(false); setSaveState('saved'); setToast('あなたの変更を保存しました') }}>自分の変更を保存</button></div></div></div>}
    {toast && <div className="toast" role="status">✓ {toast}</div>}
  </div>
}

createRoot(document.getElementById('root')!).render(<StrictMode><App /></StrictMode>)
