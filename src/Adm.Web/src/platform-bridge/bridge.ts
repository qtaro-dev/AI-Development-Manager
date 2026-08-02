export type HostInfo = {
    applicationName: string;
    bridgeVersion: string;
    runtime: string;
};

export type BridgeErrorCode =
    "bridge_unavailable" | "bridge_timeout" | "bridge_error";

export class BridgeError extends Error {
    constructor(
        public readonly code: BridgeErrorCode,
        message: string,
    ) {
        super(message);
    }
}

type BridgeResponse = {
    version: string;
    messageType: "response";
    operation: "getHostInfo";
    requestId: string;
    status: "ok" | "error" | "cancelled";
    payload?: HostInfo;
    error?: { code: string; message: string; traceId: string };
};

type WebViewBridge = {
    postMessage(message: unknown): void;
    addEventListener(
        type: "message",
        listener: (event: MessageEvent<BridgeResponse>) => void,
    ): void;
    removeEventListener(
        type: "message",
        listener: (event: MessageEvent<BridgeResponse>) => void,
    ): void;
};

declare global {
    interface Window {
        chrome?: { webview?: WebViewBridge };
    }
}

const VERSION = "1";
const OPERATION = "getHostInfo";

function getWebViewBridge() {
    return window.chrome?.webview;
}

function requestId() {
    const uuid = globalThis.crypto?.randomUUID?.();
    return `adm-web-${uuid ?? `${Date.now()}-${Math.random().toString(16).slice(2)}`}`;
}

export function isHostBridgeAvailable() {
    return getWebViewBridge() !== undefined;
}

export function cancelHostRequest(id: string) {
    getWebViewBridge()?.postMessage({
        version: VERSION,
        messageType: "cancel",
        operation: OPERATION,
        requestId: id,
        payload: {},
    });
}

export function getHostInfo(timeoutMs = 3000): Promise<HostInfo> {
    const bridge = getWebViewBridge();
    if (!bridge)
        return Promise.reject(
            new BridgeError(
                "bridge_unavailable",
                "この画面ではHost Bridgeを利用できません。",
            ),
        );
    const id = requestId();
    return new Promise((resolve, reject) => {
        const finish = (callback: () => void) => {
            window.clearTimeout(timeout);
            bridge.removeEventListener("message", onMessage);
            callback();
        };
        const onMessage = (event: MessageEvent<BridgeResponse>) => {
            const response = event.data;
            if (response?.requestId !== id) return;
            if (
                response.version !== VERSION ||
                response.messageType !== "response" ||
                response.operation !== OPERATION
            ) {
                finish(() =>
                    reject(
                        new BridgeError(
                            "bridge_error",
                            "Host Bridgeから不正な応答を受け取りました。",
                        ),
                    ),
                );
                return;
            }
            if (response.status === "ok" && response.payload) {
                finish(() => resolve(response.payload!));
            } else {
                finish(() =>
                    reject(
                        new BridgeError(
                            "bridge_error",
                            response.error?.message ??
                                "Host Bridgeで処理できませんでした。",
                        ),
                    ),
                );
            }
        };
        const timeout = window.setTimeout(() => {
            finish(() => {
                cancelHostRequest(id);
                reject(
                    new BridgeError(
                        "bridge_timeout",
                        "Host Bridgeの応答がありません。",
                    ),
                );
            });
        }, timeoutMs);
        bridge.addEventListener("message", onMessage);
        bridge.postMessage({
            version: VERSION,
            messageType: "request",
            operation: OPERATION,
            requestId: id,
            payload: {},
        });
    });
}
