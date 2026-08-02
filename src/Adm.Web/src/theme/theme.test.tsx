import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ThemeProvider, useTheme } from "./theme";

function ThemeProbe() {
    const { mode, resolvedTheme, setMode } = useTheme();
    return (
        <div>
            <output aria-label="theme-mode">{mode}</output>
            <output aria-label="resolved-theme">{resolvedTheme}</output>
            <button onClick={() => setMode("dark")}>dark</button>
        </div>
    );
}

describe("ThemeProvider", () => {
    beforeEach(() => {
        window.localStorage.clear();
    });

    it("applies the selected light or dark theme to the document root", async () => {
        const user = userEvent.setup();
        render(
            <ThemeProvider initialMode="light">
                <ThemeProbe />
            </ThemeProvider>,
        );

        expect(document.documentElement.dataset.theme).toBe("light");
        expect(screen.getByLabelText("resolved-theme")).toHaveTextContent(
            "light",
        );

        await user.click(screen.getByRole("button", { name: "dark" }));
        expect(document.documentElement.dataset.theme).toBe("dark");
        expect(screen.getByLabelText("resolved-theme")).toHaveTextContent(
            "dark",
        );
    });

    it("follows the operating system when system mode is selected", () => {
        window.localStorage.setItem("adm.theme", "system");
        Object.defineProperty(window, "matchMedia", {
            configurable: true,
            value: vi.fn(() => ({
                matches: true,
                media: "(prefers-color-scheme: dark)",
                onchange: null,
                addEventListener: vi.fn(),
                removeEventListener: vi.fn(),
                addListener: vi.fn(),
                removeListener: vi.fn(),
                dispatchEvent: vi.fn(),
            })),
        });

        render(
            <ThemeProvider>
                <ThemeProbe />
            </ThemeProvider>,
        );

        expect(screen.getByLabelText("theme-mode")).toHaveTextContent("system");
        expect(document.documentElement.dataset.theme).toBe("dark");
    });
});
