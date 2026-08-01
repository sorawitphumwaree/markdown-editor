import { describe, expect, it } from "vitest";
import { JSDOM } from "jsdom";
import {
  annotateClassDiagram,
  annotateCodeLines,
  annotateMermaidSourceMap,
  annotateSequenceDiagram,
  commitRenderedContent,
  extractMermaidNodeIds,
  findSourceBlock
} from "./renderer";

describe("source mapping", () => {
  it("keeps explicit source metadata selectable", () => {
    const dom = new JSDOM('<p data-source-start="2" data-source-end="4">Hello</p>');
    const element = dom.window.document.querySelector("[data-source-start]");
    expect(element?.getAttribute("data-source-end")).toBe("4");
  });

  it("prefers an exact rendered line over its containing block", () => {
    const dom = new JSDOM(`
      <main id="preview">
        <pre data-source-start="7" data-source-end="10">
          <code>
            <span class="line" data-source-start="8" data-source-end="8">one</span>
            <span class="line" data-source-start="9" data-source-end="9">two</span>
          </code>
        </pre>
      </main>`);
    const preview = dom.window.document.querySelector<HTMLElement>("#preview")!;

    expect(findSourceBlock(preview, 9)?.textContent).toBe("two");
  });

  it("prefers a mapped Mermaid element over its diagram wrapper", () => {
    const dom = new JSDOM(`
      <main id="preview">
        <div data-source-start="10" data-source-end="15">
          <svg>
            <g id="message" data-source-start="13" data-source-end="13"></g>
          </svg>
        </div>
      </main>`);
    const preview = dom.window.document.querySelector<HTMLElement>("#preview")!;

    expect(findSourceBlock(preview, 13)?.id).toBe("message");
  });

  it("does not commit a render that became stale", () => {
    const dom = new JSDOM('<main id="preview"><p>current</p></main><div id="staging"><p>stale</p></div>');
    const preview = dom.window.document.querySelector<HTMLElement>("#preview")!;
    const staging = dom.window.document.querySelector<HTMLElement>("#staging")!;

    const committed = commitRenderedContent(preview, staging, () => false);

    expect(committed).toBe(false);
    expect(preview.textContent).toBe("current");
  });

  it("maps highlighted code lines inside a fenced block", () => {
    const dom = new JSDOM('<pre><code><span class="line">one</span><span class="line">two</span></code></pre>');
    const pre = dom.window.document.querySelector("pre")!;

    annotateCodeLines(pre, 7);

    const lines = [...pre.querySelectorAll<HTMLElement>(".line")];
    expect(lines[0]!.dataset.sourceStart).toBe("8");
    expect(lines[1]!.dataset.sourceStart).toBe("9");
  });

  it("maps Mermaid nodes and edges to their defining source lines", () => {
    const dom = new JSDOM(`
      <svg>
        <g class="node" id="flowchart-A-0"></g>
        <g class="node" id="flowchart-B-1"></g>
        <g class="edgePath"></g>
      </svg>`);
    const svg = dom.window.document.querySelector("svg")!;

    annotateMermaidSourceMap(svg, "flowchart TD\nA[Start] --> B{Check}", 10);

    expect(svg.querySelector<HTMLElement>("#flowchart-A-0")?.dataset.sourceStart).toBe("12");
    expect(svg.querySelector<HTMLElement>("#flowchart-B-1")?.dataset.sourceStart).toBe("12");
    expect(svg.querySelector<HTMLElement>(".edgePath")?.dataset.sourceStart).toBe("12");
  });

  it("extracts Mermaid identifiers without treating keywords as nodes", () => {
    expect(extractMermaidNodeIds("flowchart LR")).toEqual([]);
    expect(extractMermaidNodeIds("A[Start] --> B{Ready?}")).toEqual(["A", "B"]);
  });

  it("maps Sequence participants, messages, and notes", () => {
    const dom = new JSDOM(`
      <svg>
        <g data-et="participant" data-id="Alice"></g>
        <line data-et="life-line" data-id="Bob"></line>
        <line class="messageLine0" data-et="message"></line>
        <text class="messageText">Hello</text>
        <g data-et="note"><text class="noteText">Remember</text></g>
      </svg>`);
    const svg = dom.window.document.querySelector("svg")!;

    annotateSequenceDiagram(
      svg,
      "sequenceDiagram\nparticipant Alice\nparticipant Bob\nAlice->>Bob: Hello\nNote over Bob: Remember",
      20
    );

    expect(svg.querySelector<HTMLElement>('[data-et="participant"]')?.dataset.sourceStart).toBe("22");
    expect(svg.querySelector<HTMLElement>('[data-et="life-line"]')?.dataset.sourceStart).toBe("23");
    expect(svg.querySelector<HTMLElement>('[data-et="message"]')?.dataset.sourceStart).toBe("24");
    expect(svg.querySelector<HTMLElement>(".messageText")?.dataset.sourceStart).toBe("24");
    expect(svg.querySelector<HTMLElement>('[data-et="note"]')?.dataset.sourceStart).toBe("25");
  });

  it("maps Class nodes, members, and relationships", () => {
    const dom = new JSDOM(`
      <svg>
        <g class="node" id="classId-Animal-0">
          <text>Animal</text><text>+String name</text>
        </g>
        <g class="node" id="classId-Dog-1"><text>Dog</text></g>
        <path class="edgePath"></path>
      </svg>`);
    const svg = dom.window.document.querySelector("svg")!;

    annotateClassDiagram(
      svg,
      "classDiagram\nclass Animal {\n+String name\n}\nclass Dog\nAnimal <|-- Dog",
      30
    );

    expect(svg.querySelector<HTMLElement>("#classId-Animal-0")?.dataset.sourceStart).toBe("32");
    expect([...svg.querySelectorAll<HTMLElement>("text")]
      .find(item => item.textContent === "+String name")?.dataset.sourceStart).toBe("33");
    expect(svg.querySelector<HTMLElement>("#classId-Dog-1")?.dataset.sourceStart).toBe("35");
    expect(svg.querySelector<HTMLElement>(".edgePath")?.dataset.sourceStart).toBe("36");
  });
});
