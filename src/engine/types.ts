export type Color = "w" | "b";
export type PieceType = "p" | "n" | "b" | "r" | "q" | "k";

export interface Piece {
  color: Color;
  type: PieceType;
}

export type Board = Array<Piece | null>;

export interface CastlingRights {
  w: {
    kingside: boolean;
    queenside: boolean;
  };
  b: {
    kingside: boolean;
    queenside: boolean;
  };
}

export interface CapturedPieces {
  w: Piece[];
  b: Piece[];
}

export interface ChessState {
  board: Board;
  sideToMove: Color;
  castlingRights: CastlingRights;
  enPassantTarget: number | null;
  halfmoveClock: number;
  fullmoveNumber: number;
  capturedPieces: CapturedPieces;
  lastMove: Move | null;
}

export interface Move {
  from: number;
  to: number;
  piece: Piece;
  captured?: Piece;
  promotion?: PieceType;
  isDoublePawnPush?: boolean;
  isEnPassant?: boolean;
  isCastling?: "kingside" | "queenside";
  san?: string;
}

export interface MoveRecord {
  move: Move;
  san: string;
  resultingFen: string;
}

export type DrawClaimReason = "threefold-repetition" | "fifty-move-rule";

export type GameResultReason =
  | "checkmate"
  | "stalemate"
  | "threefold-repetition"
  | "fifty-move-rule"
  | "fivefold-repetition"
  | "seventy-five-move-rule"
  | "insufficient-material";

export interface GameResult {
  winner: Color | null;
  reason: GameResultReason;
  message: string;
}

export interface GameStatus {
  inCheck: boolean;
  legalMoves: Move[];
  claimableDraws: DrawClaimReason[];
  result: GameResult | null;
}

export interface HistoryEntry {
  previousState: ChessState;
  previousPositionCounts: Map<string, number>;
  previousStatus: GameStatus;
  record: MoveRecord;
}
