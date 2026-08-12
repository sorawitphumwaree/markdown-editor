import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./tests/browser",
  testMatch: "**/*.e2e.ts",
  fullyParallel: false,
  retries: 0,
  workers: 1,
  reporter: "list",
  use: {
    baseURL: "http://127.0.0.1:4173",
    trace: "retain-on-failure"
  },
  projects: [
    {
      name: "edge",
      use: {
        ...devices["Desktop Edge"],
        channel: "msedge"
      }
    }
  ],
  webServer: {
    command: "corepack pnpm dev --host 127.0.0.1 --port 4173",
    url: "http://127.0.0.1:4173",
    reuseExistingServer: !process.env.CI,
    timeout: 120_000
  }
});
