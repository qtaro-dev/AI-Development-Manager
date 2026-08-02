import { afterEach, describe, expect, it, vi } from "vitest";
import {
    cancelHostRequest,
    getHostInfo,
    isHostBridgeAvailable,
} from "./bridge";

function installBridge() {
    const listeners: Array<(event: MessageEvent) => void> = [];
    const webview = {
        postMessage: vi.fn((request: { requestId: string }) => {
            queueMicrotask(() =>
                listeners.forEach((listener) =>
                    listener(
                        new MessageEvent("message", {
                            data: {
                                version: "1",
                                messageType: "response",
                                operation: "getHostInfo",
                                requestId: request.requestId,
                                status: "ok",
                                payload: {
                                    applicationName: "AI Development Manager",
                                    bridgeVersion: "1",
                                    runtime: "WebView2",
                                },
                            },
                        }),
                    ),
                ),
            );
        }),
        addEventListener: vi.fn(
            (_type: "message", listener: (event: MessageEvent) => void) =>
                listeners.push(listener),
        ),
        removeEventListener: vi.fn(),
    };
    window.chrome = { webview };
    return webview;
}

afterEach(() => {
    delete window.chrome;
    vi.useRealTimers();
});

describe("host bridge", () => {
    it("returns host information through the versioned request", async () => {
        const webview = installBridge();

        await expect(getHostInfo()).resolves.toMatchObject({
            runtime: "WebView2",
        });
        expect(webview.postMessage).toHaveBeenCalledWith(
            expect.objectContaining({
                version: "1",
                operation: "getHostInfo",
                payload: {},
            }),
        );
    });

    it("reports browser unavailability and supports cancellation", async () => {
        delete window.chrome;
        expect(isHostBridgeAvailable()).toBe(false);
        await expect(getHostInfo()).rejects.toMatchObject({
            code: "bridge_unavailable",
        });

        const webview = installBridge();
        cancelHostRequest("adm-test");
        expect(webview.postMessage).toHaveBeenCalledWith(
            expect.objectContaining({
                messageType: "cancel",
                requestId: "adm-test",
            }),
        );
    });

    it("times out and sends cancellation", async () => {
        vi.useFakeTimers();
        const webview = installBridge();
        webview.postMessage.mockImplementation(() => undefined);
        const pending = getHostInfo(10).then(
            () => null,
            (error) => error,
        );
        await vi.advanceTimersByTimeAsync(10);
        await expect(pending).resolves.toMatchObject({
            code: "bridge_timeout",
        });
        expect(webview.postMessage).toHaveBeenLastCalledWith(
            expect.objectContaining({ messageType: "cancel" }),
        );
    });
});
