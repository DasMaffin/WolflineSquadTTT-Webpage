using System.Text.Json;
using WolflineSquadTTT.Models;

namespace WolflineSquadTTT.Services
{

    public class DataWriterService
    {
        private readonly string _filePath;
        private readonly string _roundDataPath;
        private static readonly object _roundDataLock = new();

        public DataWriterService(IWebHostEnvironment env)
        {
            // Ensure wwwroot/data exists
            var dataDir = Path.Combine(env.WebRootPath, "data");
            if (!Directory.Exists(dataDir))
                Directory.CreateDirectory(dataDir);

            _filePath = Path.Combine(dataDir, "golden_deagle_shots.json");
            _roundDataPath = Path.Combine(dataDir, "roundData.json");

            // Initialize file if missing
            if (!File.Exists(_filePath))
                File.WriteAllText(_filePath, "[]");
        }

        public void AppendData(GoldenDeagleShots data)
        {
            string json = File.ReadAllText(_filePath);
            List<GoldenDeagleShots> list = JsonSerializer.Deserialize<List<GoldenDeagleShots>>(json) ?? new List<GoldenDeagleShots>();
            list.Add(data);
            File.WriteAllText(_filePath, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
        }

        // Full replace of the player-activity dataset that StatsController reads
        // (wwwroot/data/roundData.json). Used by the game server's stats upload API.
        public void WriteRoundData(Dictionary<string, List<RoundEntry>> data)
        {
            string json = JsonSerializer.Serialize(data);
            lock (_roundDataLock)
            {
                File.WriteAllText(_roundDataPath, json);
            }
        }
    }
}
