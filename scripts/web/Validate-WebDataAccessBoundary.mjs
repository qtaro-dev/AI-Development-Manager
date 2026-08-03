import { readdir, readFile } from "node:fs/promises";
import { join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const repositoryRoot = resolve(fileURLToPath(new URL("../..", import.meta.url)));
const sourceRoot = join(repositoryRoot, "src/Adm.Web/src");
const forbiddenPatterns = [
    { pattern: /\bfetch\s*\(/, label: "direct fetch" },
    { pattern: /window\.chrome\.webview/, label: "direct WebView2 API" },
    { pattern: /from ["'][^"']*\/api\/client["']/, label: "direct ApiClient import" },
];

async function sourceFiles(directory) {
    const entries = await readdir(directory, { withFileTypes: true });
    const files = [];
    for (const entry of entries) {
        const path = join(directory, entry.name);
        if (entry.isDirectory()) files.push(...(await sourceFiles(path)));
        else if (/\.(ts|tsx)$/.test(entry.name)) files.push(path);
    }
    return files;
}

const files = await sourceFiles(sourceRoot);
const violations = [];
for (const file of files) {
    const relativePath = relative(sourceRoot, file).replaceAll("\\", "/");
    if (
        relativePath.startsWith("api/") ||
        relativePath.startsWith("data-access/") ||
        relativePath.includes(".test.") ||
        relativePath.startsWith("test/") ||
        relativePath === "vite-env.d.ts"
    ) {
        continue;
    }

    const contents = await readFile(file, "utf8");
    for (const { pattern, label } of forbiddenPatterns) {
        if (pattern.test(contents)) violations.push(`${relativePath}: ${label}`);
    }
}

if (violations.length > 0) {
    console.error("Web UI DataAccess boundary violations:");
    console.error(violations.join("\n"));
    process.exit(1);
}

console.log("Web UI DataAccess boundary passed: UI source has no direct transport dependency.");
