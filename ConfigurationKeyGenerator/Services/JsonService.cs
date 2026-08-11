using System.Text.Json;

namespace ConfigurationKeyGenerator.Services
{
    public class JsonService
    {
        private readonly JsonSerializerOptions _options;

        public JsonService()
        {
            _options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
        }
        public string Serialize<T>(T obj)
        {
            return JsonSerializer.Serialize(obj, _options);
        }
        public T? Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, _options);
        }
    }
}