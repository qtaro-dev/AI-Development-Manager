import { readdir, readFile } from 'node:fs/promises';
import path from 'node:path';
import process from 'node:process';

const repositoryRoot = path.resolve(import.meta.dirname, '../..');
const distRoot = path.join(repositoryRoot, 'src', 'Adm.Web', 'dist');
const forbiddenPatterns = [
  /(?:api[_-]?key|access[_-]?token|client[_-]?secret|password|private[_-]?key)\s*[:=]\s*["'`][^"'`]{4,}/i,
  /VITE_(?:API[_-]?KEY|ACCESS[_-]?TOKEN|CLIENT[_-]?SECRET|PASSWORD|PRIVATE[_-]?KEY)/i,
  /Bearer\s+[A-Za-z0-9._~+/=-]{8,}/i,
  /[A-Z]:[\\/][^\n"']+/,
];

async function getFiles(directory) {
  const entries = await readdir(directory, { withFileTypes: true });
  const files = [];
  for (const entry of entries) {
    const entryPath = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      files.push(...(await getFiles(entryPath)));
    } else {
      files.push(entryPath);
    }
  }
  return files;
}

const files = await getFiles(distRoot);
for (const file of files) {
  const content = await readFile(file, 'utf8');
  for (const [index, pattern] of forbiddenPatterns.entries()) {
    if (pattern.test(content)) {
      throw new Error(`Forbidden secret or local absolute path detected in ${path.relative(repositoryRoot, file)} (rule ${index + 1}).`);
    }
  }
}

console.log(`Web bundle validation passed: ${files.length} files.`);
