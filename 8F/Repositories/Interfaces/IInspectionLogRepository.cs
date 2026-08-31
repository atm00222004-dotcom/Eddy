using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _8F.Models;
using Npgsql;

namespace _8F.Services
{
    public interface IInspectionLogRepository
    {
        Task<NpgsqlConnection> GetOpenConnectionAsync();
        Task<(bool IsConnected, string Message)> TestConnectionAsync();
        Task<bool> InsertLogAsync(InspectionLog log);
        Task<List<InspectionLog>> GetLogsByBatchAsync(string batchId);
        Task<List<InspectionLog>> GetLogsBySerialNumberAsync(string serialNumber);
        Task<List<InspectionLog>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<int> DeleteLogsOlderThanDaysAsync(int daysToKeep);
        Task<int> ProcessOfflineQueueAsync();
    }
}
