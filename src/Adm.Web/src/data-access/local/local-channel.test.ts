import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it, vi } from "vitest";
import {
    LocalChannelClient,
    LocalChannelProtocolError,
    parseMessage,
    serializeRequest,
    type LocalChannelTransport,
} from "./index";

const fixtureRoot = resolve(
    process.cwd(),
    "../../tests/fixtures/local-channel",
);
const fixture = (name: string) =>
    readFileSync(resolve(fixtureRoot, name), "utf8");

describe("Local Application Channel v1", () => {
    it("parses the shared response and error fixtures", () => {
        expect(parseMessage(fixture("valid-response.json")).kind).toBe(
            "response",
        );
        expect(parseMessage(fixture("valid-error.json")).kind).toBe("error");
    });

    it("serializes a strict request envelope", () => {
        const request = JSON.parse(
            serializeRequest("request-001", "test.echo", { value: "fixture" }),
        );

        expect(request).toEqual({
            version: 1,
            kind: "request",
            requestId: "request-001",
            operation: "test.echo",
            payload: { value: "fixture" },
        });
    });

    it("rejects unknown fields, unsupported kinds, and oversized messages", () => {
        expect(() =>
            parseMessage(fixture("invalid-unknown-field.json")),
        ).toThrow(LocalChannelProtocolError);
        expect(() =>
            parseMessage(
                '{"version":1,"kind":"request","requestId":"r","operation":"test.echo","payload":{}}',
            ),
        ).toThrow("kind");
        expect(() =>
            parseMessage(
                `{"version":1,"kind":"response","requestId":"request-001","result":{"value":"${"x".repeat(1024 * 1024)}"}}`,
            ),
        ).toThrow("size");
    });

    it("correlates only pending responses and ignores unknown or duplicate IDs", async () => {
        let receive: ((message: string) => void) | undefined;
        const transport: LocalChannelTransport = {
            postMessage: vi.fn((message) => {
                const request = JSON.parse(message) as { requestId: string };
                receive?.(
                    JSON.stringify({
                        version: 1,
                        kind: "response",
                        requestId: request.requestId,
                        result: { ok: true },
                    }),
                );
            }),
            subscribe: (listener) => {
                receive = listener;
                return () => {
                    receive = undefined;
                };
            },
        };
        const client = new LocalChannelClient(transport);

        await expect(client.request("test.echo", {})).resolves.toEqual({
            ok: true,
        });
        receive?.(
            '{"version":1,"kind":"response","requestId":"unknown","result":{}}',
        );
        client.dispose();
    });

    it("rejects safely when WebView2 transport is unavailable", async () => {
        await expect(
            new LocalChannelClient({
                postMessage: () => {
                    throw new Error("transport unavailable");
                },
                subscribe: () => () => undefined,
            }).request("test.echo"),
        ).rejects.toMatchObject({ code: "channel_unavailable" });
    });
});
