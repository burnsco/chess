export type GameMode = "human" | "ai";
export type AIDifficulty = "easy" | "medium" | "hard";

export const AI_DIFFICULTY_SETTINGS: Record<
  AIDifficulty,
  {
    label: string;
    movetimeMs: number;
  }
> = {
  easy: {
    label: "Easy",
    movetimeMs: 100
  },
  medium: {
    label: "Medium",
    movetimeMs: 500
  },
  hard: {
    label: "Hard",
    movetimeMs: 1500
  }
};

export const AI_MOVE_DELAY_MS = 220;
