import { screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { App } from "./App";
import { renderWithProviders } from "./test/test-utils";

describe("App foundation", () => {
    it("exposes the product identity and runtime API boundary", () => {
        renderWithProviders(<App />);

        expect(
            screen.getByRole("heading", { name: "AI Development Manager" }),
        ).toBeVisible();
        expect(screen.getByText("/api/v1")).toBeVisible();
        expect(screen.getByText("基盤準備完了")).toBeVisible();
    });
});
