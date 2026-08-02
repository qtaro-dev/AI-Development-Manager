import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const webRoot = path.resolve(scriptDirectory, "../../src/Adm.Web");
const sourceRoot = path.join(webRoot, "src");
const catalogPath = path.join(sourceRoot, "messages/catalog.ts");

function collectFiles(directory) {
    return fs.readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
        const entryPath = path.join(directory, entry.name);
        if (entry.isDirectory()) return collectFiles(entryPath);
        return /\.(ts|tsx)$/.test(entry.name) ? [entryPath] : [];
    });
}

const catalog = fs.readFileSync(catalogPath, "utf8");
const catalogKeys = new Set(
    [...catalog.matchAll(/^\s+"([^"]+)":\s*"/gm)].map((match) => match[1]),
);
const files = collectFiles(sourceRoot).filter((file) => file !== catalogPath);
const references = new Map();
const violations = [];

for (const file of files) {
    const relative = path.relative(webRoot, file);
    const source = fs.readFileSync(file, "utf8");

    for (const match of source.matchAll(/\bmessage\(\s*"([^"]+)"/g)) {
        const key = match[1];
        if (!catalogKeys.has(key)) {
            violations.push(`${relative}: unknown message key ${key}`);
        }
        references.set(key, (references.get(key) ?? 0) + 1);
    }

    if (
        !file.includes(`${path.sep}test${path.sep}`) &&
        !/\.test\.(ts|tsx)$/.test(file) &&
        /\.tsx$/.test(file)
    ) {
        const literal = source.match(/>[^<>{}]*[ぁ-んァ-ン一-龯々][^<>{}]*</);
        if (literal) {
            violations.push(`${relative}: Japanese JSX text must use message()`);
        }
    }
}

for (const key of catalogKeys) {
    if (!references.has(key)) {
        violations.push(`catalog: unused message key ${key}`);
    }
}

if (violations.length > 0) {
    console.error("Web message catalog validation failed:");
    for (const violation of violations) console.error(`- ${violation}`);
    process.exitCode = 1;
} else {
    console.log(
        `Web message catalog validation passed: ${catalogKeys.size} keys, ${references.size} referenced keys.`,
    );
}
