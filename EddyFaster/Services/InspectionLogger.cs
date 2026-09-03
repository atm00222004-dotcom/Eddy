using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Npgsql;
using _8F.Models;
using _8F.Services.Interfaces;

namespace _8F.Services
{
    public class InspectionLogger : IInspectionLogger
    {
        public void WriteLog(int ChId, bool Result, DateTime TimeStamp, Response res)
        {
            try
            {
                using (var con = new NpgsqlConnection(DeviceCOM.ConnectionString))
                {
                    string sql = string.Empty;
                    con.Open();
                    var fdData = JsonConvert.SerializeObject(DeviceCOM.channelDatas.FirstOrDefault(r => r.Id == ChId)?.graphDatas);
                    
                    var partData = JsonConvert.SerializeObject(DeviceCOM.part);
                    if (ChId == 1)
                    {
                        sql = "INSERT INTO public.\"Logs\"(\"ChId\", \"Result\", \"FDData\", \"PartData\", \"PartName\", \"BatchName\", \"SrNo\", \"BatchNo\", \"TimeStamp\") " +
                              "VALUES (@ChId, @Result, @FDData, @PartData, @PartName, @BatchName, @SrNo, @BatchNo, @TimeStamp); " +
                              "SELECT count(1) FROM public.\"Logs\" WHERE \"BatchName\" = @BatchName AND \"BatchNo\" = @BatchNo;";

                        var cmd = new NpgsqlCommand(sql, con);
                        cmd.Parameters.AddWithValue("@ChId", ChId);
                        cmd.Parameters.AddWithValue("@Result", Result.ToString());
                        cmd.Parameters.AddWithValue("@FDData", fdData ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PartData", partData ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PartName", DeviceCOM.part?.Name ?? "");
                        cmd.Parameters.AddWithValue("@BatchName", DeviceCOM.part?.BatchName ?? "");
                        cmd.Parameters.AddWithValue("@SrNo", DeviceCOM.Code ?? "");
                        cmd.Parameters.AddWithValue("@BatchNo", DeviceCOM.part?.BatchNo ?? 0);
                        cmd.Parameters.AddWithValue("@TimeStamp", TimeStamp);
                        var count = cmd.ExecuteScalar();

                        if (DeviceCOM.part?.BatchType == 1)
                        {
                            if (Convert.ToInt32(count) == DeviceCOM.part.BatchSize)
                            {
                                // stop the logging 
                            }

                            DeviceCOM.part.BatchNo = DeviceCOM.part.BatchNo + 1;
                        }
                    }
                    else
                    {
                        if (!Result)
                        {
                            sql = "update public.\"Logs\"  set \"Result\" = 'false' where \"Id\" = (select max(\"Id\") from public.\"Logs\"); select 1";
                            var cmd = new NpgsqlCommand(sql, con);
                            var count = cmd.ExecuteScalar();
                        }
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        public string GetCSVDirectoryPath()
        {
            string? configPath = System.Configuration.ConfigurationManager.AppSettings["CSVPath"]?.ToString();
            if (!string.IsNullOrWhiteSpace(configPath))
            {
                try
                {
                    if (Directory.Exists(configPath))
                    {
                        return configPath;
                    }
                    else
                    {
                        Directory.CreateDirectory(configPath);
                        if (Directory.Exists(configPath))
                        {
                            return configPath;
                        }
                    }
                }
                catch
                {
                    // Specified path/drive is invalid or inaccessible
                }
            }

            // Fallback path: %LOCALAPPDATA%\EddyFaster\Data
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string fallbackPath = System.IO.Path.Combine(localAppData, "EddyFaster", "Data");
            if (!Directory.Exists(fallbackPath))
            {
                Directory.CreateDirectory(fallbackPath);
            }
            return fallbackPath;
        }

        public void WriteLogCSV(bool Result, DateTime TimeStamp, Response res)
        {
            try
            {
                // Write to CSV File
                var ch = DeviceCOM.channelDatas.FirstOrDefault(c => c.Id == res.CN);
                if (ch != null)
                {
                    List<string> lines = new List<string>();
                    var FileName = "EddyLog_" + System.DateTime.Now.ToString("yyyy-MM-dd");
                    string csvDir = GetCSVDirectoryPath();
                    string FilePath = System.IO.Path.Combine(csvDir, FileName + ".csv");
                    if (!File.Exists(FilePath))
                    {
                        string line = "TimeStamp,Code,Operator Name,Result";
                        foreach (var fd in ch.graphDatas)
                        {
                            line = line + ",Frequency Result_" + fd.Id.ToString() + ",Frequency_" + fd.Id.ToString();
                        }
                        lines.Add(line);
                    }

                    string data = System.DateTime.Now.ToString() + ","+ DeviceCOM.Code.Replace("\n", "").Replace("\r","") + "," + DeviceCOM.part.CheckedBy + "," + (Result == true ? "Ok" : "No Ok");

                    foreach (var fd in res.FD)
                    {
                        var Gdata = ch.graphDatas.FirstOrDefault(d => d.Id == fd.FN);
                        if (Gdata != null)
                        {
                            data = data + "," + (fd.R == 1 ? "Ok" : "No Ok") + "," + Gdata.freq.ToString();
                        }
                    }

                    lines.Add(data);

                    if (lines.Count > 0)
                    {
                        File.AppendAllLines(FilePath, lines);
                    }
                }
            }
            catch (Exception)
            {

            }
        }
    }
}
