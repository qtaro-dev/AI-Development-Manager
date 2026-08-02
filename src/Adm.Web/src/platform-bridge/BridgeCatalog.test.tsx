import { screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { BridgeCatalog } from "./BridgeCatalog";
import { renderWithProviders } from "../test/test-utils";

vi.mock("./bridge", () => ({
    isHostBridgeAvailable: () => false,
    getHostInfo: vi.fn(),
    BridgeError: class BridgeError extends Error {},
}));

describe("BridgeCatalog", () => {
    it("shows only the non-business allowlisted operation in a normal browser", () => {
        renderWithProviders(<BridgeCatalog />);

        expect(
            screen.getByRole("heading", { name: "WPF Bridge許可操作" }),
        ).toBeVisible();
        expect(screen.getByText("Host情報の取得（getHostInfo）")).toBeVisible();
        expect(screen.getByText(/通常のブラウザではHost Bridge/)).toBeVisible();
        expect(
            screen.queryByText(/readFile|writeFile|execute|command/i),
        ).not.toBeInTheDocument();
    });
});
