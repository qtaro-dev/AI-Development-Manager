import path from "node:path";
import { fileURLToPath } from "node:url";
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

const rootDirectory = path.dirname(fileURLToPath(import.meta.url));

export default defineConfig({
    plugins: [react()],
    resolve: {
        alias: {
            "@": path.resolve(rootDirectory, "src"),
        },
    },
    server: {
        host: "127.0.0.1",
    },
    preview: {
        host: "127.0.0.1",
    },
    build: {
        outDir: "dist",
        emptyOutDir: true,
    },
});
