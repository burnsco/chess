import { memo, useMemo } from "react";
import { FILES, PIECE_SYMBOLS, getFile, getRankIndex, indexToAlgebraic, isOnBoard } from "../engine/board";
import type { ChessState, Color, Move, Piece } from "../engine/types";

interface ChessBoardProps {
  state: ChessState;
  selectedSquare: number | null;
  legalMoves: Move[];
  inCheck: boolean;
  interactionDisabled?: boolean;
  perspective?: Color;
  onSquareClick: (square: number) => void;
  onPieceDragStart: (square: number) => void;
  onPieceDrop: (targetSquare: number) => void;
}

function isMovablePiece(piece: Piece | null, state: ChessState): boolean {
  return Boolean(piece && piece.color === state.sideToMove);
}

function getKeyboardTarget(
  square: number,
  key: string,
  perspective: Color,
): number | null {
  const file = getFile(square);
  const rank = getRankIndex(square);
  const inverted = perspective === "b";

  const deltas: Record<string, [number, number]> = {
    ArrowUp: [0, inverted ? 1 : -1],
    ArrowDown: [0, inverted ? -1 : 1],
    ArrowLeft: [inverted ? 1 : -1, 0],
    ArrowRight: [inverted ? -1 : 1, 0],
  };

  const delta = deltas[key];
  if (!delta) {
    return null;
  }

  const nextFile = file + delta[0];
  const nextRank = rank + delta[1];
  if (nextFile < 0 || nextFile > 7 || nextRank < 0 || nextRank > 7) {
    return null;
  }

  const nextSquare = nextRank * 8 + nextFile;
  return isOnBoard(nextSquare) ? nextSquare : null;
}

function ChessBoardImpl({
  state,
  selectedSquare,
  legalMoves,
  inCheck,
  interactionDisabled = false,
  perspective = "w",
  onSquareClick,
  onPieceDragStart,
  onPieceDrop,
}: ChessBoardProps) {
  const legalTargetMap = useMemo(() => {
    const map = new Map<number, Move[]>();
    for (const move of legalMoves) {
      const existing = map.get(move.to) ?? [];
      existing.push(move);
      map.set(move.to, existing);
    }
    return map;
  }, [legalMoves]);

  // Find the king in check so we can highlight it
  const checkSquare = useMemo(() => {
    if (!inCheck) {
      return -1;
    }
    return state.board.findIndex((p) => p !== null && p.type === "k" && p.color === state.sideToMove);
  }, [inCheck, state.board, state.sideToMove]);

  const displaySquares = useMemo(() => {
    return perspective === "w"
      ? Array.from({ length: 64 }, (_, square) => square)
      : Array.from({ length: 64 }, (_, square) => 63 - square);
  }, [perspective]);

  const boardHelpId = "chess-board-help";

  return (
    <div className={`board-shell${interactionDisabled ? " board-shell--locked" : ""}`}>
      <p id={boardHelpId} className="visually-hidden">
        Use arrow keys to move between squares. Press Enter or Space to select a square.
      </p>
      <div className="board" role="grid" aria-label="Chess board" aria-describedby={boardHelpId}>
        {displaySquares.map((square, displayIndex) => {
          const piece = state.board[square];
          const isLight = (getFile(square) + getRankIndex(square)) % 2 === 1;
          const isSelected = selectedSquare === square;
          const isLastMove = state.lastMove
            ? square === state.lastMove.from || square === state.lastMove.to
            : false;
          const legalTargets = legalTargetMap.get(square) ?? [];
          const isLegalTarget = legalTargets.length > 0;
          const isInCheck = square === checkSquare;
          const label = indexToAlgebraic(square);
          const rank = 8 - getRankIndex(square);
          const file = FILES[getFile(square)];
          const isCapture = isLegalTarget && piece !== null;
          const displayFile = displayIndex % 8;
          const displayRank = Math.floor(displayIndex / 8);

          const squareCls = [
            "square",
            isLight ? "light" : "dark",
            isSelected ? "selected" : "",
            isLastMove && !isSelected ? "last-move" : "",
            isInCheck ? "in-check" : "",
          ]
            .filter(Boolean)
            .join(" ");

          return (
            <button
              key={square}
              type="button"
              className={squareCls}
              disabled={interactionDisabled}
              data-square={square}
              onClick={() => onSquareClick(square)}
              onKeyDown={(e) => {
                const nextSquare = getKeyboardTarget(square, e.key, perspective);
                if (nextSquare === null) {
                  return;
                }

                e.preventDefault();
                const nextButton = e.currentTarget
                  .closest(".board")
                  ?.querySelector<HTMLButtonElement>(`button[data-square="${nextSquare}"]`);
                nextButton?.focus();
              }}
              onDragOver={(e) => {
                if (!interactionDisabled && selectedSquare !== null) e.preventDefault();
              }}
              onDrop={(e) => {
                e.preventDefault();
                if (!interactionDisabled) {
                  onPieceDrop(square);
                }
              }}
              aria-label={`Square ${label}${piece ? `, ${piece.color === "w" ? "white" : "black"} ${piece.type}` : ""}${isSelected ? ", selected" : ""}${isLegalTarget ? ", legal target" : ""}`}
            >
              {displayFile === 0 ? (
                <span className="rank-label" aria-hidden="true">
                  {rank}
                </span>
              ) : null}
              {displayRank === 7 ? (
                <span className="file-label" aria-hidden="true">
                  {file}
                </span>
              ) : null}
              {isLegalTarget ? (
                <span
                  className={isCapture ? "legal-indicator capture" : "legal-indicator move"}
                  aria-hidden="true"
                />
              ) : null}
              {piece ? (
                <span
                  className={`piece ${piece.color === "w" ? "white-piece" : "black-piece"}`}
                  draggable={!interactionDisabled && isMovablePiece(piece, state)}
                  onDragStart={() => onPieceDragStart(square)}
                >
                  {PIECE_SYMBOLS[piece.color][piece.type]}
                </span>
              ) : null}
            </button>
          );
        })}
      </div>
    </div>
  );
}

export const ChessBoard = memo(ChessBoardImpl);
