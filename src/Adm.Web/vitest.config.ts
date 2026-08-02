import path from "node:path";
import { fileURLToPath } from "node:url";
import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";

const rootDirectory = path.dirname(fileURLToPath(import.meta.url));

export default defineConfig({
    plugins: [react()],
    resolve: {
        alias: {
            "@": path.resolve(rootDirectory, "src"),
        },
    },
    test: {
        environment: "jsdom",
        setupFiles: ["./src/test/test-setup.ts"],
        include: ["./src/**/*.test.{ts,tsx}"],
        coverage: {
            provider: "v8",
            reporter: ["text", "json-summary"],
            reportsDirectory: "./coverage",
            thresholds: {
                lines: 70,
                functions: 70,
                statements: 70,
                branches: 60,
            },
        },
    },
});
