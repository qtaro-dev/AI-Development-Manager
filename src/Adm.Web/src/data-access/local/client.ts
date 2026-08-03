import {
    LocalChannelProtocolError,
    parseMessage,
    serializeRequest,
    type LocalChannelPayload,
} from "./protocol";

export interface LocalChannelTransport {
    postMessage(message: string): void;
    subscribe(listener: (message: string) => void): () => void;
}

type PendingRequest = {
    readonly resolve: (value: LocalChannelPayload) => void;
    readonly reject: (reason: unknown) => void;
};

export class LocalChannelClient {
    private readonly pending = new Map<string, PendingRequest>();
    private readonly unsubscribe: () => void;

    constructor(private readonly transport: LocalChannelTransport) {
        this.unsubscribe = transport.subscribe((message) =>
            this.handleMessage(message),
        );
    }

    request(
        operation: string,
        payload: LocalChannelPayload = null,
    ): Promise<LocalChannelPayload> {
        const requestId = `request-${crypto.randomUUID()}`;
        const message = serializeRequest(requestId, operation, payload);

        return new Promise((resolve, reject) => {
            this.pending.set(requestId, { resolve, reject });
            try {
                this.transport.postMessage(message);
            } catch {
                this.pending.delete(requestId);
                reject(
                    new LocalChannelProtocolError(
                        "channel_unavailable",
                        "Local Channel is unavailable.",
                        requestId,
                    ),
                );
            }
        });
    }

    dispose(): void {
        this.unsubscribe();
        for (const [requestId, pending] of this.pending) {
            pending.reject(
                new LocalChannelProtocolError(
                    "channel_unavailable",
                    "Local Channel is unavailable.",
                    requestId,
                ),
            );
        }
        this.pending.clear();
    }

    private handleMessage(raw: string): void {
        let message;
        try {
            message = parseMessage(raw);
        } catch {
            return;
        }

        const pending = this.pending.get(message.requestId);
        if (!pending) return;

        this.pending.delete(message.requestId);
        if (message.kind === "response") {
            pending.resolve(message.result);
        } else {
            pending.reject(
                new LocalChannelProtocolError(
                    message.error.code,
                    message.error.messageKey,
                    message.requestId,
                ),
            );
        }
    }
}

type LocalWebView2Bridge = {
    postMessage(message: string): void;
    addEventListener(
        type: "message",
        listener: (event: MessageEvent<string>) => void,
    ): void;
    removeEventListener(
        type: "message",
        listener: (event: MessageEvent<string>) => void,
    ): void;
};

type LocalWebView2Window = Window & {
    readonly chrome?: {
        readonly webview?: LocalWebView2Bridge;
    };
};

export function createWebView2LocalTransport(
    hostWindow: Window = window,
): LocalChannelTransport {
    const webview = (hostWindow as LocalWebView2Window).chrome?.webview;
    if (!webview) {
        throw new LocalChannelProtocolError(
            "channel_unavailable",
            "Local Channel is unavailable.",
        );
    }

    return {
        postMessage: (message) => webview.postMessage(message),
        subscribe: (listener) => {
            const handler = (event: MessageEvent<string>) =>
                listener(event.data);
            webview.addEventListener("message", handler);
            return () => webview.removeEventListener("message", handler);
        },
    };
}
