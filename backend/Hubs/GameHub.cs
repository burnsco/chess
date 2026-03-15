using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using ChessDotNet;

namespace Backend.Hubs;

public class GameSession
{
    public string GameId { get; set; } = string.Empty;
    public string WhitePlayerId { get; set; } = string.Empty;
    public string BlackPlayerId { get; set; } = string.Empty;
    public ChessGame Game { get; set; } = new ChessGame();
    public bool WhiteWantsRematch { get; set; } = false;
    public bool BlackWantsRematch { get; set; } = false;

    public void Reset()
    {
        Game = new ChessGame();
        WhiteWantsRematch = false;
        BlackWantsRematch = false;
    }
}

public class GameHub : Hub
{
    // Track waiting players as a set for O(1) membership checks and safe removal on disconnect.
    // The queue provides FIFO ordering; the set provides fast existence checks and atomic add.
    private static readonly ConcurrentDictionary<string, byte> _waitingSet = new();
    private static readonly ConcurrentQueue<string> _waitingQueue = new();
    private static readonly ConcurrentDictionary<string, GameSession> _activeGames = new();
    private static readonly ConcurrentDictionary<string, string> _playerToGameId = new();

    
    public async Task RecoverSession(string gameId, string colorStr)
    {
        var playerId = Context.ConnectionId;
        if (!_activeGames.TryGetValue(gameId, out var session))
        {
            session = new GameSession { GameId = gameId };
            _activeGames[gameId] = session;
        }

        if (colorStr == "white") session.WhitePlayerId = playerId;
        else if (colorStr == "black") session.BlackPlayerId = playerId;

        _playerToGameId[playerId] = gameId;
        await Groups.AddToGroupAsync(playerId, gameId);
    }

    public async Task FindGame()
    {
        var playerId = Context.ConnectionId;

        // Atomic add-or-skip: prevents a player from entering the queue twice
        if (!_waitingSet.TryAdd(playerId, 0)) return;

        // Try to dequeue an opponent. Skip any stale entries (players who disconnected
        // while waiting — they were removed from _waitingSet in OnDisconnectedAsync).
        while (_waitingQueue.TryDequeue(out var opponentId))
        {
            // Check if this opponent is still connected and waiting
            if (!_waitingSet.TryRemove(opponentId, out _))
            {
                // Ghost entry — opponent disconnected while queued; try the next one
                continue;
            }

            // Remove the current player from waiting now that we have a match
            _waitingSet.TryRemove(playerId, out _);

            var gameId = Guid.NewGuid().ToString();
            var session = new GameSession
            {
                GameId = gameId,
                WhitePlayerId = opponentId, // First in queue gets White
                BlackPlayerId = playerId
            };

            _activeGames[gameId] = session;
            _playerToGameId[playerId] = gameId;
            _playerToGameId[opponentId] = gameId;

            await Groups.AddToGroupAsync(playerId, gameId);
            await Groups.AddToGroupAsync(opponentId, gameId);

            await Clients.Client(session.WhitePlayerId).SendAsync("GameStarted", gameId, "white");
            await Clients.Client(session.BlackPlayerId).SendAsync("GameStarted", gameId, "black");
            return;
        }

        // No live opponent found — enqueue and wait
        _waitingQueue.Enqueue(playerId);
        await Clients.Caller.SendAsync("WaitingForOpponent");
    }

    public async Task SendMove(string gameId, string moveStr)
    {
        if (!_activeGames.TryGetValue(gameId, out var session)) return;

        var playerId = Context.ConnectionId;
        var playerColor = playerId == session.WhitePlayerId ? Player.White : Player.Black;

        // Validation: Is it the player's turn?
        if (session.Game.WhoseTurn != playerColor) return;

        // Parse and validate the move (e.g., "e2e4" or "e7e8Q")
        var move = ParseMove(moveStr, playerColor);
        if (move == null) return;

        var isValid = session.Game.IsValidMove(move);
        if (!isValid) return;

        session.Game.MakeMove(move, true);

        // Broadcast validated move to the opponent
        await Clients.OthersInGroup(gameId).SendAsync("ReceiveMove", moveStr);

        // Check for game over conditions
        if (session.Game.IsCheckmated(Player.White) || session.Game.IsCheckmated(Player.Black) ||
            session.Game.IsStalemated(Player.White) || session.Game.IsStalemated(Player.Black))
        {
            var winner = session.Game.IsCheckmated(Player.Black) ? "white" :
                         session.Game.IsCheckmated(Player.White) ? "black" : "draw";
            await Clients.Group(gameId).SendAsync("GameOver", winner);
        }
    }

    public async Task RequestRematch(string gameId)
    {
        if (!_activeGames.TryGetValue(gameId, out var session)) return;

        var playerId = Context.ConnectionId;
        if (playerId == session.WhitePlayerId) session.WhiteWantsRematch = true;
        if (playerId == session.BlackPlayerId) session.BlackWantsRematch = true;

        if (session.WhiteWantsRematch && session.BlackWantsRematch)
        {
            // Swap colors for rematch
            var oldWhite = session.WhitePlayerId;
            session.WhitePlayerId = session.BlackPlayerId;
            session.BlackPlayerId = oldWhite;
            session.Reset();

            await Clients.Client(session.WhitePlayerId).SendAsync("GameStarted", gameId, "white");
            await Clients.Client(session.BlackPlayerId).SendAsync("GameStarted", gameId, "black");
        }
        else
        {
            await Clients.OthersInGroup(gameId).SendAsync("OpponentWantsRematch");
        }
    }

    private Move? ParseMove(string moveStr, Player player)
    {
        try
        {
            var from = moveStr.Substring(0, 2);
            var to = moveStr.Substring(2, 2);
            var promChar = moveStr.Length > 4 ? moveStr[4] : (char?)null;

            var promotion = promChar switch
            {
                'q' => 'Q',
                'r' => 'R',
                'b' => 'B',
                'n' => 'N',
                _ => (char?)null
            };

            return new Move(from, to, player, promotion);
        }
        catch { return null; }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var playerId = Context.ConnectionId;

        // Remove from the waiting set. If this player was in the queue but hasn't been
        // dequeued yet, the next FindGame call will see the set entry is gone and skip them.
        _waitingSet.TryRemove(playerId, out _);

        // Clean up any active game
        if (_playerToGameId.TryRemove(playerId, out var gameId))
        {
            if (_activeGames.TryRemove(gameId, out var session))
            {
                var otherPlayerId = playerId == session.WhitePlayerId
                    ? session.BlackPlayerId
                    : session.WhitePlayerId;

                _playerToGameId.TryRemove(otherPlayerId, out _);
                await Clients.Client(otherPlayerId).SendAsync("OpponentDisconnected");
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}
