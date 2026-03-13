# ♟ Multiplayer Chess Roadmap & Milestones

This document outlines the planned progression for the multiplayer chess platform, from core synchronization to a fully featured competitive ecosystem.

---

## 🚩 Milestone 1: Core Stability & Integrity

*Goal: Ensure games are fair, stable, and resilient to network issues.*

- [ ] **Server-Side Move Validation**: Move the logic from `ChessGame.ts` to the C# backend (or a shared library) to prevent players from sending illegal moves via console.
- [ ] **Reconnection Support**: Implement a "Game State" cache in Redis or memory so players can refresh their browser and resume an active match.
- [ ] **GameOver Persistence**: Ensure the server officially declares the winner and prevents further moves once checkmate or draw is reached.
- [ ] **Anti-Cheat Lite**: Detect impossible move speeds or multiple sessions from the same IP.

## 🚩 Milestone 2: The "Real-Time" Experience

*Goal: Make the game feel "alive" and urgent.*

- [ ] **Chess Clocks**: Implement Blitz (3m), Rapid (10m), and Classical (30m) timers synced via SignalR.
- [ ] **In-Game Chat**: A dedicated chat panel for opponents to talk during matches.
- [ ] **Action Notifications**: System messages for "Check!", "Draw Offered", and "Low Time".
- [ ] **Emoji Reactions**: Quick-tap reactions (👏, 😮, 😠, 💡) that appear over the board.

## 🚩 Milestone 3: Identity & Persistence

*Goal: Give players a reason to return.*

- [ ] **User Accounts**: Authentication using ASP.NET Core Identity (JWT).
- [ ] **Match History**: A dashboard to view past games, including who won and the total move count.
- [ ] **PGN Export**: Allow users to download the "Portable Game Notation" of their games to analyze elsewhere.
- [ ] **User Profiles**: Custom avatars, win/loss ratios, and "Member Since" badges.

## 🚩 Milestone 4: Competitive & Social

*Goal: Build a community and a sense of progression.*

- [ ] **ELO Rating System**: Implement a Glicko-2 or ELO ranking system to track player skill.
- [ ] **Skill-Based Matchmaking (SBMM)**: Update `GameHub` to match players with similar ratings.
- [ ] **Global Leaderboard**: A "Top 100" list of the highest-rated players.
- [ ] **Private Rooms**: Generate "Join Links" (e.g., `chess.com/play/abc-123`) so friends can play without the public queue.
- [ ] **Spectator Mode**: Allow users to watch high-rated matches in real-time.

## 🚩 Milestone 5: Advanced Analytics

*Goal: Help players get better.*

- [ ] **Server-Side AI Analysis**: After a game ends, use Stockfish on the backend to identify "Blunders", "Mistakes", and "Brilliant" moves.
- [ ] **Opening Explorer**: Show players the name of the chess opening they are currently playing (e.g., "Sicilian Defense").
- [ ] **Move Heatmaps**: Visual representation of where a player most frequently moves their pieces.

---

## 🛠 Tech Stack Considerations

- **Database**: PostgreSQL (for users/history).
- **Caching**: Redis (for active game state and matchmaking).
- **Communication**: SignalR (WebSockets).
- **Processing**: Background workers for AI analysis.
