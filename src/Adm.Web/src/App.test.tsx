import { screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { App } from "./App";
import type { DataAccessPort } from "./data-access";
import { renderWithProviders } from "./test/test-utils";

const fakeDataAccess: DataAccessPort = {
    getFoundationStatus: vi.fn(),
    getExecutionProfile: vi.fn(),
    updateExecutionProfile: vi.fn(),
};

describe("App foundation", () => {
    it("exposes the product identity and runtime API boundary", () => {
        renderWithProviders(
            <App dataAccess={fakeDataAccess} apiBoundary="/api/v1" />,
        );

        expect(
            screen.getByRole("heading", { name: "AI Development Manager" }),
        ).toBeVisible();
        expect(screen.getByText("/api/v1")).toBeVisible();
        expect(screen.getByText("基盤準備完了")).toBeVisible();
    });
});
