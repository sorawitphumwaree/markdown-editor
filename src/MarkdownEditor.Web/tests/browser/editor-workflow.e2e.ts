import { expect, test, type Page } from "@playwright/test";

declare global {
  interface Window {
    __hostMessages: any[];
    __sendHostMessage: (message: unknown) => void;
  }
}

async function startHost(page: Page): Promise<void> {
  await page.addInitScript(() => {
    const listeners: Array<(event: { data: unknown }) => void> = [];
    window.__hostMessages = [];
    window.__sendHostMessage = message => listeners.forEach(listener => listener({ data: message }));
    const chrome = (window as any).chrome ?? {};
    Object.defineProperty(chrome, "webview", {
      configurable: true,
      value: {
        addEventListener: (type: string, listener: (event: { data: unknown }) => void) => {
          if (type === "message") listeners.push(listener);
        },
        postMessage: (message: unknown) => window.__hostMessages.push(message)
      }
    });
    if (!(window as any).chrome)
      Object.defineProperty(window, "chrome", { value: chrome, configurable: true });
  });
  await page.goto("/");
  await expect.poll(() => messages(page, "web.ready")).toHaveLength(1);
}

async function send(page: Page, type: string, payload: unknown, version = 1): Promise<void> {
  await page.evaluate(({ type, payload, version }) => window.__sendHostMessage({
    type,
    protocolVersion: 1,
    documentId: "workflow-test",
    version,
    payload
  }), { type, payload, version });
}

async function load(page: Page, content: string, mode: "preview" | "split" = "split"): Promise<void> {
  await send(page, "document.load", {
    content,
    filePath: "workflow.md",
    viewMode: mode,
    cursorLine: 1,
    editorScroll: 0,
    previewScroll: 0
  });
  await expect.poll(() => messages(page, "preview.renderCompleted")).toHaveLength(1);
}

const messages = (page: Page, type: string) =>
  page.evaluate(type => window.__hostMessages.filter(message => message.type === type), type);

test.beforeEach(async ({ page }) => startHost(page));

test("loads markdown into editor and sanitized preview", async ({ page }) => {
  await load(page, "# Heading\n\n<script>bad()</script>\n\n**bold**");

  await expect(page.locator(".cm-content")).toContainText("Heading");
  await expect(page.locator("#preview h1")).toHaveText("Heading");
  await expect(page.locator("#preview strong")).toHaveText("bold");
  await expect(page.locator("#preview script")).toHaveCount(0);
});

test("editing sends changed content and refreshes preview", async ({ page }) => {
  await load(page, "before");
  await page.locator(".cm-content").click();
  await page.keyboard.press("Control+A");
  await page.keyboard.type("# After");

  await expect.poll(async () => (await messages(page, "document.changed")).at(-1)?.payload.content)
    .toBe("# After");
  await expect(page.locator("#preview h1")).toHaveText("After");
});

test("preview links are delegated to the native host", async ({ page }) => {
  await load(page, "[Docs](guide.md#intro)", "preview");

  await page.locator("#preview a").click();

  await expect.poll(async () => (await messages(page, "preview.linkClicked")).at(-1)?.payload.href)
    .toBe("guide.md#intro");
});

test("native messages switch modes, navigate fragments, and clear the document", async ({ page }) => {
  await load(page, "# First\n\n## Target", "split");
  await send(page, "view.setMode", { mode: "preview" });
  await expect(page.locator("#workspace")).toHaveAttribute("data-mode", "preview");

  await send(page, "preview.navigateFragment", { fragment: "target" });
  await send(page, "document.clear", {});

  await expect(page.locator(".cm-content")).toHaveText("");
  await expect(page.locator("#preview")).toHaveText("");
});

test("application shortcuts are forwarded with document context", async ({ page }) => {
  await load(page, "content");
  await page.keyboard.press("Control+Shift+S");

  await expect.poll(async () => (await messages(page, "application.shortcut")).at(-1)?.payload.command)
    .toBe("saveAs");
});

test("messages with an incompatible protocol version are ignored", async ({ page }) => {
  await page.evaluate(() => window.__sendHostMessage({
    type: "document.load",
    protocolVersion: 999,
    documentId: "ignored",
    version: 1,
    payload: { content: "ignored", viewMode: "preview", cursorLine: 1, editorScroll: 0, previewScroll: 0 }
  }));

  await expect(page.locator(".cm-content")).toHaveText("");
  await expect(messages(page, "preview.renderCompleted")).resolves.toHaveLength(0);
});
