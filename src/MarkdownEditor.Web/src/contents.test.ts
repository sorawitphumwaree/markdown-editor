import { expect, it } from "vitest";
import { JSDOM } from "jsdom";
import { updateContents } from "./contents";

it("lists only H1/H2 in document order with plain text and distinct navigation targets", () => {
  const dom = new JSDOM(`<article><h1>Same</h1><h3>Excluded</h3>
    <h2><em>Formatted</em> heading</h2><h1>Same</h1><h4>Excluded</h4></article><nav></nav>`);
  const preview = dom.window.document.querySelector("article")!;
  const list = dom.window.document.querySelector("nav")!;
  const visited: HTMLElement[] = [];
  updateContents(preview, list, heading => visited.push(heading));
  const buttons = [...list.querySelectorAll("button")];
  expect(buttons.map(button => button.textContent)).toEqual(["Same", "Formatted heading", "Same"]);
  expect(buttons.map(button => button.dataset.level)).toEqual(["1", "2", "1"]);
  buttons[2]!.click();
  expect(visited).toEqual([preview.querySelectorAll("h1")[1]]);
  preview.replaceChildren();
  updateContents(preview, list, () => {});
  expect(list.querySelectorAll("button")).toHaveLength(0);
  expect(list.textContent).toBe("No H1 or H2 headings");
});
