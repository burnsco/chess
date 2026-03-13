using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Backend.Hubs;

public class GameHub : Hub
{
    private static readonly ConcurrentQueue<string> _waitingPlayers = new();
    private static readonly ConcurrentDictionary<string, string> _playerToGame = new();

    public async Task FindGame()
    {
        var playerId = Context.ConnectionId;

        // Prevent duplicate queuing
        if (_waitingPlayers.Contains(playerId))
            return;

        if (_waitingPlayers.TryDequeue(out var opponentId))
        {
            var gameId = Guid.NewGuid().ToString();
            _playerToGame[playerId] = gameId;
            _playerToGame[opponentId] = gameId;

            await Groups.AddToGroupAsync(playerId, gameId);
            await Groups.AddToGroupAsync(opponentId, gameId);

            // Notify both players that a game has started
            // Player who was waiting gets White, new player gets Black
            await Clients.Client(opponentId).SendAsync("GameStarted", gameId, "white");
            await Clients.Client(playerId).SendAsync("GameStarted", gameId, "black");
        }
        else
        {
            _waitingPlayers.Enqueue(playerId);
            await Clients.Caller.SendAsync("WaitingForOpponent");
        }
    }

    public async Task SendMove(string gameId, string move)
    {
        // Broadcast the move to the other player in the game group
        await Clients.OthersInGroup(gameId).SendAsync("ReceiveMove", move);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Simple cleanup: remove from queue if they leave
        // (In a real app, you'd also notify the opponent)
        var playerId = Context.ConnectionId;
        
        // Remove from queue if present (filtering logic for ConcurrentQueue is limited)
        // For now, we'll just handle it if they were in a game.
        if (_playerToGame.TryRemove(playerId, out var gameId))
        {
            await Clients.OthersInGroup(gameId).SendAsync("OpponentDisconnected");
        }

        await base.OnDisconnectedAsync(exception);
    }
}
