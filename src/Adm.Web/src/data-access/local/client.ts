import {
    LocalChannelProtocolError,
    parseMessage,
    serializeRequest,
    type LocalChannelPayload,
} from "./protocol";

export interface LocalChannelTransport {
    postMessage(message: string): void;
    subscribe(listener: (message: string) => void): () => void;
    dispose?: () => void;
}

export type LocalChannelRequestOptions = {
    readonly timeoutMs?: number;
    readonly signal?: AbortSignal;
};

export const DEFAULT_LOCAL_CHANNEL_TIMEOUT_MS = 30_000;

type PendingRequest = {
    readonly resolve: (value: LocalChannelPayload) => void;
    readonly reject: (reason: unknown) => void;
    readonly timeout: ReturnType<typeof setTimeout>;
    readonly signal?: AbortSignal;
    readonly abort: (() => void) | undefined;
};

export class LocalChannelClient {
    private readonly pending = new Map<string, PendingRequest>();
    private readonly unsubscribe: () => void;
    private disposed = false;

    constructor(private readonly transport: LocalChannelTransport) {
        this.unsubscribe = transport.subscribe((message) =>
            this.handleMessage(message),
        );
    }

    request(
        operation: string,
        payload: LocalChannelPayload = null,
        options: LocalChannelRequestOptions = {},
    ): Promise<LocalChannelPayload> {
        if (this.disposed) {
            return Promise.reject(this.channelUnavailable());
        }

        const requestId = `request-${crypto.randomUUID()}`;
        const message = serializeRequest(requestId, operation, payload);
        const timeoutMs = options.timeoutMs ?? DEFAULT_LOCAL_CHANNEL_TIMEOUT_MS;
        if (!Number.isFinite(timeoutMs) || timeoutMs <= 0) {
            return Promise.reject(
                new LocalChannelProtocolError(
                    "invalid_request",
                    "Local Channel timeout is invalid.",
                    requestId,
                ),
            );
        }

        return new Promise((resolve, reject) => {
            const abort = options.signal
                ? () => this.finishWithError(
                      requestId,
                      new LocalChannelProtocolError(
                          "cancelled",
                          "Local Channel request was cancelled.",
                          requestId,
                      ),
                  )
                : undefined;
            const timeout = setTimeout(
                () =>
                    this.finishWithError(
                        requestId,
                        new LocalChannelProtocolError(
                            "timeout",
                            "Local Channel request timed out.",
                            requestId,
                        ),
                    ),
                timeoutMs,
            );
            this.pending.set(requestId, {
                resolve,
                reject,
                timeout,
                signal: options.signal,
                abort,
            });
            if (options.signal?.aborted) {
                abort?.();
                return;
            }
            options.signal?.addEventListener("abort", abort!, { once: true });
            try {
                this.transport.postMessage(message);
            } catch {
                this.finishWithError(requestId, this.channelUnavailable());
            }
        });
    }

    dispose(): void {
        if (this.disposed) return;
        this.disposed = true;
        this.unsubscribe();
        for (const requestId of this.pending.keys()) {
            this.finishWithError(requestId, this.channelUnavailable());
        }
        this.transport.dispose?.();
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

        this.clearPending(message.requestId);
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

    private finishWithError(
        requestId: string,
        error: LocalChannelProtocolError,
    ): void {
        const pending = this.pending.get(requestId);
        if (!pending) return;
        this.clearPending(requestId);
        pending.reject(error);
    }

    private clearPending(requestId: string): PendingRequest | undefined {
        const pending = this.pending.get(requestId);
        if (!pending) return undefined;
        this.pending.delete(requestId);
        clearTimeout(pending.timeout);
        if (pending.signal && pending.abort) {
            pending.signal.removeEventListener("abort", pending.abort);
        }
        return pending;
    }

    private channelUnavailable(): LocalChannelProtocolError {
        return new LocalChannelProtocolError(
            "channel_unavailable",
            "Local Channel is unavailable.",
        );
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
