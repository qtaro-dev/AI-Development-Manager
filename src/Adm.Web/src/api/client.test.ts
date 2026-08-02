import { describe, expect, it, vi } from "vitest";
import { createApiClient } from "./client";

describe("ApiClient", () => {
    it("uses the injectable fetch boundary", async () => {
        const request = vi.fn<typeof fetch>(
            async () =>
                new Response(
                    JSON.stringify({
                        apiVersion: "v1",
                        contractVersion: "1.0",
                        serverTimeUtc: "2026-08-03T00:00:00Z",
                        status: "ready",
                    }),
                    {
                        status: 200,
                        headers: { "Content-Type": "application/json" },
                    },
                ),
        );
        const client = createApiClient("/api/v1", request);

        await expect(client.getVersion()).resolves.toMatchObject({
            apiVersion: "v1",
        });
        expect(request).toHaveBeenCalledWith("/api/v1/version", {
            headers: { Accept: "application/json" },
        });
    });
});
