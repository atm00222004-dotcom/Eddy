namespace _8F.Models
{
    public class LogData
    {
        public string BatchName { get; set; } = default!;
        public string LogStartDate { get; set; } = default!;
        public string LogEndDate { get; set; } = default!;
        public int PassCount { get; set; }
        public int FailCount { get; set; }
        public int TotalCount { get; set; }
    }
}
