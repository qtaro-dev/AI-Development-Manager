import type { ReactNode } from "react";
import { AppShell } from "../app-shell/AppShell";

export function RouteOutlet({
    children,
    pageTitle,
    onSettings,
}: {
    children: ReactNode;
    pageTitle: string;
    onSettings?: () => void;
}) {
    return (
        <AppShell pageTitle={pageTitle} onSettings={onSettings}>
            {children}
        </AppShell>
    );
}
