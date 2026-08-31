using System;
using System.Threading.Tasks;
using Xunit;
using _8F.Services;

namespace _8F.Tests
{
    public class DbVerificationTest
    {
        [Fact]
        public async Task VerifyDatabaseTables()
        {
            var repo = new InspectionLogRepository("Host=localhost;Port=5432;Username=postgres;Password=aryan123;Database=EddyShorter;Timeout=1;");
            var (isConnected, message) = await repo.TestConnectionAsync();
            Console.WriteLine($"DB Connection Test: IsConnected={isConnected}, Message={message}");

            if (isConnected)
            {
                await repo.EnsureConfigTablesCreatedAsync();
                var profiles = await repo.ListConfigProfilesAsync();
                Assert.NotNull(profiles);
                Console.WriteLine($"ConfigProfiles Table verified successfully! Total profiles in DB: {profiles.Count}");
            }
            else
            {
                Console.WriteLine("PostgreSQL server is currently offline or unreachable with current connection string.");
            }
        }
    }
}
