import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { AppShell } from "./AppShell";
import { renderWithProviders } from "../test/test-utils";

describe("AppShell", () => {
    it("provides skip navigation, labeled navigation, and a route outlet", async () => {
        const user = userEvent.setup();
        renderWithProviders(
            <AppShell pageTitle="長いページ名を含む確認画面">
                <p>本文領域</p>
            </AppShell>,
        );

        await user.tab();
        expect(screen.getByRole("link", { name: "本文へ移動" })).toHaveFocus();
        expect(screen.getByRole("main")).toContainElement(
            screen.getByText("本文領域"),
        );
        expect(screen.getByRole("link", { name: "チケット" })).toHaveAttribute(
            "aria-current",
            "page",
        );
        expect(screen.getByRole("link", { name: "設定" })).toBeVisible();
    });
});
