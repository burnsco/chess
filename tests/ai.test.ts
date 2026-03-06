import { describe, expect, it } from "vitest";
import { AI_DIFFICULTY_SETTINGS } from "../src/ai/config";
import { parseBestMove, parseUciMove } from "../src/ai/uci";
import { algebraicToIndex } from "../src/engine/ChessGame";

describe("AI helpers", () => {
  it("maps the configured difficulty levels to search time limits", () => {
    expect(AI_DIFFICULTY_SETTINGS.easy.movetimeMs).toBe(100);
    expect(AI_DIFFICULTY_SETTINGS.medium.movetimeMs).toBe(500);
    expect(AI_DIFFICULTY_SETTINGS.hard.movetimeMs).toBe(1500);
  });

  it("parses UCI bestmove lines", () => {
    expect(parseBestMove("bestmove e2e4 ponder e7e5")).toBe("e2e4");
    expect(parseBestMove("bestmove (none)")).toBeNull();
  });

  it("parses plain and promotion UCI moves", () => {
    expect(parseUciMove("e2e4")).toEqual({
      from: algebraicToIndex("e2"),
      to: algebraicToIndex("e4"),
      promotion: undefined
    });

    expect(parseUciMove("a7a8q")).toEqual({
      from: algebraicToIndex("a7"),
      to: algebraicToIndex("a8"),
      promotion: "q"
    });
  });

  it("rejects malformed UCI moves", () => {
    expect(parseUciMove("e9e4")).toBeNull();
    expect(parseUciMove("bestmove e2e4")).toBeNull();
  });
});
