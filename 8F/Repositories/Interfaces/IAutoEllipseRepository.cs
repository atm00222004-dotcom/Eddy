using System.Collections.Generic;
using System.Threading.Tasks;
using _8F.Models;

namespace _8F.Services
{
    public interface IAutoEllipseRepository
    {
        Task<bool> InsertAutoEllipseTestAsync(AutoEllipseTest test);
        Task<bool> DeleteAutoEllipseTestAsync(long testId);
        Task<List<AutoEllipseTest>> GetAutoEllipseTestsByChannelAsync(int channelId);
        Task<bool> InsertAutoEllipseResultAsync(AutoEllipseResultRecord result);
        Task<bool> UpdateAutoEllipseTestsAppliedAsync(int channelId);
    }
}
