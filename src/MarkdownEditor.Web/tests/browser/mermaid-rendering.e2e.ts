import { expect, test, type Page } from "@playwright/test";
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

declare global {
  interface Window {
    __hostMessages: unknown[];
    __sendHostMessage: (message: unknown) => void;
  }
}

async function loadMarkdown(page: Page, source: string): Promise<void> {
  await page.addInitScript(() => {
    const listeners: Array<(event: { data: unknown }) => void> = [];
    window.__hostMessages = [];
    window.__sendHostMessage = message => {
      listeners.forEach(listener => listener({ data: message }));
    };
    const chrome = (window as any).chrome ?? {};
    Object.defineProperty(chrome, "webview", {
      value: {
        addEventListener: (
          type: string,
          listener: (event: { data: unknown }) => void
        ) => {
          if (type === "message")
            listeners.push(listener);
        },
        postMessage: (message: unknown) => window.__hostMessages.push(message)
      },
      configurable: true
    });
    if (!(window as any).chrome)
      Object.defineProperty(window, "chrome", { value: chrome, configurable: true });
  });

  await page.goto("/");
  await expect.poll(() => page.evaluate(() =>
    window.__hostMessages.some((message: any) => message.type === "web.ready")
  )).toBe(true);

  await page.evaluate(markdown => {
    window.__sendHostMessage({
      type: "document.load",
      protocolVersion: 1,
      documentId: "browser-test",
      version: 1,
      payload: {
        content: markdown,
        cursorLine: 1,
        viewMode: "preview",
        editorScroll: 0,
        previewScroll: 0
      }
    });
  }, source);

  await expect.poll(() => page.evaluate(() =>
    window.__hostMessages.some((message: any) =>
      message.type === "preview.renderCompleted"
    )
  )).toBe(true);
}

const fixturePath = fileURLToPath(new URL(
  "../../../../docs/test-cases/mermaid-rendering.md",
  import.meta.url
));
const fixtureSource = readFileSync(fixturePath, "utf8");
const expectedLabels = [
  "Markdown source",
  "WPF Host",
  "Document",
  "Still",
  "Draft",
  "DOCUMENT",
  "Open fixture",
  "Reproduce defect",
  "Automated",
  "State diagram",
  "visible_labels",
  "reproduce",
  "Markdown Editor",
  "Mermaid",
  "Missing labels",
  "Markdown",
  "Rendering checks",
  "Markdown source",
  "Header",
  "Reproduce defect",
  "WPF Host",
  "Parse",
  "Automated tests"
] as const;

const fixtures = [...fixtureSource.matchAll(
  /^## (\d+) (.+?)\r?\n[\s\S]*?```mermaid\r?\n([\s\S]*?)```/gm
)].map((match, index) => ({
  number: match[1]!,
  name: match[2]!,
  source: match[3]!.trim(),
  expectedLabel: expectedLabels[index]!
}));

test("manual fixture covers every supported Mermaid family", () => {
  expect(fixtures).toHaveLength(expectedLabels.length);
  expect(fixtures.every(fixture => fixture.expectedLabel)).toBe(true);
});

for (const fixture of fixtures) {
  test(`${fixture.number} ${fixture.name} renders sanitized labeled SVG`, async ({ page }) => {
    await loadMarkdown(page, `\`\`\`mermaid\n${fixture.source}\n\`\`\``);

    const diagram = page.locator(".mermaid-diagram");
    await expect(diagram).toHaveCount(1);
    await expect(page.locator(".render-error")).toHaveCount(0);
    const rootSvg = diagram.locator(":scope > svg");
    await expect(rootSvg).toHaveCount(1);
    await expect(diagram).toContainText(fixture.expectedLabel);

    const geometry = await rootSvg.evaluate(svg => {
      const box = svg.getBoundingClientRect();
      const viewBox = svg.getAttribute("viewBox")?.split(/\s+/).map(Number) ?? [];
      return {
        width: box.width,
        height: box.height,
        viewBox,
        drawingElements: svg.querySelectorAll(
          "path, rect, circle, ellipse, polygon, line"
        ).length,
        markers: svg.querySelectorAll("marker").length
      };
    });

    expect(geometry.width).toBeGreaterThan(0);
    expect(geometry.height).toBeGreaterThan(0);
    expect(geometry.viewBox).toHaveLength(4);
    expect(geometry.viewBox.every(Number.isFinite)).toBe(true);
    expect(geometry.drawingElements).toBeGreaterThan(0);
    if (fixture.name === "State v2")
      expect(geometry.markers).toBeGreaterThan(0);
  });
}
