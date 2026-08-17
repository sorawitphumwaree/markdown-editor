import { EditorState } from "@codemirror/state";
import { EditorView, keymap, lineNumbers, highlightActiveLine } from "@codemirror/view";
import { defaultKeymap, history, historyKeymap, indentWithTab } from "@codemirror/commands";
import { markdown as markdownLanguage } from "@codemirror/lang-markdown";
import { searchKeymap } from "@codemirror/search";
import { listen, post } from "./messaging";
import { findSourceBlock, getRenderState, renderMarkdown } from "./renderer";
import { resolveApplicationShortcut } from "./shortcuts";
import { isEligibleTranslationSelection, mapTranslationPayload } from "./translation";
import type { AppMessage, DocumentPayload } from "./types";
import "./styles.css";

const editorHost = document.querySelector<HTMLElement>("#editor")!;
const preview = document.querySelector<HTMLElement>("#preview")!;
const workspace = document.querySelector<HTMLElement>("#workspace")!;
const divider = document.querySelector<HTMLElement>("#divider")!;
const translationMenu = document.querySelector<HTMLElement>("#translation-menu")!;
const translateCommand = document.querySelector<HTMLButtonElement>("#translate-command")!;
const translationPopup = document.querySelector<HTMLElement>("#translation-popup")!;
const translationClose = document.querySelector<HTMLButtonElement>("#translation-close")!;

let documentId: string | undefined;
let documentVersion = 0;
const theme = "light";
let renderTimer: number | undefined;
let applyingNativeUpdate = false;
let currentMode: "preview" | "split" = "preview";
let modeBeforeExport: "preview" | "split" = "preview";
let editorScrollFrame: number | undefined;
let previewScrollFrame: number | undefined;
let renderGeneration = 0;
let translationRequestId: string | undefined;
let translationSelection: { word: string; anchor: { left: number; top: number } } | undefined;

document.documentElement.dataset.theme = theme;

const editor = new EditorView({
  state: EditorState.create({
    doc: "",
    extensions: [
      lineNumbers(),
      highlightActiveLine(),
      history(),
      markdownLanguage(),
      EditorView.lineWrapping,
      keymap.of([...defaultKeymap, ...historyKeymap, ...searchKeymap, indentWithTab]),
      EditorView.updateListener.of(update => {
        if (update.docChanged && !applyingNativeUpdate) {
          documentVersion++;
          const content = update.state.doc.toString();
          post("document.changed", documentId, documentVersion, { content });
          scheduleRender(content, documentVersion);
        }
        if (update.selectionSet) {
          const line = update.state.doc.lineAt(update.state.selection.main.head).number;
          post("editor.cursorChanged", documentId, documentVersion, { line });
        }
      }),
      EditorView.domEventHandlers({
        click: () => {
          window.setTimeout(() => {
            const line = editor.state.doc.lineAt(editor.state.selection.main.head).number;
            synchronizeToPreview(line);
          });
        },
        scroll: () => {
          window.cancelAnimationFrame(editorScrollFrame ?? 0);
          editorScrollFrame = window.requestAnimationFrame(() => {
            post("editor.scrollChanged", documentId, documentVersion, {
              top: editor.scrollDOM.scrollTop
            });
          });
        },
        contextmenu: (event, view) => {
          const selection = view.state.selection.main;
          const word = view.state.sliceDoc(selection.from, selection.to);
          const pointerPosition = view.posAtCoords({ x: event.clientX, y: event.clientY });
          if (selection.empty || pointerPosition === null
              || pointerPosition < selection.from || pointerPosition > selection.to
              || !isEligibleTranslationSelection(word)) {
            hideTranslationMenu();
            return false;
          }
          event.preventDefault();
          hideTranslationPopup();
          const end = view.coordsAtPos(selection.to);
          translationSelection = {
            word,
            anchor: { left: end?.left ?? event.clientX, top: end?.bottom ?? event.clientY }
          };
          placeOverlay(translationMenu, event.clientX, event.clientY);
          translationMenu.hidden = false;
          translateCommand.focus();
          return true;
        }
      })
    ]
  }),
  parent: editorHost
});

listen(message => void handleMessage(message));
post("web.ready", undefined, undefined, {});

async function handleMessage(message: AppMessage): Promise<void> {
  switch (message.type) {
    case "document.load": {
      const payload = message.payload as DocumentPayload;
      documentId = message.documentId;
      documentVersion = message.version ?? 0;
      const loadingDocumentId = documentId;
      const loadingVersion = documentVersion;
      const loadingGeneration = ++renderGeneration;
      applyingNativeUpdate = true;
      editor.dispatch({
        changes: { from: 0, to: editor.state.doc.length, insert: payload.content }
      });
      const cursorLine = editor.state.doc.line(
        Math.max(1, Math.min(payload.cursorLine, editor.state.doc.lines))
      );
      editor.dispatch({ selection: { anchor: cursorLine.from } });
      applyingNativeUpdate = false;
      setMode(payload.viewMode);
      await renderMarkdown(
        payload.content,
        preview,
        theme,
        loadingVersion,
        () => isCurrentRender(loadingDocumentId, loadingVersion, loadingGeneration)
      );
      if (!isCurrentRender(loadingDocumentId, loadingVersion, loadingGeneration))
        break;
      restoreViewports(payload.editorScroll, payload.previewScroll);
      post("preview.renderCompleted", documentId, documentVersion, { state: getRenderState() });
      break;
    }
    case "document.clear":
      clearTimeout(renderTimer);
      renderGeneration++;
      documentId = undefined;
      documentVersion = 0;
      applyingNativeUpdate = true;
      editor.dispatch({
        changes: { from: 0, to: editor.state.doc.length, insert: "" }
      });
      applyingNativeUpdate = false;
      preview.replaceChildren();
      editor.scrollDOM.scrollTop = 0;
      preview.scrollTop = 0;
      break;
    case "view.setMode":
      setMode((message.payload as { mode: "preview" | "split" }).mode);
      break;
    case "translation.loading":
    case "translation.completed":
    case "translation.failed": {
      if (message.requestId !== translationRequestId)
        break;
      const payload = message.payload as import("./translation").TranslationPayload
        & { anchor: { left: number; top: number } };
      const mapped = mapTranslationPayload(payload);
      document.querySelector<HTMLElement>("#translation-word")!.textContent = mapped.heading;
      document.querySelector<HTMLElement>("#translation-languages")!.textContent = mapped.languagePair;
      document.querySelector<HTMLElement>("#translation-pos")!.textContent =
        message.type === "translation.loading" ? "Looking up…" : mapped.partOfSpeech;
      document.querySelector<HTMLElement>("#translation-result")!.textContent =
        message.type === "translation.loading" ? "Translating selected word…" : mapped.result;
      translationPopup.dataset.state = message.type.split(".")[1];
      translationPopup.hidden = false;
      placeOverlay(translationPopup, payload.anchor.left, payload.anchor.top);
      break;
    }
    case "preview.navigateFragment": {
      const fragment = (message.payload as { fragment: string }).fragment;
      document.getElementById(fragment)?.scrollIntoView({ block: "start" });
      break;
    }
    case "export.prepare":
      modeBeforeExport = currentMode;
      setMode("preview");
      document.documentElement.classList.add("exporting");
      await renderPreservingViewports(
        editor.state.doc.toString(), "light", documentVersion
      );
      await document.fonts.ready;
      post("export.ready", documentId, documentVersion, { state: getRenderState() });
      break;
    case "export.complete":
      document.documentElement.classList.remove("exporting");
      setMode(modeBeforeExport);
      await renderPreservingViewports(
        editor.state.doc.toString(), theme, documentVersion
      );
      break;
  }
}

function scheduleRender(content: string, version: number): void {
  clearTimeout(renderTimer);
  renderTimer = window.setTimeout(async () => {
    try {
      const completedVersion = await renderPreservingViewports(content, theme, version);
      if (completedVersion === documentVersion)
        post("preview.renderCompleted", documentId, completedVersion, { state: getRenderState() });
    } catch (error) {
      post("preview.renderFailed", documentId, version, {
        message: error instanceof Error ? error.message : String(error)
      });
    }
  }, 200);
}

function synchronizeToPreview(line: number): void {
  const block = findSourceBlock(preview, line);
  if (!block)
    return;
  block.scrollIntoView({ block: "center", behavior: "smooth" });
  block.classList.add("source-active");
  window.setTimeout(() => {
    block.classList.remove("source-active");
  }, 250);
}

async function renderPreservingViewports(
  content: string,
  renderTheme: "light" | "dark",
  version: number
): Promise<number> {
  const renderingDocumentId = documentId;
  const renderingGeneration = ++renderGeneration;
  const editorTop = editor.scrollDOM.scrollTop;
  const previewTop = preview.scrollTop;
  const completedVersion = await renderMarkdown(
    content,
    preview,
    renderTheme,
    version,
    () => isCurrentRender(renderingDocumentId, version, renderingGeneration)
  );
  if (isCurrentRender(renderingDocumentId, version, renderingGeneration))
    restoreViewports(editorTop, previewTop);
  return completedVersion;
}

function isCurrentRender(
  renderingDocumentId: string | undefined,
  version: number,
  generation: number
): boolean {
  return renderingDocumentId === documentId
    && version === documentVersion
    && generation === renderGeneration;
}

function restoreViewports(editorTop: number, previewTop: number): void {
  editor.scrollDOM.scrollTop = editorTop;
  preview.scrollTop = previewTop;
  window.requestAnimationFrame(() => {
    editor.scrollDOM.scrollTop = editorTop;
    preview.scrollTop = previewTop;
  });
}

preview.addEventListener("click", event => {
  const target = event.target as HTMLElement;
  const link = target.closest<HTMLAnchorElement>("a[href]");
  if (link) {
    event.preventDefault();
    post("preview.linkClicked", documentId, documentVersion, { href: link.getAttribute("href") });
    return;
  }
  const block = target.closest<HTMLElement>("[data-source-start]");
  if (!block)
    return;
  const lineNumber = Number(block.dataset.sourceStart);
  const line = editor.state.doc.line(Math.min(lineNumber, editor.state.doc.lines));
  editor.dispatch({ selection: { anchor: line.from }, scrollIntoView: true });
  editor.focus();
});

preview.addEventListener("scroll", () => {
  window.cancelAnimationFrame(previewScrollFrame ?? 0);
  previewScrollFrame = window.requestAnimationFrame(() => {
    post("preview.scrollChanged", documentId, documentVersion, {
      top: preview.scrollTop
    });
  });
}, { passive: true });

window.addEventListener("keydown", event => {
  if (event.key === "Escape") {
    hideTranslationMenu();
    hideTranslationPopup();
    return;
  }
  if (!event.ctrlKey)
    return;
  const command = resolveApplicationShortcut(event.key, event.shiftKey);
  if (!command)
    return;
  event.preventDefault();
  post("application.shortcut", documentId, documentVersion, { command });
});

translateCommand.addEventListener("click", () => {
  if (!translationSelection)
    return;
  hideTranslationMenu();
  translationRequestId = crypto.randomUUID();
  post("translation.requested", documentId, documentVersion, {
    word: translationSelection.word,
    anchor: translationSelection.anchor
  });
});

translationClose.addEventListener("click", hideTranslationPopup);
document.addEventListener("pointerdown", event => {
  const target = event.target as Node;
  if (!translationMenu.contains(target))
    hideTranslationMenu();
  if (!translationPopup.hidden && !translationPopup.contains(target))
    hideTranslationPopup();
});

function hideTranslationMenu(): void {
  translationMenu.hidden = true;
}

function hideTranslationPopup(): void {
  translationPopup.hidden = true;
  translationRequestId = undefined;
}

function placeOverlay(element: HTMLElement, left: number, top: number): void {
  element.style.left = `${Math.max(8, left)}px`;
  element.style.top = `${Math.max(8, top + 6)}px`;
  window.requestAnimationFrame(() => {
    const bounds = element.getBoundingClientRect();
    element.style.left = `${Math.max(8, Math.min(left, window.innerWidth - bounds.width - 8))}px`;
    element.style.top = `${Math.max(8, Math.min(top + 6, window.innerHeight - bounds.height - 8))}px`;
  });
}

let dragStart: { x: number; ratio: number } | undefined;
divider.addEventListener("pointerdown", event => {
  dragStart = { x: event.clientX, ratio: editorHost.getBoundingClientRect().width / workspace.clientWidth };
  divider.setPointerCapture(event.pointerId);
});
divider.addEventListener("pointermove", event => {
  if (!dragStart)
    return;
  const ratio = Math.min(0.8, Math.max(0.2,
    dragStart.ratio + (event.clientX - dragStart.x) / workspace.clientWidth));
  workspace.style.setProperty("--split", `${ratio * 100}%`);
});
divider.addEventListener("pointerup", () => { dragStart = undefined; });

function setMode(mode: "preview" | "split"): void {
  currentMode = mode;
  workspace.dataset.mode = mode;
}
