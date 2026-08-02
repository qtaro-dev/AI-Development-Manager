import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const webRoot = path.resolve(scriptDirectory, "../../src/Adm.Web");
const sourceRoot = path.join(webRoot, "src");
const tokenPath = path.join(sourceRoot, "styles/tokens.css");
const tokenSource = fs.readFileSync(tokenPath, "utf8");
const requiredTokens = [
    "--adm-space-1",
    "--adm-space-4",
    "--adm-space-8",
    "--adm-radius-control",
    "--adm-radius-panel",
    "--adm-focus-ring-width",
    "--adm-color-bg",
    "--adm-color-surface",
    "--adm-color-text",
    "--adm-color-muted",
    "--adm-color-border",
    "--adm-color-primary",
    "--adm-color-success",
    "--adm-color-warning",
    "--adm-color-danger",
];
const violations = [];

for (const token of requiredTokens) {
    if (!new RegExp(`${token}\\s*:`).test(tokenSource)) {
        violations.push(`tokens.css: missing ${token}`);
    }
}

const files = [];
function collect(directory) {
    for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
        const entryPath = path.join(directory, entry.name);
        if (entry.isDirectory()) collect(entryPath);
        else if (/\.css$/.test(entry.name) && entryPath !== tokenPath) {
            files.push(entryPath);
        }
    }
}
collect(sourceRoot);

for (const file of files) {
    const source = fs.readFileSync(file, "utf8");
    const relative = path.relative(webRoot, file);
    if (/#(?:[0-9a-f]{3,8})\b/i.test(source)) {
        violations.push(`${relative}: direct color literal must use a design token`);
    }
    const declarationSource = source.split("@media")[0];
    if (/(?<![\w-])\d+px\b/.test(declarationSource)) {
        violations.push(`${relative}: direct pixel value must use a design token`);
    }
}

const darkTheme = tokenSource.match(
    /:root\[data-theme="dark"\][\s\S]*?\}/,
)?.[0];
if (!tokenSource.includes("--adm-color-bg:") || !darkTheme?.includes("--adm-color-bg:")) {
    violations.push("tokens.css: light/dark color themes are incomplete");
}

if (violations.length > 0) {
    console.error("Web design token validation failed:");
    for (const violation of violations) console.error(`- ${violation}`);
    process.exitCode = 1;
} else {
    console.log(
        `Web design token validation passed: ${requiredTokens.length} required tokens.`,
    );
}
