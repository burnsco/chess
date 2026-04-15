using System.Diagnostics;
using System.Collections.Concurrent;

namespace Backend.Services
{
    public class StockfishService : IDisposable
    {
        private readonly ILogger<StockfishService> _logger;
        private readonly string _stockfishPath;
        private readonly SemaphoreSlim _poolSemaphore;
        private readonly ConcurrentQueue<StockfishInstance> _availableInstances = new();
        private readonly StockfishInstance[] _instances;
        private bool _disposed;

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

            const int poolSize = 2;
            _poolSemaphore = new SemaphoreSlim(poolSize, poolSize);
            _instances = new StockfishInstance[poolSize];
            for (var i = 0; i < poolSize; i += 1)
            {
                _instances[i] = new StockfishInstance(_logger, _stockfishPath);
                _availableInstances.Enqueue(_instances[i]);
            }
        }

        private static readonly TimeSpan InitTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(1);

        private async Task<StockfishInstance> RentInstanceAsync()
        {
            await _poolSemaphore.WaitAsync();
            if (_availableInstances.TryDequeue(out var instance))
            {
                return instance;
            }

            _poolSemaphore.Release();
            throw new InvalidOperationException("Stockfish instance pool was unexpectedly empty.");
        }

        private void ReturnInstance(StockfishInstance instance)
        {
            _availableInstances.Enqueue(instance);
            _poolSemaphore.Release();
        }

        public async Task<string?> GetBestMoveAsync(string fen, int skillLevel, int movetimeMs)
        {
            if (_disposed)
            {
                return null;
            }

            var instance = await RentInstanceAsync();
            try
            {
                return await instance.GetBestMoveAsync(fen, skillLevel, movetimeMs);
            }
            finally
            {
                ReturnInstance(instance);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _poolSemaphore.Dispose();

            foreach (var instance in _instances)
            {
                instance.Dispose();
            }
        }

        private sealed class StockfishInstance : IDisposable
        {
            private readonly ILogger _logger;
            private readonly string _stockfishPath;
            private readonly SemaphoreSlim _mutex = new(1, 1);
            private Process? _process;
            private StreamWriter? _writer;
            private StreamReader? _reader;
            private bool _disposed;

            public StockfishInstance(ILogger logger, string stockfishPath)
            {
                _logger = logger;
                _stockfishPath = stockfishPath;
            }

            public async Task<string?> GetBestMoveAsync(string fen, int skillLevel, int movetimeMs)
            {
                await _mutex.WaitAsync();
                try
                {
                    EnsureProcessRunning();
                    if (_writer == null || _reader == null)
                    {
                        return null;
                    }

                    skillLevel = Math.Clamp(skillLevel, 0, 20);
                    movetimeMs = Math.Clamp(movetimeMs, 100, 300000);

                    _logger.LogInformation("Stockfish >> setoption name Skill Level value {SkillLevel}", skillLevel);
                    await _writer.WriteLineAsync($"setoption name Skill Level value {skillLevel}");
                    await _writer.WriteLineAsync($"position fen {fen}");
                    _logger.LogInformation("Stockfish >> go movetime {MovetimeMs}", movetimeMs);
                    await _writer.WriteLineAsync($"go movetime {movetimeMs}");

                    var responseTimeout = TimeSpan.FromMilliseconds(Math.Clamp((long)movetimeMs + 10000L, 15000L, 310000L));
                    string? line;
                    while ((line = await ReadLineWithTimeoutAsync(_reader, responseTimeout)) != null)
                    {
                        _logger.LogInformation("Stockfish << {Line}", line);
                        if (line.StartsWith("bestmove", StringComparison.Ordinal))
                        {
                            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                            {
                                return parts[1] == "(none)" ? null : parts[1];
                            }

                            return null;
                        }
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting best move from Stockfish");
                    KillProcess();
                    return null;
                }
                finally
                {
                    _mutex.Release();
                }
            }

            private void EnsureProcessRunning()
            {
                if (_process != null && !_process.HasExited)
                {
                    return;
                }

                KillProcess();

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

                var process = new Process { StartInfo = startInfo };
                process.Start();

                _ = Task.Run(async () =>
                {
                    try
                    {
                        string? errorLine;
                        while ((errorLine = await process.StandardError.ReadLineAsync()) != null)
                        {
                            _logger.LogWarning("Stockfish stderr: {Line}", errorLine);
                        }
                    }
                    catch
                    {
                        // Best-effort drain only.
                    }
                });

                var writer = process.StandardInput;
                var reader = process.StandardOutput;

                RunUciHandshake(process, writer, reader);

                _process = process;
                _writer = writer;
                _reader = reader;
            }

            private void RunUciHandshake(Process process, StreamWriter writer, StreamReader reader)
            {
                writer.WriteLine("uci");
                if (!WaitForLine(reader, "uciok", InitTimeout))
                {
                    _logger.LogError("Stockfish failed to respond with 'uciok' within {Timeout}s", InitTimeout.TotalSeconds);
                    KillProcess(process);
                    throw new InvalidOperationException("Stockfish initialization timed out waiting for uciok");
                }

                writer.WriteLine("isready");
                if (!WaitForLine(reader, "readyok", InitTimeout))
                {
                    _logger.LogError("Stockfish failed to respond with 'readyok' within {Timeout}s", InitTimeout.TotalSeconds);
                    KillProcess(process);
                    throw new InvalidOperationException("Stockfish initialization timed out waiting for readyok");
                }
            }

            private static bool WaitForLine(StreamReader reader, string expectedLine, TimeSpan timeout)
            {
                var deadline = DateTimeOffset.UtcNow + timeout;
                while (DateTimeOffset.UtcNow < deadline)
                {
                    var remaining = deadline - DateTimeOffset.UtcNow;
                    var readTask = Task.Run(() => reader.ReadLine());
                    if (!readTask.Wait(remaining))
                    {
                        return false;
                    }

                    var line = readTask.Result;
                    if (line == null)
                    {
                        return false;
                    }

                    if (line == expectedLine)
                    {
                        return true;
                    }
                }

                return false;
            }

            private async Task<string?> ReadLineWithTimeoutAsync(StreamReader reader, TimeSpan timeout)
            {
                var readTask = Task.Run(() => reader.ReadLine());
                var completedTask = await Task.WhenAny(readTask, Task.Delay(timeout));
                if (completedTask != readTask)
                {
                    KillProcess();
                    return null;
                }

                return await readTask;
            }

            private void KillProcess(Process? process = null)
            {
                var target = process ?? _process;
                if (target == null)
                {
                    return;
                }

                try
                {
                    if (!target.HasExited)
                    {
                        target.Kill(entireProcessTree: true);
                        if (!target.WaitForExit((int)ShutdownTimeout.TotalMilliseconds))
                        {
                            _logger.LogWarning("Stockfish did not exit cleanly before the shutdown timeout.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to terminate Stockfish cleanly.");
                }
                finally
                {
                    target.Dispose();
                    if (ReferenceEquals(target, _process))
                    {
                        _process = null;
                        _writer = null;
                        _reader = null;
                    }
                }
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _mutex.Dispose();
                KillProcess();
            }
        }
    }
}
