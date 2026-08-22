const MIN_SCALE = 0.5;
const MAX_SCALE = 4;
const SCALE_STEP = 0.25;

interface DiagramState {
  scale: number;
  baseWidth: number;
  scrollLeft: number;
  scrollTop: number;
}

export function initializeMermaidInteractions(container: ParentNode): void {
  for (const diagram of container.querySelectorAll<HTMLElement>(".mermaid-diagram"))
    initializeDiagram(diagram);
}

export function initializeDiagram(diagram: HTMLElement): void {
  if (diagram.dataset.interactive === "true")
    return;
  const svg = diagram.querySelector<SVGSVGElement>(":scope > svg");
  if (!svg)
    return;

  diagram.dataset.interactive = "true";
  diagram.tabIndex = 0;
  diagram.setAttribute("aria-label", "Interactive Mermaid diagram");

  const toolbar = diagram.ownerDocument.createElement("div");
  toolbar.className = "mermaid-toolbar";
  toolbar.setAttribute("role", "toolbar");
  toolbar.setAttribute("aria-label", "Mermaid diagram zoom controls");
  const zoomOut = createButton(diagram, "−", "Zoom out");
  const zoomLabel = diagram.ownerDocument.createElement("span");
  zoomLabel.className = "mermaid-zoom-label";
  const zoomIn = createButton(diagram, "+", "Zoom in");
  const reset = createButton(diagram, "Reset", "Reset zoom and pan");
  toolbar.append(zoomOut, zoomLabel, zoomIn, reset);
  diagram.prepend(toolbar);

  const baseWidth = Math.max(1, svg.getBoundingClientRect().width);
  const state: DiagramState = { scale: 1, baseWidth, scrollLeft: 0, scrollTop: 0 };
  let panStart: { x: number; y: number } | undefined;
  let suppressClick = false;
  const applyScale = (nextScale: number, anchorX?: number, anchorY?: number) => {
    const previousScale = state.scale;
    state.scale = Math.min(MAX_SCALE, Math.max(MIN_SCALE, nextScale));
    svg.style.width = `${state.baseWidth * state.scale}px`;
    svg.style.maxWidth = "none";
    svg.style.height = "auto";
    zoomLabel.textContent = `${Math.round(state.scale * 100)}%`;
    zoomOut.disabled = state.scale <= MIN_SCALE;
    zoomIn.disabled = state.scale >= MAX_SCALE;
    if (anchorX !== undefined && anchorY !== undefined && previousScale !== state.scale) {
      const ratio = state.scale / previousScale;
      diagram.scrollLeft = (diagram.scrollLeft + anchorX) * ratio - anchorX;
      diagram.scrollTop = (diagram.scrollTop + anchorY) * ratio - anchorY;
    }
    diagram.classList.toggle("is-zoomed", state.scale > 1);
  };

  zoomOut.addEventListener("click", event => {
    event.stopPropagation();
    applyScale(state.scale - SCALE_STEP);
  });
  zoomIn.addEventListener("click", event => {
    event.stopPropagation();
    applyScale(state.scale + SCALE_STEP);
  });
  reset.addEventListener("click", event => {
    event.stopPropagation();
    applyScale(1);
    diagram.scrollTo({ left: 0, top: 0 });
  });
  toolbar.addEventListener("pointerdown", event => event.stopPropagation());

  diagram.addEventListener("wheel", event => {
    if (!event.ctrlKey)
      return;
    event.preventDefault();
    const bounds = diagram.getBoundingClientRect();
    applyScale(
      state.scale + (event.deltaY < 0 ? SCALE_STEP : -SCALE_STEP),
      event.clientX - bounds.left,
      event.clientY - bounds.top
    );
  }, { passive: false });

  diagram.addEventListener("pointerdown", event => {
    if (event.button !== 0 || state.scale <= 1 || toolbar.contains(event.target as Node))
      return;
    state.scrollLeft = diagram.scrollLeft + event.clientX;
    state.scrollTop = diagram.scrollTop + event.clientY;
    panStart = { x: event.clientX, y: event.clientY };
    diagram.classList.add("is-panning");
    diagram.setPointerCapture(event.pointerId);
    event.preventDefault();
  });
  diagram.addEventListener("pointermove", event => {
    if (!diagram.hasPointerCapture(event.pointerId))
      return;
    if (panStart && (Math.abs(event.clientX - panStart.x) > 3
        || Math.abs(event.clientY - panStart.y) > 3))
      suppressClick = true;
    diagram.scrollLeft = state.scrollLeft - event.clientX;
    diagram.scrollTop = state.scrollTop - event.clientY;
  });
  const finishPan = (event: PointerEvent) => {
    if (diagram.hasPointerCapture(event.pointerId))
      diagram.releasePointerCapture(event.pointerId);
    diagram.classList.remove("is-panning");
    panStart = undefined;
  };
  diagram.addEventListener("pointerup", finishPan);
  diagram.addEventListener("pointercancel", finishPan);
  diagram.addEventListener("click", event => {
    if (!suppressClick)
      return;
    suppressClick = false;
    event.preventDefault();
    event.stopPropagation();
  }, { capture: true });

  applyScale(1);
}

function createButton(diagram: HTMLElement, text: string, label: string): HTMLButtonElement {
  const button = diagram.ownerDocument.createElement("button");
  button.type = "button";
  button.textContent = text;
  button.setAttribute("aria-label", label);
  button.title = label;
  return button;
}
