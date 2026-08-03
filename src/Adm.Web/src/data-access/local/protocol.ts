export const LOCAL_CHANNEL_VERSION = 1 as const;
export const MAX_LOCAL_CHANNEL_MESSAGE_BYTES = 1024 * 1024;
export const MAX_LOCAL_CHANNEL_JSON_DEPTH = 16;
const REQUEST_ID_PATTERN = /^[A-Za-z0-9_-]{1,64}$/;
const OPERATION_PATTERN = /^[A-Za-z][A-Za-z0-9]*(?:[._-][A-Za-z0-9]+)*$/;

export type LocalChannelPayload = Record<string, unknown> | null;

export type LocalChannelRequest = {
    readonly version: typeof LOCAL_CHANNEL_VERSION;
    readonly kind: "request";
    readonly requestId: string;
    readonly operation: string;
    readonly payload: LocalChannelPayload;
};

export type LocalChannelResponse = {
    readonly version: typeof LOCAL_CHANNEL_VERSION;
    readonly kind: "response";
    readonly requestId: string;
    readonly result: LocalChannelPayload;
};

export type LocalChannelError = {
    readonly version: typeof LOCAL_CHANNEL_VERSION;
    readonly kind: "error";
    readonly requestId: string;
    readonly error: {
        readonly code: string;
        readonly messageKey: string;
    };
};

export type LocalChannelMessage = LocalChannelResponse | LocalChannelError;

export class LocalChannelProtocolError extends Error {
    constructor(
        readonly code: string,
        message: string,
        readonly requestId: string | null = null,
    ) {
        super(message);
        this.name = "LocalChannelProtocolError";
    }
}

export function serializeRequest(
    requestId: string,
    operation: string,
    payload: LocalChannelPayload = null,
): string {
    assertRequestId(requestId);
    assertOperation(operation);
    assertPayload(payload);

    return serializeWithSize({
        version: LOCAL_CHANNEL_VERSION,
        kind: "request",
        requestId,
        operation,
        payload,
    });
}

export function parseMessage(raw: string): LocalChannelMessage {
    ensureMessageSize(raw);

    let value: unknown;
    try {
        value = JSON.parse(raw);
    } catch {
        throw new LocalChannelProtocolError(
            "invalid_json",
            "Local Channel message is not valid JSON.",
        );
    }

    if (!isRecord(value)) {
        throw invalidMessage();
    }
    assertJsonDepth(value);

    const kind = value.kind;
    if (kind === "response") {
        assertExactKeys(value, ["version", "kind", "requestId", "result"]);
        assertVersion(value.version);
        const requestId = readRequestId(value.requestId);
        assertPayload(value.result);
        return {
            version: LOCAL_CHANNEL_VERSION,
            kind,
            requestId,
            result: value.result,
        };
    }

    if (kind === "error") {
        assertExactKeys(value, ["version", "kind", "requestId", "error"]);
        assertVersion(value.version);
        const requestId = readRequestId(value.requestId);
        if (!isRecord(value.error)) throw invalidMessage(requestId);
        assertExactKeys(value.error, ["code", "messageKey"]);
        if (
            typeof value.error.code !== "string" ||
            typeof value.error.messageKey !== "string" ||
            value.error.code.length === 0 ||
            value.error.messageKey.length === 0
        ) {
            throw invalidMessage(requestId);
        }
        return {
            version: LOCAL_CHANNEL_VERSION,
            kind,
            requestId,
            error: {
                code: value.error.code,
                messageKey: value.error.messageKey,
            },
        };
    }

    throw new LocalChannelProtocolError(
        kind === undefined ? "invalid_message" : "unsupported_kind",
        "Local Channel message kind is not supported.",
        readOptionalRequestId(value.requestId),
    );
}

function serializeWithSize(value: unknown): string {
    const raw = JSON.stringify(value);
    ensureMessageSize(raw);
    return raw;
}

function ensureMessageSize(raw: string): void {
    if (
        new TextEncoder().encode(raw).byteLength >
        MAX_LOCAL_CHANNEL_MESSAGE_BYTES
    ) {
        throw new LocalChannelProtocolError(
            "message_too_large",
            "Local Channel message exceeds the allowed size.",
        );
    }
}

function assertVersion(
    value: unknown,
): asserts value is typeof LOCAL_CHANNEL_VERSION {
    if (value !== LOCAL_CHANNEL_VERSION) {
        throw new LocalChannelProtocolError(
            "unsupported_version",
            "Local Channel version is not supported.",
        );
    }
}

function assertRequestId(value: string): void {
    if (!REQUEST_ID_PATTERN.test(value)) {
        throw new LocalChannelProtocolError(
            "invalid_request",
            "Local Channel request ID is invalid.",
            null,
        );
    }
}

function readRequestId(value: unknown): string {
    if (typeof value !== "string") throw invalidMessage();
    assertRequestId(value);
    return value;
}

function readOptionalRequestId(value: unknown): string | null {
    return typeof value === "string" && REQUEST_ID_PATTERN.test(value)
        ? value
        : null;
}

function assertOperation(value: string): void {
    if (value.length > 100 || !OPERATION_PATTERN.test(value)) {
        throw new LocalChannelProtocolError(
            "invalid_request",
            "Local Channel operation is invalid.",
        );
    }
}

function assertPayload(value: unknown): asserts value is LocalChannelPayload {
    if (value !== null && (!isRecord(value) || Array.isArray(value))) {
        throw invalidMessage();
    }
}

function assertExactKeys(
    value: Record<string, unknown>,
    expected: string[],
): void {
    const keys = Object.keys(value);
    if (
        keys.length !== expected.length ||
        new Set(keys).size !== expected.length ||
        expected.some(
            (key) => !Object.prototype.hasOwnProperty.call(value, key),
        )
    ) {
        throw new LocalChannelProtocolError(
            "invalid_request",
            "Local Channel message contains an unknown field.",
            readOptionalRequestId(value.requestId),
        );
    }
}

function invalidMessage(
    requestId: string | null = null,
): LocalChannelProtocolError {
    return new LocalChannelProtocolError(
        "invalid_request",
        "Local Channel message is invalid.",
        requestId,
    );
}

function assertJsonDepth(value: unknown, depth = 0): void {
    if (depth > MAX_LOCAL_CHANNEL_JSON_DEPTH) {
        throw new LocalChannelProtocolError(
            "invalid_request",
            "Local Channel message is too deeply nested.",
        );
    }
    if (Array.isArray(value)) {
        value.forEach((item) => assertJsonDepth(item, depth + 1));
    } else if (isRecord(value)) {
        Object.values(value).forEach((item) =>
            assertJsonDepth(item, depth + 1),
        );
    }
}

function isRecord(value: unknown): value is Record<string, unknown> {
    return typeof value === "object" && value !== null;
}
