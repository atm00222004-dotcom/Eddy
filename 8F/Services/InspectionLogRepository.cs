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
        private readonly string _autoEllipseOfflineQueuePath;

        public InspectionLogRepository(string? connectionString = null)
        {
            // 1. Connection string resolution priority:
            //    Explicit parameter > Environment Variable > App.config > Default fallback
            _connectionString = connectionString 
                ?? Environment.GetEnvironmentVariable("EDDY_DB_CONNECTION_STRING")
                ?? ConfigurationManager.AppSettings["ConnectionString"]
                ?? "Host=localhost;Port=5432;Username=postgres;Password=ary123;Database=Eddy;Pooling=true;Minimum Pool Size=2;Maximum Pool Size=20;";

            var eddyDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Eddy");

            _offlineQueuePath = Path.Combine(eddyDataFolder, "offline_logs_queue.json");
            _autoEllipseOfflineQueuePath = Path.Combine(eddyDataFolder, "auto_ellipse_offline_queue.json");
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

        #region 6. Auto Ellipse Operations

        /// <summary>
        /// Inserts a raw multi-frequency Auto Ellipse test run into PostgreSQL.
        /// </summary>
        public async Task<bool> InsertAutoEllipseTestAsync(AutoEllipseTest test)
        {
            const string sql = @"
                INSERT INTO public.""AutoEllipseTests"" (
                    ""ChId"", ""TestNumber"", ""TimeStamp"", ""OperatorName"", ""FrequencyValues"", ""IsDeleted""
                ) VALUES (
                    @ChId, @TestNumber, @TimeStamp, @OperatorName, @FrequencyValues::json, @IsDeleted
                )
                RETURNING ""Id"";";

            try
            {
                await using var conn = await GetOpenConnectionAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);

                cmd.Parameters.AddWithValue("@ChId", test.ChannelId);
                cmd.Parameters.AddWithValue("@TestNumber", test.TestNumber);
                cmd.Parameters.AddWithValue("@TimeStamp", test.TimeStamp.ToUniversalTime());
                cmd.Parameters.AddWithValue("@OperatorName", (object?)test.OperatorName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FrequencyValues", string.IsNullOrWhiteSpace(test.FrequencyValuesJson) ? "{}" : test.FrequencyValuesJson);
                cmd.Parameters.AddWithValue("@IsDeleted", test.IsDeleted);

                var insertedId = await cmd.ExecuteScalarAsync();
                if (insertedId != null && insertedId != DBNull.Value)
                {
                    test.Id = Convert.ToInt64(insertedId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                EnqueueOfflineAutoEllipseTest(test, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Soft-deletes a raw Auto Ellipse test run by setting IsDeleted = true.
        /// </summary>
        public async Task<bool> SoftDeleteAutoEllipseTestAsync(long testId)
        {
            const string sql = @"
                UPDATE public.""AutoEllipseTests""
                SET ""IsDeleted"" = TRUE
                WHERE ""Id"" = @Id;";

            try
            {
                await using var conn = await GetOpenConnectionAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id", testId);

                int rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error soft deleting AutoEllipse test record: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Fetches all active (non-deleted) raw Auto Ellipse test runs for a given channel from PostgreSQL.
        /// </summary>
        public async Task<List<AutoEllipseTest>> GetAutoEllipseTestsByChannelAsync(int channelId)
        {
            List<AutoEllipseTest> list = new();
            const string sql = @"
                SELECT ""Id"", ""ChId"", ""TestNumber"", ""TimeStamp"", ""OperatorName"", ""FrequencyValues""
                FROM public.""AutoEllipseTests""
                WHERE ""ChId"" = @ChId AND ""IsDeleted"" = FALSE
                ORDER BY ""TestNumber"" ASC;";

            try
            {
                await using var conn = await GetOpenConnectionAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ChId", channelId);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new AutoEllipseTest
                    {
                        Id = reader.GetInt64(0),
                        ChannelId = reader.GetInt32(1),
                        TestNumber = reader.GetInt32(2),
                        TimeStamp = reader.GetDateTime(3),
                        OperatorName = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                        FrequencyValuesJson = reader.IsDBNull(5) ? "{}" : reader.GetString(5),
                        IsDeleted = false
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAutoEllipseTestsByChannelAsync error: {ex.Message}");
            }
            return list;
        }

        /// <summary>
        /// Inserts a computed Auto Ellipse result record into PostgreSQL for audit logging.
        /// </summary>
        public async Task<bool> InsertAutoEllipseResultAsync(AutoEllipseResultRecord result)
        {
            const string sql = @"
                INSERT INTO public.""AutoEllipseResults"" (
                    ""ChId"", ""Frequency"", ""TimeStamp"", ""SelectedTestIds"",
                    ""ComputedCenterX"", ""ComputedCenterY"",
                    ""ComputedWidth"", ""ComputedHeight"", ""ComputedRotationAngle"",
                    ""SampleCount""
                ) VALUES (
                    @ChId, @Frequency, @TimeStamp, @SelectedTestIds::json,
                    @ComputedCenterX, @ComputedCenterY,
                    @ComputedWidth, @ComputedHeight, @ComputedRotationAngle,
                    @SampleCount
                )
                RETURNING ""Id"";";

            try
            {
                await using var conn = await GetOpenConnectionAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);

                cmd.Parameters.AddWithValue("@ChId", result.ChannelId);
                cmd.Parameters.AddWithValue("@Frequency", result.Frequency ?? string.Empty);
                cmd.Parameters.AddWithValue("@TimeStamp", result.TimeStamp.ToUniversalTime());
                cmd.Parameters.AddWithValue("@SelectedTestIds", string.IsNullOrWhiteSpace(result.SelectedTestIdsJson) ? "[]" : result.SelectedTestIdsJson);
                cmd.Parameters.AddWithValue("@ComputedCenterX", result.ComputedCenterX);
                cmd.Parameters.AddWithValue("@ComputedCenterY", result.ComputedCenterY);
                cmd.Parameters.AddWithValue("@ComputedWidth", result.ComputedWidth);
                cmd.Parameters.AddWithValue("@ComputedHeight", result.ComputedHeight);
                cmd.Parameters.AddWithValue("@ComputedRotationAngle", result.ComputedRotationAngle);
                cmd.Parameters.AddWithValue("@SampleCount", result.SampleCount);

                var insertedId = await cmd.ExecuteScalarAsync();
                if (insertedId != null && insertedId != DBNull.Value)
                {
                    result.Id = Convert.ToInt64(insertedId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error inserting AutoEllipse result audit: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Updates AutoEllipseTests rows for a channel setting Applied = true and AppliedTimeStamp = now.
        /// </summary>
        public async Task<bool> UpdateAutoEllipseTestsAppliedAsync(int channelId)
        {
            const string sql = @"
                UPDATE public.""AutoEllipseTests""
                SET ""Applied"" = TRUE, ""AppliedTimeStamp"" = @AppliedTimeStamp
                WHERE ""ChId"" = @ChId AND ""Applied"" = FALSE;";

            try
            {
                await using var conn = await GetOpenConnectionAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ChId", channelId);
                cmd.Parameters.AddWithValue("@AppliedTimeStamp", DateTime.UtcNow);

                int rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating AutoEllipseTests applied state: {ex.Message}");
                return false;
            }
        }

        private void EnqueueOfflineAutoEllipseTest(AutoEllipseTest test, string reason)
        {
            try
            {
                var dir = Path.GetDirectoryName(_autoEllipseOfflineQueuePath);
                if (!Directory.Exists(dir) && dir != null)
                {
                    Directory.CreateDirectory(dir);
                }

                List<AutoEllipseTest> queue = new();
                if (File.Exists(_autoEllipseOfflineQueuePath))
                {
                    var json = File.ReadAllText(_autoEllipseOfflineQueuePath);
                    queue = JsonSerializer.Deserialize<List<AutoEllipseTest>>(json) ?? new();
                }

                queue.Add(test);
                File.WriteAllText(_autoEllipseOfflineQueuePath, JsonSerializer.Serialize(queue, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Critical: Failed to queue offline auto ellipse test: {ex.Message}");
            }
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
