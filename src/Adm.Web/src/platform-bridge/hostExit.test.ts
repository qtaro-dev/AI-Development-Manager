import { describe, expect, it, vi } from "vitest";
import { requestHostExit } from "./hostExit";

describe("requestHostExit", () => {
    it("sends the exact string exit message to the embedded WebView host", () => {
        const postMessage = vi.fn();
        Object.defineProperty(window, "chrome", {
            configurable: true,
            value: { webview: { postMessage } },
        });

        requestHostExit();

        expect(postMessage).toHaveBeenCalledOnce();
        expect(postMessage).toHaveBeenCalledWith("exit");
    });

    it("falls back to window.close outside the embedded host", () => {
        Object.defineProperty(window, "chrome", {
            configurable: true,
            value: undefined,
        });
        const close = vi.spyOn(window, "close").mockImplementation(() => {});

        requestHostExit();

        expect(close).toHaveBeenCalledOnce();
        close.mockRestore();
    });
});
