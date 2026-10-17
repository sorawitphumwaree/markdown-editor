export function updateContents(
  preview: HTMLElement,
  list: HTMLElement,
  navigate: (heading: HTMLElement) => void
): void {
  const headings = preview.querySelectorAll<HTMLElement>("h1, h2");
  const items = [...headings].map(heading => {
    const button = list.ownerDocument.createElement("button");
    button.type = "button";
    button.textContent = heading.textContent?.trim() || "Untitled heading";
    button.title = button.textContent;
    button.dataset.level = heading.tagName === "H1" ? "1" : "2";
    button.addEventListener("click", () => navigate(heading));
    return button;
  });
  if (items.length) {
    list.replaceChildren(...items);
  } else {
    const empty = list.ownerDocument.createElement("p");
    empty.textContent = "No H1 or H2 headings";
    list.replaceChildren(empty);
  }
}
