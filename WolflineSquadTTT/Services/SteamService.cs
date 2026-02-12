using System.Text.Json;
using System.Web;

namespace WolflineSquadTTT.Services
{
    public interface ISteamService
    {
        Task<KeyValuePair<ulong, string>> GetWorkshopPreviewImageAsync(ulong workshopId);
        Task<Dictionary<ulong, string>> GetWorkshopPreviewImagesAsync(List<ulong> workshopIds);
    }

    public class SteamService : ISteamService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public SteamService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["SteamApi:ApiKey"]!;
            if (_apiKey == null) throw new ArgumentNullException();
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
    }
}
