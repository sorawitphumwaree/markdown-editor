import { describe, expect, it } from "vitest";
import { resolveApplicationShortcut } from "./shortcuts";

describe("application shortcuts", () => {
  it.each([
    ["Tab", false, "nextTab"],
    ["Tab", true, "previousTab"],
    ["w", false, "closeTab"],
    ["t", false, "newTab"],
    ["n", false, "newTab"],
    ["o", false, "open"],
    ["s", false, "save"],
    ["s", true, "saveAs"]
  ])("maps Ctrl+%s with shift=%s to %s", (key, shift, command) => {
    expect(resolveApplicationShortcut(key, shift as boolean)).toBe(command);
  });
});
