import type { ReactNode } from "react";
import { AppShell } from "../app-shell/AppShell";

export function RouteOutlet({
    children,
    pageTitle,
}: {
    children: ReactNode;
    pageTitle: string;
}) {
    return <AppShell pageTitle={pageTitle}>{children}</AppShell>;
}
