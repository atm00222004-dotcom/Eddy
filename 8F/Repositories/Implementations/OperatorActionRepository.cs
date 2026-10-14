using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using _8F.Models;
using Npgsql;

namespace _8F.Services
{
    public class OperatorActionRepository : IOperatorActionRepository
    {
        private readonly string _connectionString;
        private readonly string _offlineQueuePath;
        private readonly object _offlineLock = new();
        private const int MaxOfflineQueueItems = 10000;

        public OperatorActionRepository(string? connectionString = null)
        {
            _connectionString = connectionString 
                ?? Environment.GetEnvironmentVariable("EDDY_DB_CONNECTION_STRING")
                ?? ConfigurationManager.AppSettings["ConnectionString"]
                ?? "Host=localhost;Port=5432;Username=postgres;Password=aryan123;Database=EddyShorter;Pooling=true;Minimum Pool Size=2;Maximum Pool Size=20;";

            var eddyDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Eddy");

            _offlineQueuePath = Path.Combine(eddyDataFolder, "operator_action_offline_queue.json");
        }

        private async Task<NpgsqlConnection> GetOpenConnectionAsync()
        {
            var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            return conn;
        }
        public async Task<bool> InsertBatchAsync(IReadOnlyList<OperatorActionEvent> events)
        {
            if (events == null || events.Count == 0) return true;

            try
            {
                await using var conn = await GetOpenConnectionAsync();

                var sb = new StringBuilder();
                sb.AppendLine(@"INSERT INTO public.""OperatorActionLog"" (
                    ""EventTimestamp"", ""ActionType"", ""MouseButton"", ""ControlName"", ""KeyName"", ""WindowTitle"", ""OperatorName"", ""SessionId"", ""ScreenX"", ""ScreenY""
                ) VALUES ");

                for (int i = 0; i < events.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append($"(@ts{i}, @at{i}, @mb{i}, @cn{i}, @kn{i}, @wt{i}, @op{i}, @sid{i}, @sx{i}, @sy{i})");
                }
                sb.Append(";");

                await using var cmd = new NpgsqlCommand(sb.ToString(), conn);

                for (int i = 0; i < events.Count; i++)
                {
                    var ev = events[i];
                    cmd.Parameters.AddWithValue($"@ts{i}", ev.EventTimestamp.ToUniversalTime());
                    cmd.Parameters.AddWithValue($"@at{i}", ev.ActionType ?? "Action");
                    cmd.Parameters.AddWithValue($"@mb{i}", (object?)ev.MouseButton ?? DBNull.Value);
                    cmd.Parameters.AddWithValue($"@cn{i}", (object?)ev.ControlName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue($"@kn{i}", (object?)ev.KeyName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue($"@wt{i}", (object?)ev.WindowTitle ?? DBNull.Value);
                    cmd.Parameters.AddWithValue($"@op{i}", (object?)ev.OperatorName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue($"@sid{i}", ev.SessionId);
                    cmd.Parameters.AddWithValue($"@sx{i}", (object?)ev.ScreenX ?? DBNull.Value);
                    cmd.Parameters.AddWithValue($"@sy{i}", (object?)ev.ScreenY ?? DBNull.Value);
                }

                await cmd.ExecuteNonQueryAsync();

                if (File.Exists(_offlineQueuePath))
                {
                    _ = Task.Run(() => ProcessOfflineQueueAsync());
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OperatorActionRepository] DB insert failed: {ex.Message}. Queuing offline.");
                EnqueueOfflineBatch(events, ex.Message);
                return false;
            }
        }

        public bool InsertEventSync(OperatorActionEvent ev)
        {
            if (ev == null) return true;
            return InsertBatchSync(new[] { ev });
        }

        public bool InsertBatchSync(IReadOnlyList<OperatorActionEvent> events)
        {
            if (events == null || events.Count == 0) return true;

            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();

                var sb = new StringBuilder();
                sb.AppendLine(@"INSERT INTO public.""OperatorActionLog"" (
                    ""EventTimestamp"", ""ActionType"", ""MouseButton"", ""ControlName"", ""KeyName"", ""WindowTitle"", ""OperatorName"", ""SessionId"", ""ScreenX"", ""ScreenY""
                ) VALUES ");

                for (int i = 0; i < events.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append($"(@ts{i}, @at{i}, @mb{i}, @cn{i}, @kn{i}, @wt{i}, @op{i}, @sid{i}, @sx{i}, @sy{i})");
                }
                sb.Append(";");

                using var cmd = new NpgsqlCommand(sb.ToString(), conn);

                for (int i = 0; i < events.Count; i++)
                {
                    var ev = events[i];
                    cmd.Parameters.AddWithValue($"@ts{i}", ev.EventTimestamp.ToUniversalTime());
                    cmd.Parameters.AddWithValue($"@at{i}", ev.ActionType ?? "Action");
                    cmd.Parameters.AddWithValue($"@mb{i}", (object?)ev.MouseButton ?? DBNull.Value);
                    cmd.Parameters.AddWithValue($"@cn{i}", (object?)ev.ControlName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue($"@kn{i}", (object?)ev.KeyName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue($"@wt{i}", (object?)ev.WindowTitle ?? DBNull.Value);
                    cmd.Parameters.AddWithValue($"@op{i}", (object?)ev.OperatorName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue($"@sid{i}", ev.SessionId);
                    cmd.Parameters.AddWithValue($"@sx{i}", (object?)ev.ScreenX ?? DBNull.Value);
                    cmd.Parameters.AddWithValue($"@sy{i}", (object?)ev.ScreenY ?? DBNull.Value);
                }

                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OperatorActionRepository] Sync DB insert failed: {ex.Message}. Queuing offline.");
                EnqueueOfflineBatch(events, ex.Message);
                return false;
            }
        }

        public async Task<int> DeleteOlderThanDaysAsync(int days)
        {
            if (days <= 0) return 0;

            const string sql = @"
                DELETE FROM public.""OperatorActionLog""
                WHERE ""EventTimestamp"" < @CutoffDate;";

            try
            {
                await using var conn = await GetOpenConnectionAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@CutoffDate", DateTime.UtcNow.AddDays(-days));

                int deleted = await cmd.ExecuteNonQueryAsync();
                System.Diagnostics.Debug.WriteLine($"[OperatorActionRepository] Purged {deleted} action records older than {days} days.");
                return deleted;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OperatorActionRepository] Error purging old logs: {ex.Message}");
                return 0;
            }
        }

        #region Offline Queue Fallback & Sync

        private void EnqueueOfflineBatch(IReadOnlyList<OperatorActionEvent> batch, string reason)
        {
            try
            {
                lock (_offlineLock)
                {
                    var dir = Path.GetDirectoryName(_offlineQueuePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    List<OperatorActionEvent> queue = new();
                    if (File.Exists(_offlineQueuePath))
                    {
                        try
                        {
                            var existingJson = File.ReadAllText(_offlineQueuePath);
                            queue = JsonSerializer.Deserialize<List<OperatorActionEvent>>(existingJson) ?? new();
                        }
                        catch
                        {
                            queue = new();
                        }
                    }

                    queue.AddRange(batch);

                    // Bound offline queue to prevent unbounded disk growth
                    if (queue.Count > MaxOfflineQueueItems)
                    {
                        queue.RemoveRange(0, queue.Count - MaxOfflineQueueItems);
                    }

                    File.WriteAllText(_offlineQueuePath, JsonSerializer.Serialize(queue, new JsonSerializerOptions { WriteIndented = true }));
                    System.Diagnostics.Debug.WriteLine($"[OperatorActionRepository] Queued {batch.Count} events offline ({reason}). Total queue: {queue.Count}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OperatorActionRepository] Critical: Failed to queue offline batch: {ex.Message}");
            }
        }

        public async Task<int> ProcessOfflineQueueAsync()
        {
            if (!File.Exists(_offlineQueuePath)) return 0;

            List<OperatorActionEvent>? queue = null;
            lock (_offlineLock)
            {
                try
                {
                    var json = File.ReadAllText(_offlineQueuePath);
                    queue = JsonSerializer.Deserialize<List<OperatorActionEvent>>(json);
                }
                catch
                {
                    return 0;
                }
            }

            if (queue == null || queue.Count == 0) return 0;

            int syncedCount = 0;
            const int chunkSize = 100;

            for (int i = 0; i < queue.Count; i += chunkSize)
            {
                var chunk = queue.GetRange(i, Math.Min(chunkSize, queue.Count - i));
                try
                {
                    await using var conn = await GetOpenConnectionAsync();

                    var sb = new StringBuilder();
                    sb.AppendLine(@"INSERT INTO public.""OperatorActionLog"" (
                        ""EventTimestamp"", ""ActionType"", ""MouseButton"", ""ControlName"", ""KeyName"", ""WindowTitle"", ""OperatorName"", ""SessionId"", ""ScreenX"", ""ScreenY""
                    ) VALUES ");

                    for (int c = 0; c < chunk.Count; c++)
                    {
                        if (c > 0) sb.Append(", ");
                        sb.Append($"(@ts{c}, @at{c}, @mb{c}, @cn{c}, @kn{c}, @wt{c}, @op{c}, @sid{c}, @sx{c}, @sy{c})");
                    }
                    sb.Append(";");

                    await using var cmd = new NpgsqlCommand(sb.ToString(), conn);

                    for (int c = 0; c < chunk.Count; c++)
                    {
                        var ev = chunk[c];
                        cmd.Parameters.AddWithValue($"@ts{c}", ev.EventTimestamp.ToUniversalTime());
                        cmd.Parameters.AddWithValue($"@at{c}", ev.ActionType ?? "Action");
                        cmd.Parameters.AddWithValue($"@mb{c}", (object?)ev.MouseButton ?? DBNull.Value);
                        cmd.Parameters.AddWithValue($"@cn{c}", (object?)ev.ControlName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue($"@kn{c}", (object?)ev.KeyName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue($"@wt{c}", (object?)ev.WindowTitle ?? DBNull.Value);
                        cmd.Parameters.AddWithValue($"@op{c}", (object?)ev.OperatorName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue($"@sid{c}", ev.SessionId);
                        cmd.Parameters.AddWithValue($"@sx{c}", (object?)ev.ScreenX ?? DBNull.Value);
                        cmd.Parameters.AddWithValue($"@sy{c}", (object?)ev.ScreenY ?? DBNull.Value);
                    }

                    await cmd.ExecuteNonQueryAsync();
                    syncedCount += chunk.Count;
                }
                catch
                {
                    break;
                }
            }

            lock (_offlineLock)
            {
                if (syncedCount >= queue.Count)
                {
                    try { File.Delete(_offlineQueuePath); } catch { }
                }
                else if (syncedCount > 0)
                {
                    queue.RemoveRange(0, syncedCount);
                    try { File.WriteAllText(_offlineQueuePath, JsonSerializer.Serialize(queue)); } catch { }
                }
            }

            return syncedCount;
        }

        #endregion
    }
}
