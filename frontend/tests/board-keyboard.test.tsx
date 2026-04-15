// @vitest-environment jsdom

import React from "react";
import { act } from "react";
import { afterEach, beforeEach, describe, expect, it } from "vitest";
import { createRoot, type Root } from "react-dom/client";
import { ChessBoard } from "../src/components/ChessBoard";
import { createEmptyBoard } from "../src/engine/ChessGame";
import type { ChessState, Color } from "../src/engine/types";

function createState(sideToMove: Color = "w"): ChessState {
  return {
    board: createEmptyBoard(),
    sideToMove,
    castlingRights: {
      w: { kingside: false, queenside: false },
      b: { kingside: false, queenside: false },
    },
    enPassantTarget: null,
    halfmoveClock: 0,
    fullmoveNumber: 1,
    capturedPieces: { w: [], b: [] },
    lastMove: null,
  };
}

function getSquareButton(container: HTMLElement, square: string): HTMLButtonElement {
  const button = container.querySelector<HTMLButtonElement>(`button[aria-label^="Square ${square}"]`);
  if (!button) {
    throw new Error(`Missing square button for ${square}`);
  }

  return button;
}

describe("ChessBoard keyboard navigation", () => {
  let container: HTMLDivElement;
  let root: Root | null = null;

  beforeEach(() => {
    container = document.createElement("div");
    document.body.appendChild(container);
  });

  afterEach(async () => {
    if (root) {
      await act(async () => {
        root?.unmount();
      });
    }

    container.remove();
    root = null;
  });

  it("moves focus with arrow keys", async () => {
    await act(async () => {
      root = createRoot(container);
      root.render(
        React.createElement(ChessBoard, {
          state: createState(),
          selectedSquare: null,
          legalMoves: [],
          inCheck: false,
          perspective: "w",
          onSquareClick: () => {},
          onPieceDragStart: () => {},
          onPieceDrop: () => {},
        }),
      );
    });

    const fromSquare = getSquareButton(container, "a1");
    const toSquare = getSquareButton(container, "b1");

    fromSquare.focus();
    expect(document.activeElement).toBe(fromSquare);

    await act(async () => {
      fromSquare.dispatchEvent(
        new KeyboardEvent("keydown", {
          key: "ArrowRight",
          bubbles: true,
        }),
      );
    });

    expect(document.activeElement).toBe(toSquare);
  });
});
