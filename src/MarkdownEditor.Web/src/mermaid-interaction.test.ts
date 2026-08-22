import { JSDOM } from "jsdom";
import { describe, expect, it, vi } from "vitest";
import { initializeDiagram } from "./mermaid-interaction";

function createDiagram(): { dom: JSDOM; diagram: HTMLElement; svg: SVGSVGElement } {
  const dom = new JSDOM('<div class="mermaid-diagram"><svg viewBox="0 0 800 400"></svg></div>');
  const diagram = dom.window.document.querySelector<HTMLElement>(".mermaid-diagram")!;
  const svg = diagram.querySelector<SVGSVGElement>("svg")!;
  vi.spyOn(svg, "getBoundingClientRect").mockReturnValue({
    width: 800, height: 400, top: 0, left: 0, right: 800, bottom: 400, x: 0, y: 0,
    toJSON: () => ({})
  });
  (diagram as any).scrollTo = ({ left, top }: ScrollToOptions) => {
    diagram.scrollLeft = Number(left);
    diagram.scrollTop = Number(top);
  };
  return { dom, diagram, svg };
}

describe("Mermaid interaction", () => {
  it("zooms one diagram and resets its scale and pan", () => {
    const { diagram, svg } = createDiagram();
    initializeDiagram(diagram);
    const zoomIn = diagram.querySelector<HTMLButtonElement>('[aria-label="Zoom in"]')!;
    const reset = diagram.querySelector<HTMLButtonElement>('[aria-label="Reset zoom and pan"]')!;

    zoomIn.click();
    expect(svg.style.width).toBe("1000px");
    expect(diagram.querySelector(".mermaid-zoom-label")?.textContent).toBe("125%");

    diagram.scrollLeft = 120;
    diagram.scrollTop = 80;
    reset.click();
    expect(svg.style.width).toBe("800px");
    expect(diagram.scrollLeft).toBe(0);
    expect(diagram.scrollTop).toBe(0);
  });

  it("only captures the wheel gesture when Control requests diagram zoom", () => {
    const { dom, diagram, svg } = createDiagram();
    initializeDiagram(diagram);
    const ordinaryWheel = new dom.window.WheelEvent("wheel", { deltaY: -1, cancelable: true });
    diagram.dispatchEvent(ordinaryWheel);
    expect(ordinaryWheel.defaultPrevented).toBe(false);
    expect(svg.style.width).toBe("800px");

    const zoomWheel = new dom.window.WheelEvent("wheel", {
      deltaY: -1, ctrlKey: true, cancelable: true, clientX: 100, clientY: 100
    });
    diagram.dispatchEvent(zoomWheel);
    expect(zoomWheel.defaultPrevented).toBe(true);
    expect(svg.style.width).toBe("1000px");
  });
});
