import { describe, expect, it } from "vitest";
import {
  isEligibleTranslationSelection,
  mapTranslationPayload,
  shouldDismissTranslation
} from "./translation";

describe("translation", () => {
  it.each(["hello", "can't", "well-being", "café"])(
    "allows a selected single word: %s", selection => {
      expect(isEligibleTranslationSelection(selection)).toBe(true);
    }
  );

  it.each(["", "two words", "word.", "hello\nworld"])(
    "rejects an ineligible selection: %s", selection => {
      expect(isEligibleTranslationSelection(selection)).toBe(false);
    }
  );

  it("maps a result to popup labels and concise alternatives", () => {
    expect(mapTranslationPayload({
      word: "factory",
      sourceLanguage: "en",
      destinationLanguage: "th",
      partOfSpeech: "noun",
      translations: ["โรงงาน", "กิจการ"]
    })).toEqual({
      heading: "factory",
      languagePair: "English → Thai",
      partOfSpeech: "noun",
      result: "โรงงาน · กิจการ"
    });
  });

  it.each(["close", "outside", "escape"] as const)("dismisses for %s", reason => {
    expect(shouldDismissTranslation(reason)).toBe(true);
  });
});
