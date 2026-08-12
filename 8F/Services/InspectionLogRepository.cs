using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using _8F.Models;
using Npgsql;

namespace _8F.Services
{
    /// <summary>
    /// Thread-safe database repository for managing Eddy Current inspection logs in PostgreSQL.
    /// Includes connection pooling, parameterized queries, and an offline JSON fallback queue for resilience.
    /// </summary>
    public class InspectionLogRepository
    {
        private readonly string _connectionString;
        private readonly string _offlineQueuePath;

        public InspectionLogRepository(string? connectionString = null)
        {
            // 1. Connection string resolution priority:
            //    Explicit parameter > Environment Variable > App.config > Default fallback
            _connectionString = connectionString 
                ?? Environment.GetEnvironmentVariable("EDDY_DB_CONNECTION_STRING")
                ?? ConfigurationManager.AppSettings["ConnectionString"]
                ?? "Host=localhost;Port=5432;Username=postgres;Password=ary123;Database=Eddy;Pooling=true;Minimum Pool Size=2;Maximum Pool Size=20;";

            _offlineQueuePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Eddy", "offline_logs_queue.json");
        }

        #region 1. Connection Setup & Health Check

        /// <summary>
        /// Gets an open NpgsqlConnection wrapped in asynchronous handling.
        /// </summary>
        public async Task<NpgsqlConnection> GetOpenConnectionAsync()
        {
            var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            return conn;
        }

        /// <summary>
        /// Verifies PostgreSQL database connectivity.
        /// </summary>
        public async Task<(bool IsConnected, string Message)> TestConnectionAsync()
        {
            try
            {
                await using var conn = await GetOpenConnectionAsync();
                await using var cmd = new NpgsqlCommand("SELECT 1;", conn);
                var result = await cmd.ExecuteScalarAsync();

                return result != null && (int)result == 1
                    ? (true, "Database connection established successfully.")
                    : (false, "Unexpected response from database.");
            }
            catch (Exception ex)
            {
                return (false, $"Connection Failed: {ex.Message}");
            }
        }

        #endregion

        #region 2. Create / Insert Operations

        /// <summary>
        /// Inserts an inspection record into PostgreSQL. Automatically falls back to offline queue if DB is unreachable.
        /// </summary>
        public async Task<bool> InsertLogAsync(InspectionLog log)
        {
            const string sql = @"
                INSERT INTO public.""Logs"" (
                    ""SerialNumber"", ""BatchId"", ""OperatorName"", ""InspectionTimestamp"",
                    ""ChannelNumber"", ""FrequencyHz"", ""XValue"", ""YValue"",
                    ""ResultPass"", ""DefectType"", ""MachineId"", ""CreatedAt""
                ) VALUES (
                    @SerialNumber, @BatchId, @OperatorName, @InspectionTimestamp,
                    @ChannelNumber, @FrequencyHz, @XValue, @YValue,
                    @ResultPass, @DefectType, @MachineId, @CreatedAt
                )
                RETURNING ""Id"";";

            try
            {
                await using var conn = await GetOpenConnectionAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);

                cmd.Parameters.AddWithValue("@SerialNumber", log.SerialNumber ?? string.Empty);
                cmd.Parameters.AddWithValue("@BatchId", log.BatchId ?? string.Empty);
                cmd.Parameters.AddWithValue("@OperatorName", log.OperatorName ?? string.Empty);
                cmd.Parameters.AddWithValue("@InspectionTimestamp", log.InspectionTimestamp.ToUniversalTime());
                cmd.Parameters.AddWithValue("@ChannelNumber", log.ChannelNumber);
                cmd.Parameters.AddWithValue("@FrequencyHz", log.FrequencyHz);
                cmd.Parameters.AddWithValue("@XValue", log.XValue);
                cmd.Parameters.AddWithValue("@YValue", log.YValue);
                cmd.Parameters.AddWithValue("@ResultPass", log.ResultPass);
                cmd.Parameters.AddWithValue("@DefectType", (object?)log.DefectType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MachineId", log.MachineId ?? string.Empty);
                cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

                var insertedId = await cmd.ExecuteScalarAsync();
                if (insertedId != null && insertedId != DBNull.Value)
                {
                    log.Id = Convert.ToInt64(insertedId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                // Fallback: Queue record locally to prevent inspection data loss
                EnqueueOfflineLog(log, ex.Message);
                return false;
            }
        }

        #endregion

        #region 3. Read / Query Operations

        /// <summary>
        /// Queries inspection records by Batch ID.
        /// </summary>
        public async Task<List<InspectionLog>> GetLogsByBatchAsync(string batchId)
        {
            const string sql = @"
                SELECT ""Id"", ""SerialNumber"", ""BatchId"", ""OperatorName"", ""InspectionTimestamp"",
                       ""ChannelNumber"", ""FrequencyHz"", ""XValue"", ""YValue"", ""ResultPass"",
                       ""DefectType"", ""MachineId"", ""CreatedAt""
                FROM public.""Logs""
                WHERE ""BatchId"" = @BatchId
                ORDER BY ""InspectionTimestamp"" ASC;";

            var list = new List<InspectionLog>();

            try
            {
                await using var conn = await GetOpenConnectionAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@BatchId", batchId);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(MapLog(reader));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error querying by BatchId: {ex.Message}");
            }

            return list;
        }

        /// <summary>
        /// Queries inspection records by Component Serial Number.
        /// </summary>
        public async Task<List<InspectionLog>> GetLogsBySerialNumberAsync(string serialNumber)
        {
            const string sql = @"
                SELECT ""Id"", ""SerialNumber"", ""BatchId"", ""OperatorName"", ""InspectionTimestamp"",
                       ""ChannelNumber"", ""FrequencyHz"", ""XValue"", ""YValue"", ""ResultPass"",
                       ""DefectType"", ""MachineId"", ""CreatedAt""
                FROM public.""Logs""
                WHERE ""SerialNumber"" = @SerialNumber
                ORDER BY ""InspectionTimestamp"" DESC;";

            var list = new List<InspectionLog>();

            try
            {
                await using var conn = await GetOpenConnectionAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@SerialNumber", serialNumber);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(MapLog(reader));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error querying by SerialNumber: {ex.Message}");
            }

            return list;
        }

        /// <summary>
        /// Queries inspection records within a specified date range.
        /// </summary>
        public async Task<List<InspectionLog>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            const string sql = @"
                SELECT ""Id"", ""SerialNumber"", ""BatchId"", ""OperatorName"", ""InspectionTimestamp"",
                       ""ChannelNumber"", ""FrequencyHz"", ""XValue"", ""YValue"", ""ResultPass"",
                       ""DefectType"", ""MachineId"", ""CreatedAt""
                FROM public.""Logs""
                WHERE ""InspectionTimestamp"" BETWEEN @StartDate AND @EndDate
                ORDER BY ""InspectionTimestamp"" DESC;";

            var list = new List<InspectionLog>();

            try
            {
                await using var conn = await GetOpenConnectionAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@StartDate", startDate.ToUniversalTime());
                cmd.Parameters.AddWithValue("@EndDate", endDate.ToUniversalTime());

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(MapLog(reader));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error querying by DateRange: {ex.Message}");
            }

            return list;
        }

        #endregion

        #region 4. Delete / Maintenance Operations

        /// <summary>
        /// Purges logs older than a specified number of days to maintain database size.
        /// </summary>
        public async Task<int> DeleteLogsOlderThanDaysAsync(int daysToKeep)
        {
            const string sql = @"
                DELETE FROM public.""Logs""
                WHERE ""InspectionTimestamp"" < @CutoffDate;";

            try
            {
                await using var conn = await GetOpenConnectionAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@CutoffDate", DateTime.UtcNow.AddDays(-daysToKeep));

                return await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error purging logs: {ex.Message}");
                return 0;
            }
        }

        #endregion

        #region 5. Offline Queue Fallback & Sync

        private void EnqueueOfflineLog(InspectionLog log, string reason)
        {
            try
            {
                var dir = Path.GetDirectoryName(_offlineQueuePath);
                if (!Directory.Exists(dir) && dir != null)
                {
                    Directory.CreateDirectory(dir);
                }

                List<InspectionLog> queue = new();
                if (File.Exists(_offlineQueuePath))
                {
                    var existingJson = File.ReadAllText(_offlineQueuePath);
                    queue = JsonSerializer.Deserialize<List<InspectionLog>>(existingJson) ?? new();
                }

                queue.Add(log);
                File.WriteAllText(_offlineQueuePath, JsonSerializer.Serialize(queue, new JsonSerializerOptions { WriteIndented = true }));
                System.Diagnostics.Debug.WriteLine($"Log queued offline ({reason}). Current queue size: {queue.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Critical: Failed to queue offline log: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes and syncs locally queued logs once PostgreSQL connectivity is restored.
        /// </summary>
        public async Task<int> ProcessOfflineQueueAsync()
        {
            if (!File.Exists(_offlineQueuePath)) return 0;

            List<InspectionLog>? queue;
            try
            {
                var json = await File.ReadAllTextAsync(_offlineQueuePath);
                queue = JsonSerializer.Deserialize<List<InspectionLog>>(json);
                if (queue == null || queue.Count == 0) return 0;
            }
            catch
            {
                return 0;
            }

            int syncedCount = 0;
            var remainingQueue = new List<InspectionLog>();

            foreach (var log in queue)
            {
                bool success = await InsertLogAsync(log);
                if (success)
                {
                    syncedCount++;
                }
                else
                {
                    remainingQueue.Add(log);
                }
            }

            if (remainingQueue.Count > 0)
            {
                await File.WriteAllTextAsync(_offlineQueuePath, JsonSerializer.Serialize(remainingQueue));
            }
            else
            {
                File.Delete(_offlineQueuePath);
            }

            return syncedCount;
        }

        #endregion

        #region Helper Mapper

        private static InspectionLog MapLog(NpgsqlDataReader reader)
        {
            return new InspectionLog
            {
                Id = reader.GetInt64(reader.GetOrdinal("Id")),
                SerialNumber = reader.GetString(reader.GetOrdinal("SerialNumber")),
                BatchId = reader.GetString(reader.GetOrdinal("BatchId")),
                OperatorName = reader.GetString(reader.GetOrdinal("OperatorName")),
                InspectionTimestamp = reader.GetDateTime(reader.GetOrdinal("InspectionTimestamp")),
                ChannelNumber = reader.GetInt32(reader.GetOrdinal("ChannelNumber")),
                FrequencyHz = reader.GetDouble(reader.GetOrdinal("FrequencyHz")),
                XValue = reader.GetDouble(reader.GetOrdinal("XValue")),
                YValue = reader.GetDouble(reader.GetOrdinal("YValue")),
                ResultPass = reader.GetBoolean(reader.GetOrdinal("ResultPass")),
                DefectType = reader.IsDBNull(reader.GetOrdinal("DefectType")) ? null : reader.GetString(reader.GetOrdinal("DefectType")),
                MachineId = reader.GetString(reader.GetOrdinal("MachineId")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            };
        }

        #endregion
    }
}
