import { mkdir, open, readFile, stat, unlink } from "node:fs/promises";
import { join, resolve } from "node:path";
import { spawn } from "node:child_process";

const webRoot = resolve(process.cwd());
const distRoot = join(webRoot, "dist");
const lockPath = join(distRoot, ".adm-web-build.lock");
const isWindows = process.platform === "win32";
const npmCommand = isWindows ? "npm.cmd" : "npm";
const waitMilliseconds = 250;
const staleMilliseconds = 10 * 60 * 1000;

await mkdir(distRoot, { recursive: true });

let lock;
while (!lock) {
    try {
        lock = await open(lockPath, "wx");
        await lock.writeFile(`${process.pid}\n`);
    } catch (error) {
        if (error.code !== "EEXIST") throw error;

        try {
            const stats = await readFile(lockPath, "utf8");
            const age = Date.now() - (await stat(lockPath)).mtimeMs;
            if (age > staleMilliseconds && stats.trim().length > 0) {
                await unlink(lockPath);
                continue;
            }
        } catch (lockError) {
            if (lockError.code !== "ENOENT") throw lockError;
        }

        await new Promise(resolveWait => setTimeout(resolveWait, waitMilliseconds));
    }
}

try {
    const runNpm = args => new Promise((resolveExit, reject) => {
        const child = spawn(npmCommand, args, {
            cwd: webRoot,
            stdio: "inherit",
            shell: true,
            windowsHide: true,
        });
        child.once("error", reject);
        child.once("exit", code => resolveExit(code ?? 1));
    });

    const installExitCode = await runNpm(["ci", "--ignore-scripts"]);
    if (installExitCode !== 0) {
        process.exitCode = installExitCode;
    } else {
        process.exitCode = await runNpm(["run", "build"]);
    }
} finally {
    await lock.close();
    await unlink(lockPath).catch(error => {
        if (error.code !== "ENOENT") throw error;
    });
}
