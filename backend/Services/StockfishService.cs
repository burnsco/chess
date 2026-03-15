using System.Diagnostics;
using System.Text;

namespace Backend.Services
{
    public class StockfishService : IDisposable
    {
        private readonly ILogger<StockfishService> _logger;
        private readonly string _stockfishPath;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private Process? _process;
        private StreamWriter? _writer;
        private StreamReader? _reader;

        public StockfishService(ILogger<StockfishService> logger)
        {
            _logger = logger;
            if (File.Exists("/usr/games/stockfish"))
            {
                _stockfishPath = "/usr/games/stockfish";
            }
            else
            {
                _stockfishPath = "stockfish";
            }
            EnsureProcessRunning();
        }

        private void EnsureProcessRunning()
        {
            if (_process != null && !_process.HasExited)
                return;

            _logger.LogInformation("Stockfish: Starting persistent process...");
            var startInfo = new ProcessStartInfo
            {
                FileName = _stockfishPath,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _process = new Process { StartInfo = startInfo };
            _process.Start();

            _writer = _process.StandardInput;
            _reader = _process.StandardOutput;

            _writer.WriteLine("uci");
            string? line;
            while ((line = _reader.ReadLine()) != null)
            {
                _logger.LogInformation("Stockfish << {Line}", line);
                if (line == "uciok") break;
            }
            _writer.WriteLine("isready");
            while ((line = _reader.ReadLine()) != null)
            {
                if (line == "readyok") break;
            }
        }

        public async Task<string?> GetBestMoveAsync(string fen, int skillLevel, int movetimeMs)
        {
            await _semaphore.WaitAsync();
            try
            {
                EnsureProcessRunning();
                if (_writer == null || _reader == null) return null;

                _logger.LogInformation("Stockfish >> setoption name Skill Level value {SkillLevel}", skillLevel);
                await _writer.WriteLineAsync($"setoption name Skill Level value {skillLevel}");
                await _writer.WriteLineAsync($"position fen {fen}");
                _logger.LogInformation("Stockfish >> go movetime {MovetimeMs}", movetimeMs);
                await _writer.WriteLineAsync($"go movetime {movetimeMs}");

                string? bestMove = null;
                string? line;
                while ((line = await _reader.ReadLineAsync()) != null)
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
                return bestMove;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting best move from Stockfish");
                if (_process != null && !_process.HasExited)
                {
                    _process.Kill();
                }
                return null;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            _semaphore.Dispose();
            if (_process != null && !_process.HasExited)
            {
                try
                {
                    _writer?.WriteLine("quit");
                    if (!_process.WaitForExit(1000))
                    {
                        _process.Kill();
                    }
                }
                catch { }
                _process.Dispose();
            }
        }
    }
}
