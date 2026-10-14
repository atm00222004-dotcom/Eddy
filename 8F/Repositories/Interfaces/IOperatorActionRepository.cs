using System.Collections.Generic;
using System.Threading.Tasks;
using _8F.Models;

namespace _8F.Services
{
    
    public interface IOperatorActionRepository
    {
        
        Task<bool> InsertBatchAsync(IReadOnlyList<OperatorActionEvent> events);

        bool InsertBatchSync(IReadOnlyList<OperatorActionEvent> events);

        bool InsertEventSync(OperatorActionEvent ev);

        Task<int> DeleteOlderThanDaysAsync(int days);

        Task<int> ProcessOfflineQueueAsync();
    }
}
