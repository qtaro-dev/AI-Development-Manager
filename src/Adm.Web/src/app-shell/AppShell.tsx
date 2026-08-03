import type { ReactNode } from "react";
import { message } from "../messages/catalog";

type NavItem = {
    key: string;
    label: string;
    icon: string;
    active?: boolean;
};

const navItems: NavItem[] = [
    {
        key: "tickets",
        label: message("shell.navTickets"),
        icon: "▦",
        active: true,
    },
    { key: "test-cases", label: message("shell.navTestCases"), icon: "✓" },
    { key: "search", label: message("shell.navSearch"), icon: "⌕" },
    { key: "knowledge", label: message("shell.navKnowledge"), icon: "◫" },
];

export function AppShell({
    children,
    pageTitle,
    onSettings,
}: {
    children: ReactNode;
    pageTitle: string;
    onSettings?: () => void;
}) {
    return (
        <div className="app-shell">
            <a className="skip-link" href="#main-content">
                {message("shell.skipToContent")}
            </a>
            <aside
                className="app-sidebar"
                aria-label={message("shell.navigation")}
            >
                <div className="brand-mark" aria-hidden="true">
                    A
                </div>
                <div className="brand-copy">
                    <strong>AI Development</strong>
                    <span>Manager</span>
                </div>

                <div className="workspace-picker">
                    <span className="sidebar-section-label">
                        {message("shell.workspace")}
                    </span>
                    <span className="workspace-name">
                        <span className="connection-dot" aria-hidden="true" />
                        Product Core
                    </span>
                </div>

                <nav
                    className="primary-navigation"
                    aria-label={message("shell.navigation")}
                >
                    {navItems.map((item) => (
                        <a
                            className={`nav-item${item.active ? " is-active" : ""}`}
                            href={`#${item.key}`}
                            key={item.key}
                            aria-current={item.active ? "page" : undefined}
                            aria-label={item.label}
                        >
                            <span className="nav-icon" aria-hidden="true">
                                {item.icon}
                            </span>
                            <span className="nav-label">{item.label}</span>
                        </a>
                    ))}
                </nav>

                <div className="sidebar-footer">
                    <div className="connection-status">
                        <span className="connection-dot" aria-hidden="true" />
                        <span className="connection-label">
                            {message("shell.connection")}
                        </span>
                        <span className="https-label">
                            {message("shell.https")}
                        </span>
                    </div>
                    <a
                        className="nav-item"
                        href="#settings"
                        aria-label={message("shell.settings")}
                        onClick={(event) => {
                            if (onSettings) {
                                event.preventDefault();
                                onSettings();
                            }
                        }}
                    >
                        <span className="nav-icon" aria-hidden="true">
                            ⚙
                        </span>
                        <span className="nav-label">
                            {message("shell.settings")}
                        </span>
                    </a>
                    <div className="user-summary">
                        <span className="user-avatar" aria-hidden="true">
                            QT
                        </span>
                        <span className="user-copy">
                            <strong>qtaro</strong>
                            <span>{message("shell.roleAdministrator")}</span>
                        </span>
                    </div>
                </div>
            </aside>

            <header className="app-topbar">
                <div className="breadcrumbs" aria-label="breadcrumb">
                    <span>{message("shell.breadcrumbWorkspace")}</span>
                    <span aria-hidden="true">
                        {message("shell.breadcrumbSeparator")}
                    </span>
                    <strong>{pageTitle}</strong>
                </div>
                <div
                    className="topbar-reserved"
                    aria-label={message("shell.primaryActions")}
                />
            </header>

            <main className="app-main" id="main-content" tabIndex={-1}>
                <div className="page-heading">
                    <div>
                        <p className="eyebrow">DOCUMENTS / TICKETS</p>
                        <h1>{pageTitle}</h1>
                        <p className="page-description">
                            {message("shell.pageDescription")}
                        </p>
                    </div>
                    <div
                        className="page-actions"
                        aria-label={message("shell.primaryActions")}
                    />
                </div>
                {children}
            </main>
        </div>
    );
}
