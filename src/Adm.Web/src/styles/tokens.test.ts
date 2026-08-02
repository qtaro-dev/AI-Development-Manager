import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const tokenSource = fs.readFileSync(
    path.resolve(path.dirname(fileURLToPath(import.meta.url)), "tokens.css"),
    "utf8",
);

describe("design tokens", () => {
    it("contains the P0-021 spacing, radius, focus, and semantic color tokens", () => {
        for (const token of [
            "--adm-space-1",
            "--adm-space-4",
            "--adm-space-8",
            "--adm-radius-control",
            "--adm-radius-panel",
            "--adm-focus-ring-width",
            "--adm-color-bg",
            "--adm-color-surface",
            "--adm-color-text",
            "--adm-color-primary",
            "--adm-color-success",
            "--adm-color-warning",
            "--adm-color-danger",
        ]) {
            expect(tokenSource).toContain(`${token}:`);
        }
    });

    it("defines both light and dark semantic color sets", () => {
        expect(tokenSource).toMatch(/--adm-color-bg:\s*#f6f7fb/);
        expect(tokenSource).toMatch(
            /:root\[data-theme="dark"\][\s\S]*--adm-color-bg:\s*#151924/,
        );
    });
});
