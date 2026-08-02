import type { PropsWithChildren, ReactElement } from "react";
import { render, type RenderOptions } from "@testing-library/react";
import { ThemeProvider } from "../theme/theme";

function TestProviders({ children }: PropsWithChildren) {
    return <ThemeProvider initialMode="light">{children}</ThemeProvider>;
}

export function renderWithProviders(
    ui: ReactElement,
    options?: Omit<RenderOptions, "wrapper">,
) {
    return render(ui, { wrapper: TestProviders, ...options });
}

export * from "@testing-library/react";
export { default as userEvent } from "@testing-library/user-event";
