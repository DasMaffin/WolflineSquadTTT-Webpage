using System.Text.Json;
using System.Web;

namespace WolflineSquadTTT.Services
{
    public interface ISteamService
    {
        Task<KeyValuePair<ulong, string>> GetWorkshopPreviewImageAsync(ulong workshopId);
        Task<Dictionary<ulong, string>> GetWorkshopPreviewImagesAsync(List<ulong> workshopIds);
        Task<string> GetPrettyNameAsync(ulong steamId);
        Task<Dictionary<ulong, string>> GetPrettyNamesAsync(IEnumerable<ulong> steamIds);
    }

    public class SteamService : ISteamService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ISteamNameCache _nameCache;

        public SteamService(HttpClient httpClient, IConfiguration config, ISteamNameCache nameCache)
        {
            _httpClient = httpClient;
            _apiKey = config["SteamApi:ApiKey"]!;
            if (_apiKey == null) throw new ArgumentNullException();
            _nameCache = nameCache;
        }

        private string BuildWorkshopDetailsUrl(ulong publishedFileId)
        {
            var baseUri = new Uri("https://api.steampowered.com/IPublishedFileService/GetDetails/v1/");
            var builder = new UriBuilder(baseUri);

            var query = HttpUtility.ParseQueryString(builder.Query);
            query["key"] = _apiKey;
            query["itemcount"] = "1";
            query["publishedfileids[0]"] = publishedFileId.ToString();

            builder.Query = query.ToString();
            return builder.ToString();
        }

        public async Task<KeyValuePair<ulong, string>> GetWorkshopPreviewImageAsync(ulong publishedFileId)
        {
            string url = BuildWorkshopDetailsUrl(publishedFileId);

            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();

            using JsonDocument doc = JsonDocument.Parse(json);

            string previewUrl = doc.RootElement
                .GetProperty("response")
                .GetProperty("publishedfiledetails")[0]
                .GetProperty("preview_url")
                .GetString() ?? string.Empty;

            if(previewUrl == null) previewUrl = string.Empty;

            KeyValuePair<ulong, string> ret = new KeyValuePair<ulong, string>(publishedFileId, previewUrl);

            return ret;
        }

        public async Task<Dictionary<ulong, string>> GetWorkshopPreviewImagesAsync(List<ulong> publishedFileIds)
        {
            IDictionary<ulong, string> ret = new Dictionary<ulong, string>();

            foreach(ulong fileId in publishedFileIds)
            {
                ret.Add(await GetWorkshopPreviewImageAsync(fileId));
            }

            return (Dictionary<ulong, string>)ret;
        }

        public async Task<string> GetPrettyNameAsync(ulong steamId)
        {
            if (_nameCache.TryGet(steamId, out var cachedName))
                return cachedName;

            try
            {
                var url = $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key={_apiKey}&steamids={steamId}";
                var response = await _httpClient.GetStringAsync(url);
                using var doc = JsonDocument.Parse(response);

                var player = doc.RootElement
                                .GetProperty("response")
                                .GetProperty("players")[0];

                var name = player.GetProperty("personaname").GetString() ?? $"Unknown ({steamId})";

                _nameCache.Set(steamId, name);
                return name;
            }
            catch
            {
                return $"Unknown ({steamId})";
            }
        }

        public async Task<Dictionary<ulong, string>> GetPrettyNamesAsync(IEnumerable<ulong> steamIds)
        {
            var distinct = steamIds.Distinct().ToList();
            var result = new Dictionary<ulong, string>();
            var toFetch = new List<ulong>();

            foreach (var id in distinct)
            {
                if (_nameCache.TryGet(id, out var cached))
                    result[id] = cached;
                else
                    toFetch.Add(id);
            }

            // GetPlayerSummaries accepts up to 100 ids per request — batch to stay well under rate limits.
            foreach (var batch in toFetch.Chunk(100))
            {
                try
                {
                    var url = $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key={_apiKey}&steamids={string.Join(",", batch)}";
                    var response = await _httpClient.GetStringAsync(url);
                    using var doc = JsonDocument.Parse(response);

                    foreach (var player in doc.RootElement.GetProperty("response").GetProperty("players").EnumerateArray())
                    {
                        var idStr = player.GetProperty("steamid").GetString();
                        var name = player.GetProperty("personaname").GetString();
                        if (ulong.TryParse(idStr, out var sid) && name != null)
                        {
                            _nameCache.Set(sid, name);
                            result[sid] = name;
                        }
                    }
                }
                catch
                {
                    // leave unresolved ids to the fallback below
                }
            }

            foreach (var id in distinct)
            {
                if (!result.ContainsKey(id))
                    result[id] = $"Unknown ({id})";
            }

            return result;
        }
    }
}
