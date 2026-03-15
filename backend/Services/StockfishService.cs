using System.Diagnostics;
using System.Text;

namespace Backend.Services
{
    public class StockfishService
    {
        private readonly ILogger<StockfishService> _logger;
        private readonly string _stockfishPath;

        public StockfishService(ILogger<StockfishService> logger)
        {
            _logger = logger;
            // Ubuntu/Debian installs stockfish to /usr/games/stockfish
            if (File.Exists("/usr/games/stockfish"))
            {
                _stockfishPath = "/usr/games/stockfish";
            }
            else
            {
                _stockfishPath = "stockfish";
            }
        }

        public async Task<string?> GetBestMoveAsync(string fen, int skillLevel, int movetimeMs)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = _stockfishPath,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                _logger.LogInformation("Stockfish: Starting process...");
                process.Start();

                using var writer = process.StandardInput;
                using var reader = process.StandardOutput;

                _logger.LogInformation("Stockfish >> uci");
                await writer.WriteLineAsync("uci");
                
                // Wait for uciok
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    _logger.LogInformation("Stockfish << {Line}", line);
                    if (line == "uciok") break;
                }

                _logger.LogInformation("Stockfish >> setoption name Skill Level value {SkillLevel}", skillLevel);
                await writer.WriteLineAsync($"setoption name Skill Level value {skillLevel}");
                await writer.WriteLineAsync($"position fen {fen}");
                _logger.LogInformation("Stockfish >> go movetime {MovetimeMs}", movetimeMs);
                await writer.WriteLineAsync($"go movetime {movetimeMs}");

                string? bestMove = null;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    _logger.LogInformation("Stockfish << {Line}", line);
                    if (line.StartsWith("bestmove"))
                    {
                        var parts = line.Split(' ');
                        if (parts.Length >= 2)
                        {
                            bestMove = parts[1];
                            if (bestMove == "(none)") bestMove = null;
                        }
                        break;
                    }
                }

                await writer.WriteLineAsync("quit");
                if (!process.WaitForExit(1000))
                {
                    process.Kill();
                }

                return bestMove;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting best move from Stockfish");
                return null;
            }
        }
    }
}
