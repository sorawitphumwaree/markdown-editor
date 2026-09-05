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

async function replaceMarkdown(page: Page, source: string, version: number): Promise<void> {
  await page.evaluate(({ markdown, documentVersion }) => {
    window.__sendHostMessage({
      type: "document.load",
      protocolVersion: 1,
      documentId: "browser-test",
      version: documentVersion,
      payload: {
        content: markdown,
        cursorLine: 1,
        viewMode: "preview",
        editorScroll: 0,
        previewScroll: 0
      }
    });
  }, { markdown: source, documentVersion: version });

  await expect.poll(() => page.evaluate(documentVersion =>
    window.__hostMessages.some((message: any) =>
      message.type === "preview.renderCompleted" && message.version === documentVersion
    ), version
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

test("repeated invalid Mermaid renders keep only the latest block error", async ({ page }) => {
  await loadMarkdown(page, "```mermaid\nflowchart TD\nA[First\n```");
  await expect(page.locator("#preview .render-error")).toHaveCount(1);
  await expect(page.locator('body > div[id^="dmermaid-"]')).toHaveCount(0);

  await replaceMarkdown(page, "```mermaid\nflowchart TD\nB[Second\n```", 2);

  await expect(page.locator("#preview .render-error")).toHaveCount(1);
  await expect(page.locator("#preview .render-error")).toContainText("Second");
  await expect(page.locator('body > div[id^="dmermaid-"]')).toHaveCount(0);

  await page.evaluate(() => {
    window.__sendHostMessage({
      type: "export.prepare",
      protocolVersion: 1,
      documentId: "browser-test",
      version: 2,
      payload: {}
    });
  });
  await expect.poll(() => page.evaluate(() =>
    window.__hostMessages.some((message: any) =>
      message.type === "export.ready" && message.version === 2
    )
  )).toBe(true);
  await expect(page.locator("#preview .render-error")).toHaveCount(1);
  await expect(page.locator('body > div[id^="dmermaid-"]')).toHaveCount(0);
});

test("a valid Mermaid render clears the preceding block error", async ({ page }) => {
  await loadMarkdown(page, "```mermaid\nflowchart TD\nA[Broken\n```");
  await expect(page.locator("#preview .render-error")).toHaveCount(1);

  await replaceMarkdown(page, "```mermaid\nflowchart TD\nA[Valid] --> B[Done]\n```", 2);

  await expect(page.locator("#preview .render-error")).toHaveCount(0);
  await expect(page.locator("#preview .mermaid-diagram")).toHaveCount(1);
  await expect(page.locator("#preview .mermaid-diagram")).toContainText("Valid");
  await expect(page.locator('body > div[id^="dmermaid-"]')).toHaveCount(0);
});

test("Mermaid zoom, pan, and reset stay scoped to one diagram", async ({ page }) => {
  await loadMarkdown(page, [
    "```mermaid",
    "flowchart LR",
    "A[Start] --> B[Review] --> C[Build] --> D[Test] --> E[Ship]",
    "A --> F[Plan] --> G[Design] --> H[Implement] --> I[Verify] --> E",
    "A --> J[Research] --> K[Prototype] --> L[Measure] --> M[Refine] --> E",
    "```",
    "",
    "```mermaid",
    "flowchart TD",
    "X[Independent] --> Y[Diagram]",
    "```"
  ].join("\n"));

  const diagrams = page.locator(".mermaid-diagram");
  const first = diagrams.nth(0);
  const second = diagrams.nth(1);
  await first.getByRole("button", { name: "Zoom in" }).click();
  await first.getByRole("button", { name: "Zoom in" }).click();
  await first.getByRole("button", { name: "Zoom in" }).click();
  await first.getByRole("button", { name: "Zoom in" }).click();

  await expect(first.locator(".mermaid-zoom-label")).toHaveText("200%");
  await expect(second.locator(".mermaid-zoom-label")).toHaveText("100%");
  const beforePan = await first.evaluate(element => ({
    left: element.scrollLeft,
    top: element.scrollTop,
    width: element.scrollWidth
  }));
  expect(beforePan.width).toBeGreaterThan(await first.evaluate(element => element.clientWidth));

  const bounds = await first.boundingBox();
  expect(bounds).not.toBeNull();
  await page.mouse.move(bounds!.x + bounds!.width * 0.75, bounds!.y + bounds!.height * 0.7);
  await page.mouse.down();
  await page.mouse.move(bounds!.x + bounds!.width * 0.35, bounds!.y + bounds!.height * 0.35);
  await page.mouse.up();
  const afterPan = await first.evaluate(element => ({
    left: element.scrollLeft,
    top: element.scrollTop
  }));
  expect(afterPan.left).toBeGreaterThan(beforePan.left);
  expect(afterPan.top).toBeGreaterThanOrEqual(beforePan.top);

  await first.getByRole("button", { name: "Reset zoom and pan" }).click();
  await expect(first.locator(".mermaid-zoom-label")).toHaveText("100%");
  await expect.poll(() => first.evaluate(element => [element.scrollLeft, element.scrollTop]))
    .toEqual([0, 0]);
});

test("dense labelled flowcharts use readable non-overlapping ELK routing", async ({ page }) => {
  await loadMarkdown(page, [
    "```mermaid",
    "flowchart LR",
    "Request[Incoming request] -->|validate| Validate{Valid?}",
    "Validate -->|yes| Authorize{Authorized?}",
    "Validate -->|no| Reject[Reject request]",
    "Authorize -->|yes| Process[Process request]",
    "Authorize -->|no| Reject",
    "Process -->|cached| Cache[(Result cache)]",
    "Process -->|complete| Return[Return response]",
    "Cache -->|hit| Return",
    "Cache -->|miss| Process",
    "Reject -->|error response| Return",
    "```"
  ].join("\n"));

  const diagram = page.locator(".mermaid-diagram");
  const geometry = await diagram.evaluate(element => {
    const rectangles = (selector: string) => [...element.querySelectorAll<SVGGraphicsElement>(selector)]
      .map(item => item.getBoundingClientRect())
      .filter(rect => rect.width > 0 && rect.height > 0);
    const overlaps = (first: DOMRect, second: DOMRect) =>
      first.left < second.right - 1
      && first.right > second.left + 1
      && first.top < second.bottom - 1
      && first.bottom > second.top + 1;
    const nodes = rectangles("g.node");
    const labels = rectangles("g.edgeLabel");
    const labelCollisions = labels.flatMap((label, index) =>
      labels.slice(index + 1).filter(other => overlaps(label, other)));
    const nodeLabelCollisions = labels.flatMap(label =>
      nodes.filter(node => overlaps(label, node)));
    const edgePaths = [...element.querySelectorAll<SVGPathElement>("path.flowchart-link")];

    return {
      nodeCount: nodes.length,
      labelCount: labels.length,
      labelCollisionCount: labelCollisions.length,
      nodeLabelCollisionCount: nodeLabelCollisions.length,
      cubicEdgeCount: edgePaths.filter(path => /\bC\s/i.test(path.getAttribute("d") ?? "")).length,
      mappedEdgeCount: element.querySelectorAll(
        ".edgePath[data-source-start], path.flowchart-link[data-source-start]"
      ).length,
      mappedLabelCount: element.querySelectorAll(".edgeLabel[data-source-start]").length
    };
  });

  expect(geometry).toEqual({
    nodeCount: 7,
    labelCount: 10,
    labelCollisionCount: 0,
    nodeLabelCollisionCount: 0,
    cubicEdgeCount: 0,
    mappedEdgeCount: 10,
    mappedLabelCount: 10
  });

  await page.evaluate(() => {
    window.__sendHostMessage({
      type: "view.setMode",
      protocolVersion: 1,
      documentId: "browser-test",
      version: 1,
      payload: { mode: "split" }
    });
  });
  await expect(diagram).toBeVisible();

  await page.evaluate(() => {
    window.__sendHostMessage({
      type: "export.prepare",
      protocolVersion: 1,
      documentId: "browser-test",
      version: 1,
      payload: {}
    });
  });
  await expect.poll(() => page.evaluate(() =>
    window.__hostMessages.some((message: any) =>
      message.type === "export.ready" && message.version === 1
    )
  )).toBe(true);
  await expect(diagram.locator("g.edgeLabel")).toHaveCount(10);
});

test("dense machine state diagram keeps transition labels clear", async ({ page }) => {
  await loadMarkdown(page, [
    "```mermaid",
    "stateDiagram",
    "[*] --> Starting",
    "Starting --> Initializing : Load configuration / Initialize system",
    "Initializing --> Production : Machine ready",
    "Initializing --> NotReady : Machine not ready",
    "Production --> Production : Operator login / Select recipe / Select module lot / Execute test",
    "Production --> Engineering : Ctrl+Q+W+E",
    "NotReady --> NotReady : Display machine & hardware status / Contact engineer",
    "NotReady --> Engineering : Ctrl+Q+W+E",
    "Engineering --> Engineering : Configure machine / Diagnose / Manual test",
    "Engineering --> Production : Machine ready / Exit engineering",
    "Engineering --> NotReady : Machine not ready / Exit engineering",
    "Production --> ShuttingDown : Shutdown",
    "NotReady --> ShuttingDown : Shutdown",
    "Engineering --> ShuttingDown : Shutdown",
    "ShuttingDown --> [*]",
    "```"
  ].join("\n"));

  const geometry = await page.locator(".mermaid-diagram").evaluate(element => {
    const visibleRectangles = (selector: string) =>
      [...element.querySelectorAll<SVGGraphicsElement>(selector)]
        .map(item => item.getBoundingClientRect())
        .filter(rect => rect.width > 0 && rect.height > 0);
    const overlaps = (first: DOMRect, second: DOMRect) =>
      first.left < second.right - 1
      && first.right > second.left + 1
      && first.top < second.bottom - 1
      && first.bottom > second.top + 1;
    const states = visibleRectangles("g.stateGroup, g.node");
    const labelElements = [...element.querySelectorAll<SVGGraphicsElement>(
      "g.edgeLabel:not(:has(g.edgeLabel))"
    )].filter(item => item.textContent?.trim());
    const labels = labelElements.map(item => item.getBoundingClientRect());
    return {
      stateCount: states.length,
      labelCount: labels.length,
      labelCollisionCount: labels.flatMap((label, index) =>
        labels.slice(index + 1).filter(other => overlaps(label, other))).length,
      stateLabelCollisionCount: labels.flatMap(label =>
        states.filter(state => overlaps(label, state))).length
    };
  });

  expect(geometry.stateCount).toBeGreaterThanOrEqual(6);
  expect(geometry.labelCount).toBe(13);
  expect(geometry.labelCollisionCount).toBe(0);
  expect(geometry.stateLabelCollisionCount).toBe(0);

  await page.evaluate(() => {
    window.__sendHostMessage({
      type: "view.setMode",
      protocolVersion: 1,
      documentId: "browser-test",
      version: 1,
      payload: { mode: "split" }
    });
  });
  await expect(page.locator(".mermaid-state-diagram")).toBeVisible();

  await page.evaluate(() => {
    window.__sendHostMessage({
      type: "export.prepare",
      protocolVersion: 1,
      documentId: "browser-test",
      version: 1,
      payload: {}
    });
  });
  await expect.poll(() => page.evaluate(() =>
    window.__hostMessages.some((message: any) =>
      message.type === "export.ready" && message.version === 1
    )
  )).toBe(true);
  await expect(page.locator(".mermaid-state-diagram g.edgeLabel").filter({ hasText: "Shutdown" }))
    .toHaveCount(3);
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
