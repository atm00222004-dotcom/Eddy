using System;
using _8F.Models;

namespace _8F.Services.Interfaces
{
    public interface IInspectionLogger
    {
        void WriteLog(int chId, bool result, DateTime timeStamp, Response res);
        void WriteLogCSV(bool result, DateTime timeStamp, Response res);
        string GetCSVDirectoryPath();
    }
}
