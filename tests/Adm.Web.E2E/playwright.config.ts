import { defineConfig, devices } from "@playwright/test";

const baseURL = "http://127.0.0.1:5199";

export default defineConfig({
    testDir: "./specs",
    fullyParallel: true,
    forbidOnly: Boolean(process.env.CI),
    retries: process.env.CI ? 2 : 0,
    workers: process.env.CI ? 1 : undefined,
    reporter: process.env.CI
        ? [["list"], ["junit", { outputFile: "../../artifacts/ci-evidence/playwright/results.xml" }]]
        : [["list"]],
    outputDir: "../../artifacts/ci-evidence/playwright/test-results",
    use: {
        baseURL,
        trace: "retain-on-failure",
        screenshot: "only-on-failure",
        video: "retain-on-failure",
        ...devices["Desktop Chrome"],
    },
    webServer: {
        command: "dotnet ../../artifacts/bin/Adm.Server.Host/Debug/net10.0/Adm.Server.Host.dll --Server:Port=5199",
        url: `${baseURL}/health/ready`,
        reuseExistingServer: !process.env.CI,
        timeout: 120_000,
    },
});
