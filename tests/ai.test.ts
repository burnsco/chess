import { describe, expect, it } from "vitest";
import { AI_DIFFICULTY_SETTINGS } from "../src/ai/config";
import { parseBestMove, parseUciMove } from "../src/ai/uci";
import { algebraicToIndex } from "../src/engine/ChessGame";

describe("AI helpers", () => {
  it("maps the configured difficulty levels to search settings", () => {
    expect(AI_DIFFICULTY_SETTINGS).toEqual({
      easy: {
        label: "Easy",
        skillLevel: 3,
        movetimeMs: 300,
      },
      medium: {
        label: "Medium",
        skillLevel: 12,
        movetimeMs: 800,
      },
      hard: {
        label: "Hard",
        skillLevel: 20,
        movetimeMs: 2000,
      },
    });

    expect(AI_DIFFICULTY_SETTINGS.easy.movetimeMs).toBeLessThan(
      AI_DIFFICULTY_SETTINGS.medium.movetimeMs,
    );
    expect(AI_DIFFICULTY_SETTINGS.medium.movetimeMs).toBeLessThan(
      AI_DIFFICULTY_SETTINGS.hard.movetimeMs,
    );
    expect(AI_DIFFICULTY_SETTINGS.easy.skillLevel).toBeLessThan(
      AI_DIFFICULTY_SETTINGS.medium.skillLevel,
    );
    expect(AI_DIFFICULTY_SETTINGS.medium.skillLevel).toBeLessThan(
      AI_DIFFICULTY_SETTINGS.hard.skillLevel,
    );
  });

  it("parses UCI bestmove lines", () => {
    expect(parseBestMove("bestmove e2e4 ponder e7e5")).toBe("e2e4");
    expect(parseBestMove("bestmove (none)")).toBeNull();
  });

  it("parses plain and promotion UCI moves", () => {
    expect(parseUciMove("e2e4")).toEqual({
      from: algebraicToIndex("e2"),
      to: algebraicToIndex("e4"),
      promotion: undefined,
    });

    expect(parseUciMove("a7a8q")).toEqual({
      from: algebraicToIndex("a7"),
      to: algebraicToIndex("a8"),
      promotion: "q",
    });
  });

  it("rejects malformed UCI moves", () => {
    expect(parseUciMove("e9e4")).toBeNull();
    expect(parseUciMove("bestmove e2e4")).toBeNull();
  });
});
