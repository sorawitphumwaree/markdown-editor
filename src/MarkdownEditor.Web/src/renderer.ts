import MarkdownIt from "markdown-it";
import anchor from "markdown-it-anchor";
import footnote from "markdown-it-footnote";
import taskLists from "markdown-it-task-lists";
import DOMPurify from "dompurify";
import mermaid from "mermaid";
import { createHighlighter, type Highlighter } from "shiki";
import type Token from "markdown-it/lib/token.mjs";
import type { RenderState } from "./types";
import { initializeMermaidInteractions } from "./mermaid-interaction";

let state: RenderState = "idle";
let highlighterPromise: Promise<Highlighter> | undefined;
let renderSequence = 0;

const markdown = new MarkdownIt({
  html: true,
  linkify: true,
  typographer: false
}).use(anchor).use(footnote).use(taskLists, { enabled: true });

const mappedOpenRules = [
  "paragraph_open", "heading_open", "blockquote_open", "bullet_list_open",
  "ordered_list_open", "list_item_open", "table_open", "tr_open"
] as const;

for (const ruleName of mappedOpenRules) {
  const fallback = markdown.renderer.rules[ruleName];
  markdown.renderer.rules[ruleName] = (tokens, index, options, env, renderer) => {
    addSourceRange(tokens[index]);
    return fallback
      ? fallback(tokens, index, options, env, renderer)
      : renderer.renderToken(tokens, index, options);
  };
}

for (const ruleName of ["hr", "image"] as const) {
  const fallback = markdown.renderer.rules[ruleName];
  markdown.renderer.rules[ruleName] = (tokens, index, options, env, renderer) => {
    addSourceRange(tokens[index]);
    return fallback
      ? fallback(tokens, index, options, env, renderer)
      : renderer.renderToken(tokens, index, options);
  };
}

for (const ruleName of ["fence", "code_block"] as const) {
  const fallback = markdown.renderer.rules[ruleName];
  markdown.renderer.rules[ruleName] = (tokens, index, options, env, renderer) => {
    const token = tokens[index];
    const rendered = fallback
      ? fallback(tokens, index, options, env, renderer)
      : renderer.renderToken(tokens, index, options);
    if (!token?.map)
      return rendered;
    const attributes =
      ` data-source-start="${token.map[0] + 1}" data-source-end="${token.map[1]}"`;
    return rendered.replace("<pre", `<pre${attributes}`);
  };
}

function addSourceRange(token: Token | undefined): void {
  if (!token?.map)
    return;
  token.attrSet("data-source-start", String(token.map[0] + 1));
  token.attrSet("data-source-end", String(token.map[1]));
}

export async function renderMarkdown(
  source: string,
  preview: HTMLElement,
  theme: "light" | "dark",
  version: number,
  isCurrent: () => boolean = () => true
): Promise<number> {
  const renderId = ++renderSequence;
  state = "rendering-markdown";
  const html = markdown.render(source);
  const staging = preview.ownerDocument.createElement("div");
  staging.innerHTML = DOMPurify.sanitize(html, {
    USE_PROFILES: { html: true, svg: true, svgFilters: true },
    FORBID_TAGS: ["script", "iframe", "object", "embed"],
    FORBID_ATTR: ["style"],
    ADD_ATTR: ["data-source-start", "data-source-end", "class"]
  });

  state = "rendering-mermaid";
  mermaid.initialize({
    startOnLoad: false,
    suppressErrorRendering: true,
    securityLevel: "strict",
    htmlLabels: false,
    theme: theme === "dark" ? "dark" : "base",
    themeVariables: theme === "dark" ? undefined : {
      primaryColor: "#eee9fc",
      primaryTextColor: "#17181e",
      primaryBorderColor: "#6d4cc4",
      lineColor: "#6d4cc4",
      secondaryColor: "#f6f3fc",
      tertiaryColor: "#ffffff",
      fontFamily: '"Segoe UI", "Leelawadee UI", sans-serif'
    }
  });
  const diagrams = [...staging.querySelectorAll<HTMLElement>("pre > code.language-mermaid")];
  for (const [index, code] of diagrams.entries()) {
    const pre = code.parentElement!;
    try {
      const { svg } = await mermaid.render(
        `mermaid-${version}-${renderId}-${index}`,
        code.textContent ?? ""
      );
      const wrapper = document.createElement("div");
      wrapper.className = "mermaid-diagram";
      copySourceRange(pre, wrapper);
      wrapper.innerHTML = DOMPurify.sanitize(svg, {
        USE_PROFILES: { svg: true, svgFilters: true }
      });
      annotateMermaidSourceMap(
        wrapper,
        code.textContent ?? "",
        Number(pre.dataset.sourceStart ?? 1)
      );
      pre.replaceWith(wrapper);
    } catch (error) {
      pre.classList.add("render-error");
      pre.textContent = `Mermaid error: ${error instanceof Error ? error.message : String(error)}`;
    }
  }

  state = "highlighting-code";
  const codeBlocks = [...staging.querySelectorAll<HTMLElement>("pre > code")]
    .filter(code => !code.classList.contains("language-mermaid"));
  if (codeBlocks.length > 0) {
    const highlighter = await getHighlighter();
    for (const code of codeBlocks) {
      const language = [...code.classList]
        .find(item => item.startsWith("language-"))?.slice("language-".length) ?? "text";
      const loadedLanguage = highlighter.getLoadedLanguages().includes(language) ? language : "text";
      const highlighted = highlighter.codeToHtml(code.textContent ?? "", {
        lang: loadedLanguage,
        theme: theme === "dark" ? "github-dark" : "github-light"
      });
      const container = document.createElement("div");
      container.innerHTML = DOMPurify.sanitize(highlighted);
      const replacement = container.firstElementChild;
      if (replacement) {
        copySourceRange(code.parentElement!, replacement);
        annotateCodeLines(
          replacement,
          Number(code.parentElement!.getAttribute("data-source-start") ?? 1)
        );
        code.parentElement!.replaceWith(replacement);
      }
    }
  }

  if (!commitRenderedContent(preview, staging, isCurrent))
    return version;

  initializeMermaidInteractions(preview);

  state = "loading-assets";
  await waitForImages(preview);
  if (isCurrent())
    state = "ready";
  return version;
}

export function commitRenderedContent(
  preview: HTMLElement,
  staging: HTMLElement,
  isCurrent: () => boolean
): boolean {
  if (!isCurrent())
    return false;
  preview.replaceChildren(...staging.childNodes);
  return true;
}

export function getRenderState(): RenderState {
  return state;
}

export function findSourceBlock(preview: HTMLElement, line: number): HTMLElement | null {
  const blocks = [...preview.querySelectorAll<HTMLElement>("[data-source-start]")];
  const containing = blocks.filter(block => {
    const start = Number(block.dataset.sourceStart);
    const end = Number(block.dataset.sourceEnd ?? start);
    return start <= line && line <= end;
  });

  if (containing.length > 0) {
    return containing.reduce((best, current) => {
      const bestStart = Number(best.dataset.sourceStart);
      const bestEnd = Number(best.dataset.sourceEnd ?? bestStart);
      const currentStart = Number(current.dataset.sourceStart);
      const currentEnd = Number(current.dataset.sourceEnd ?? currentStart);
      const bestSpan = bestEnd - bestStart;
      const currentSpan = currentEnd - currentStart;

      if (currentSpan !== bestSpan)
        return currentSpan < bestSpan ? current : best;

      // When ranges are equally precise, prefer the nested rendered element
      // (for example a Mermaid node or highlighted code line) over its wrapper.
      return best.contains(current) ? current : best;
    });
  }

  return blocks.reduce<HTMLElement | null>((nearest, current) => {
    const currentStart = Number(current.dataset.sourceStart);
    if (currentStart > line)
      return nearest;
    if (!nearest || currentStart >= Number(nearest.dataset.sourceStart))
      return current;
    return nearest;
  }, null);
}

function getHighlighter(): Promise<Highlighter> {
  highlighterPromise ??= createHighlighter({
    themes: ["github-light", "github-dark"],
    langs: ["text", "csharp", "typescript", "javascript", "json", "yaml", "xml", "powershell", "sql"]
  });
  return highlighterPromise;
}

function copySourceRange(source: Element, destination: Element): void {
  for (const name of ["data-source-start", "data-source-end"]) {
    const value = source.getAttribute(name);
    if (value)
      destination.setAttribute(name, value);
  }
}

export function annotateCodeLines(container: Element, fenceStartLine: number): void {
  const lines = [...container.querySelectorAll<HTMLElement>(".line")];
  lines.forEach((line, index) => {
    const sourceLine = fenceStartLine + index + 1;
    line.dataset.sourceStart = String(sourceLine);
    line.dataset.sourceEnd = String(sourceLine);
  });
}

export function annotateMermaidSourceMap(
  container: Element,
  source: string,
  fenceStartLine: number
): void {
  const diagramType = source.match(/^\s*(flowchart|graph|sequenceDiagram|classDiagram)\b/i)?.[1]
    ?.toLowerCase();
  if (diagramType === "sequencediagram") {
    annotateSequenceDiagram(container, source, fenceStartLine);
    return;
  }
  if (diagramType === "classdiagram") {
    annotateClassDiagram(container, source, fenceStartLine);
    return;
  }
  annotateFlowchart(container, source, fenceStartLine);
}

function annotateFlowchart(
  container: Element,
  source: string,
  fenceStartLine: number
): void {
  const nodes = new Map<string, number>();
  const edgeLines: number[] = [];

  source.split(/\r?\n/).forEach((rawLine, index) => {
    const line = rawLine.replace(/%%.*$/, "");
    const sourceLine = fenceStartLine + index + 1;
    if (/(?:-->|---|-.->|==>|~~~|--o|--x)/.test(line))
      edgeLines.push(sourceLine);

    for (const id of extractMermaidNodeIds(line))
      if (!nodes.has(id))
        nodes.set(id, sourceLine);
  });

  for (const node of container.querySelectorAll<HTMLElement>("g.node")) {
    const generatedId = node.id;
    const dataId = node.dataset.id;
    const sourceEntry = [...nodes].find(([id]) =>
      dataId === id
      || generatedId === id
      || generatedId.includes(`-${id}-`)
      || generatedId.endsWith(`-${id}`));
    if (sourceEntry)
      setSingleSourceLine(node, sourceEntry[1]);
  }

  const renderedEdges = [...container.querySelectorAll<HTMLElement>(".edgePath")];
  const renderedLabels = [...container.querySelectorAll<HTMLElement>(".edgeLabel")];
  edgeLines.forEach((line, index) => {
    if (renderedEdges[index])
      setSingleSourceLine(renderedEdges[index], line);
    if (renderedLabels[index])
      setSingleSourceLine(renderedLabels[index], line);
  });
}

export function annotateSequenceDiagram(
  container: Element,
  source: string,
  fenceStartLine: number
): void {
  const participants = new Map<string, number>();
  const messageLines: number[] = [];
  const noteLines: number[] = [];
  const controlLines: number[] = [];

  source.split(/\r?\n/).forEach((rawLine, index) => {
    const line = rawLine.replace(/%%.*$/, "").trim();
    const sourceLine = fenceStartLine + index + 1;
    const declaration = line.match(/^(?:participant|actor)\s+([^\s]+)(?:\s+as\s+(.+))?$/i);
    if (declaration) {
      participants.set(declaration[1]!, sourceLine);
      if (declaration[2])
        participants.set(normalizeLabel(declaration[2]), sourceLine);
      return;
    }

    const message = line.match(
      /^([A-Za-z_][\w.-]*)\s*(?:--?>>?|->>?|--x|-x|--\)|-\)|<<->>|<->>)\s*([A-Za-z_][\w.-]*)\s*:/
    );
    if (message) {
      messageLines.push(sourceLine);
      if (!participants.has(message[1]!))
        participants.set(message[1]!, sourceLine);
      if (!participants.has(message[2]!))
        participants.set(message[2]!, sourceLine);
      return;
    }

    if (/^note\s+(?:right of|left of|over)\b/i.test(line)) {
      noteLines.push(sourceLine);
      return;
    }
    if (/^(?:loop|alt|else|opt|par|and|critical|option|break|rect)\b/i.test(line))
      controlLines.push(sourceLine);
  });

  for (const actor of container.querySelectorAll<HTMLElement>(
    '[data-et="participant"], [data-et="life-line"]'
  )) {
    const id = actor.dataset.id ?? actor.getAttribute("name") ?? "";
    const line = participants.get(id) ?? participants.get(normalizeLabel(actor.textContent ?? ""));
    if (line)
      setSingleSourceLine(actor, line);
  }

  annotateElementsByOrder(
    container.querySelectorAll<HTMLElement>('[data-et="message"]'),
    messageLines
  );
  annotateElementsByOrder(
    container.querySelectorAll<HTMLElement>(".messageText"),
    messageLines
  );
  annotateElementsByOrder(
    container.querySelectorAll<HTMLElement>('[data-et="note"]'),
    noteLines
  );
  annotateElementsByOrder(
    container.querySelectorAll<HTMLElement>(".noteText"),
    noteLines
  );
  annotateElementsByOrder(
    container.querySelectorAll<HTMLElement>(".loopText, .labelText"),
    controlLines
  );
}

export function annotateClassDiagram(
  container: Element,
  source: string,
  fenceStartLine: number
): void {
  const classes = new Map<string, number>();
  const relationshipLines: number[] = [];
  const members: Array<{ classId: string; text: string; line: number }> = [];
  let currentClass: string | undefined;

  source.split(/\r?\n/).forEach((rawLine, index) => {
    const line = rawLine.replace(/%%.*$/, "").trim();
    const sourceLine = fenceStartLine + index + 1;
    const classDeclaration = line.match(/^class\s+([A-Za-z_][\w.-]*)/i);
    if (classDeclaration) {
      currentClass = classDeclaration[1]!;
      classes.set(currentClass, sourceLine);
      if (!line.includes("{"))
        currentClass = undefined;
      return;
    }
    if (line === "}") {
      currentClass = undefined;
      return;
    }
    if (currentClass && line) {
      members.push({ classId: currentClass, text: line, line: sourceLine });
      return;
    }

    const colonMember = line.match(/^([A-Za-z_][\w.-]*)\s*:\s*(.+)$/);
    if (colonMember) {
      const classId = colonMember[1]!;
      if (!classes.has(classId))
        classes.set(classId, sourceLine);
      members.push({ classId, text: colonMember[2]!, line: sourceLine });
      return;
    }

    const relationship = line.match(
      /^([A-Za-z_][\w.-]*)\s+(?:<\|--|--\|>|<\|\.\.|\.\.\|>|-->|<--|<-->|--|o--|--o|\*--|--\*|\.\.>|\.\.|<\.\.)\s+([A-Za-z_][\w.-]*)/
    );
    if (relationship) {
      relationshipLines.push(sourceLine);
      if (!classes.has(relationship[1]!))
        classes.set(relationship[1]!, sourceLine);
      if (!classes.has(relationship[2]!))
        classes.set(relationship[2]!, sourceLine);
    }
  });

  const renderedClasses = container.querySelectorAll<HTMLElement>("g.node, g.classGroup");
  for (const renderedClass of renderedClasses) {
    const classEntry = [...classes].find(([id]) =>
      elementMatchesIdentifier(renderedClass, id)
    );
    if (!classEntry)
      continue;
    setSingleSourceLine(renderedClass, classEntry[1]);

    for (const member of members.filter(item => item.classId === classEntry[0])) {
      const normalizedMember = normalizeLabel(member.text);
      const memberElement = [...renderedClass.querySelectorAll<HTMLElement>(
        "text, span, p, div"
      )].find(element => normalizeLabel(element.textContent ?? "") === normalizedMember);
      if (memberElement)
        setSingleSourceLine(memberElement, member.line);
    }
  }

  annotateElementsByOrder(
    container.querySelectorAll<HTMLElement>(".edgePath, [data-et=\"relation\"]"),
    relationshipLines
  );
  annotateElementsByOrder(
    container.querySelectorAll<HTMLElement>(".edgeLabel"),
    relationshipLines
  );
}

export function extractMermaidNodeIds(line: string): string[] {
  const ignored = new Set([
    "flowchart", "graph", "subgraph", "end", "direction", "style",
    "classDef", "class", "click", "linkStyle"
  ]);
  const ids = new Set<string>();
  const definitionPattern = /\b([A-Za-z_][\w-]*)\s*(?=\[\[?|\(\(?|\{\{?|>)/g;
  for (const match of line.matchAll(definitionPattern)) {
    const id = match[1]!;
    if (!ignored.has(id))
      ids.add(id);
  }

  const arrowPattern =
    /([A-Za-z_][\w-]*)[^;\n]*?(?:-->|---|-.->|==>|~~~|--o|--x)\s*(?:\|[^|]*\|\s*)?([A-Za-z_][\w-]*)/g;
  for (const match of line.matchAll(arrowPattern)) {
    ids.add(match[1]!);
    ids.add(match[2]!);
  }
  return [...ids];
}

function setSingleSourceLine(element: HTMLElement, line: number): void {
  element.dataset.sourceStart = String(line);
  element.dataset.sourceEnd = String(line);
}

function annotateElementsByOrder(elements: NodeListOf<HTMLElement>, lines: number[]): void {
  [...elements].forEach((element, index) => {
    const line = lines[index];
    if (line)
      setSingleSourceLine(element, line);
  });
}

function elementMatchesIdentifier(element: HTMLElement, id: string): boolean {
  const generatedId = element.id;
  const dataId = element.dataset.id;
  return dataId === id
    || generatedId === id
    || generatedId.includes(`-${id}-`)
    || generatedId.endsWith(`-${id}`)
    || normalizeLabel(element.textContent ?? "").startsWith(normalizeLabel(id));
}

const normalizeLabel = (value: string): string =>
  value.replace(/<br\s*\/?>/gi, " ").replace(/\s+/g, "").trim();

function waitForImages(container: HTMLElement): Promise<void> {
  const pending = [...container.querySelectorAll("img")]
    .filter(image => !image.complete)
    .map(image => new Promise<void>(resolve => {
      image.addEventListener("load", () => resolve(), { once: true });
      image.addEventListener("error", () => resolve(), { once: true });
    }));
  return Promise.all(pending).then(() => undefined);
}
