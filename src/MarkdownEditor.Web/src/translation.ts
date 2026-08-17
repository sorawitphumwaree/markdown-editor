export interface TranslationPayload {
  word: string;
  sourceLanguage: string;
  destinationLanguage: string;
  partOfSpeech?: string | null;
  translations?: string[];
  message?: string;
}

export function isEligibleTranslationSelection(selection: string): boolean {
  return selection.length <= 100
    && /^[\p{L}\p{M}]+(?:['’\-][\p{L}\p{M}]+)*$/u.test(selection);
}

export function languageName(code: string): string {
  return ({ en: "English", th: "Thai" } as Record<string, string>)[code] ?? code;
}

export function mapTranslationPayload(payload: TranslationPayload): {
  heading: string;
  languagePair: string;
  partOfSpeech: string;
  result: string;
} {
  return {
    heading: payload.word,
    languagePair: `${languageName(payload.sourceLanguage)} → ${languageName(payload.destinationLanguage)}`,
    partOfSpeech: payload.partOfSpeech?.trim() || "Part of speech unavailable",
    result: payload.message || payload.translations?.filter(Boolean).join(" · ")
      || "No translation available"
  };
}

export function shouldDismissTranslation(reason: "close" | "outside" | "escape"): boolean {
  return reason === "close" || reason === "outside" || reason === "escape";
}
