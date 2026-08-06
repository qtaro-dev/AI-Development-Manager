import { afterEach, describe, expect, it, vi } from "vitest";
import {
    cancelHostRequest,
    getHostInfo,
    isHostBridgeAvailable,
    selectProjectFolder,
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
        emit(data: unknown) {
            listeners.forEach((listener) =>
                listener(new MessageEvent("message", { data })),
            );
        },
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

    it("rejects malformed responses without exposing their contents", async () => {
        const webview = installBridge();
        const pending = getHostInfo();
        const id = webview.postMessage.mock.calls[0][0].requestId;

        webview.emit({
            version: "1",
            messageType: "response",
            operation: "getHostInfo",
            requestId: id,
            status: "ok",
            payload: { applicationName: "secret", bridgeVersion: 1 },
        });

        await expect(pending).rejects.toMatchObject({ code: "bridge_error" });
    });

    it("selects a project folder and supports cancellation", async () => {
        const webview = installBridge();
        webview.postMessage.mockImplementation((request: { requestId: string; operation?: string }) => {
            queueMicrotask(() => webview.emit({
                version: "1",
                messageType: "response",
                operation: request.operation ?? "selectProjectFolder",
                requestId: request.requestId,
                status: "ok",
                payload: { selected: true, path: "C:\\Projects\\Demo" },
            }));
        });

        await expect(selectProjectFolder()).resolves.toEqual({ selected: true, path: "C:\\Projects\\Demo" });
        expect(webview.postMessage).toHaveBeenCalledWith(expect.objectContaining({ operation: "selectProjectFolder", payload: {} }));
    });

    it("returns a stable unselected result when the user cancels", async () => {
        const webview = installBridge();
        webview.postMessage.mockImplementation((request: { requestId: string; operation?: string }) => {
            queueMicrotask(() => webview.emit({
                version: "1",
                messageType: "response",
                operation: request.operation ?? "selectProjectFolder",
                requestId: request.requestId,
                status: "cancelled",
            }));
        });

        await expect(selectProjectFolder()).resolves.toEqual({ selected: false });
    });
});
