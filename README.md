# Chess

A local two-player chess game built with Vite, React, and TypeScript. No accounts, no servers, no AI — just a clean, fast pass-and-play chess board in the browser.

## Features

- Complete chess rules engine written from scratch in TypeScript
  - Legal move generation for all pieces
  - Castling (kingside & queenside)
  - En passant
  - Pawn promotion with piece picker
  - Check, checkmate, and stalemate detection
  - Threefold repetition draw
  - Fifty-move rule draw
  - Insufficient material draw
- Click-to-move and drag-and-drop piece movement
- Visual indicators: selected piece, legal move dots/rings, last move highlight, check glow on king
- Move history in algebraic notation with auto-scroll to latest move
- Captured pieces display with material advantage count per player
- Undo last move
- Dark UI that fits a single viewport on desktop

## Tech Stack

- [Vite](https://vite.dev/) — build tool and dev server
- [React 19](https://react.dev/) — UI
- [TypeScript](https://www.typescriptlang.org/) — end-to-end type safety
- [Vitest](https://vitest.dev/) — engine unit tests
- Pure CSS — no UI frameworks or component libraries

## Getting Started

```bash
npm install
npm run dev
```

Open [http://localhost:3000](http://localhost:3000).

## Scripts

| Command | Description |
|---|---|
| `npm run dev` | Start dev server at `localhost:3000` |
| `npm run build` | Type-check and produce a production build |
| `npm run preview` | Serve the production build locally |
| `npm test` | Run engine unit tests |

## Project Structure

```
src/
  engine/
    types.ts        # Shared types: Piece, Move, ChessState, GameStatus, etc.
    board.ts        # Board helpers, piece symbols, coordinate utilities
    ChessGame.ts    # Full rules engine + ChessGame class
  components/
    ChessBoard.tsx  # Board renderer — squares, pieces, highlights, drag/drop
    MoveList.tsx    # Move history panel with auto-scroll
  styles/
    index.css       # All styles via CSS custom properties, no frameworks
  App.tsx           # Game state, event handlers, layout, sub-components
  main.tsx          # React entry point
tests/
  chess.test.ts     # Engine unit tests
```

## How Move Legality Works

The UI never moves pieces directly. It always asks the engine for the set of legal moves.

1. The engine generates pseudo-legal moves for the side to move.
2. Each candidate move is simulated on a cloned board.
3. Any move that leaves that side's king in check is discarded.
4. Only the remaining moves are exposed to the UI — illegal moves cannot be played.

Castling, en passant, promotion, threefold repetition tracking, insufficient material, and the fifty-move rule are all enforced in the engine layer.

## License

MIT
