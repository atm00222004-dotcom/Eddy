using Npgsql;
using ConfigurationKeyGenerator.Models;

namespace ConfigurationKeyGenerator.Services
{
    public class ConfigurationKeyLogService
    {
        private readonly string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=aryan123;Database=EddyShorter";
        public void Save(ConfigurationKeyLog log)
        {

            using NpgsqlConnection con = new NpgsqlConnection(connectionString);


            con.Open();


            string query = @"
                INSERT INTO ConfigurationKeyLogs
                (
                    ProductName,
                    CustomerName,
                    MachineId,
                    ConfigurationFileName,
                    GeneratedFileName,
                    GeneratedFile
                )
                VALUES
                (
                    @product,
                    @customer,
                    @machine,
                    @configFile,
                    @generatedFile,
                    @file
                )";


            using NpgsqlCommand cmd =
                new NpgsqlCommand(query, con);


            cmd.Parameters.AddWithValue("@product",
                log.ProductName);

            cmd.Parameters.AddWithValue("@customer",
                log.CustomerName);

            cmd.Parameters.AddWithValue("@machine",
                log.MachineId);

            cmd.Parameters.AddWithValue("@configFile",
                log.ConfigurationFileName);

            cmd.Parameters.AddWithValue("@generatedFile",
                log.GeneratedFileName);

            cmd.Parameters.AddWithValue("@file",
                log.GeneratedFile);


            cmd.ExecuteNonQuery();
        }
        public List<ConfigurationKeyLog> GetAll()
        {
            try
            {
                List<ConfigurationKeyLog> list = new();


                using NpgsqlConnection con = new NpgsqlConnection(connectionString);


                con.Open();


                string query =
                "SELECT * FROM ConfigurationKeyLogs ORDER BY Id DESC";


                using NpgsqlCommand cmd =
                    new NpgsqlCommand(query, con);


                using NpgsqlDataReader reader =
                    cmd.ExecuteReader();


                while (reader.Read())
                {
                    list.Add(new ConfigurationKeyLog
                    {
                        Id = reader.GetInt32(0),

                        ProductName = reader.GetString(1),

                        CustomerName = reader.GetString(2),

                        MachineId = reader.GetString(3),

                        ConfigurationFileName = reader.GetString(4),

                        GeneratedFileName = reader.GetString(5),

                        GeneratedFile = (byte[])reader[6],

                        GeneratedDate = reader.GetDateTime(7),

                        UpdatedDate = reader.IsDBNull(8)? null: reader.GetDateTime(8)
                    });
                }


                return list;
            }
            catch (Exception)
            {
                throw;
            }

        }
        public bool ExistsByMachineId(string machineId)
        {
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);

            con.Open();

            string query = @"
        SELECT COUNT(1)
        FROM ConfigurationKeyLogs
        WHERE MachineId = @machineId";


            using NpgsqlCommand cmd = new NpgsqlCommand(query, con);

            cmd.Parameters.AddWithValue("@machineId", machineId);


            int count = Convert.ToInt32(cmd.ExecuteScalar());


            return count > 0;
        }

        public void Update(ConfigurationKeyLog log)
        {
            using NpgsqlConnection con = new NpgsqlConnection(connectionString);

            con.Open();

            string query = @"
        UPDATE ConfigurationKeyLogs
        SET
            ProductName = @product,
            CustomerName = @customer,
            MachineId = @machine,
            ConfigurationFileName = @configFile,
            GeneratedFileName = @generatedFile,
            GeneratedFile = @file,
            UpdatedDate = NOW()
        WHERE Id = @id";

            using NpgsqlCommand cmd = new NpgsqlCommand(query, con);

            cmd.Parameters.AddWithValue("@id", log.Id);

            cmd.Parameters.AddWithValue("@product", log.ProductName);

            cmd.Parameters.AddWithValue("@customer", log.CustomerName);

            cmd.Parameters.AddWithValue("@machine", log.MachineId);

            cmd.Parameters.AddWithValue("@configFile", log.ConfigurationFileName);

            cmd.Parameters.AddWithValue("@generatedFile", log.GeneratedFileName);

            cmd.Parameters.AddWithValue("@file", log.GeneratedFile);

            cmd.ExecuteNonQuery();
        }
    }
}