export type ApplicationShortcut =
  | "nextTab"
  | "previousTab"
  | "closeTab"
  | "newTab"
  | "open"
  | "save"
  | "saveAs";

export function resolveApplicationShortcut(
  key: string,
  shiftKey: boolean
): ApplicationShortcut | undefined {
  const normalized = key.toLowerCase();
  if (key === "Tab")
    return shiftKey ? "previousTab" : "nextTab";
  if (normalized === "w")
    return "closeTab";
  if (normalized === "t" || normalized === "n")
    return "newTab";
  if (normalized === "o")
    return "open";
  if (normalized === "s")
    return shiftKey ? "saveAs" : "save";
  return undefined;
}
