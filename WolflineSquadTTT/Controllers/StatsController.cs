using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WolflineSquadTTT.Infrastructure.Security;
using WolflineSquadTTT.Models;
using WolflineSquadTTT.Models.Enums;
using WolflineSquadTTT.Services;

namespace WolflineSquadTTT.Controllers
{
    [RequiresPermission(Permission.ViewPlayerActivity)]
    public class StatsController : Controller
    {

        private readonly ISteamService _steamService;
        public StatsController(ISteamService steamService)
        {
            _steamService = steamService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new StatsViewModel());
        }

        [HttpPost]
        [Route("Stats")]
        public async Task<IActionResult> Index(StatsViewModel model)
        {
            Dictionary<string, List<RoundEntry>> data = new Dictionary<string, List<RoundEntry>>(); // Website is limited to 256MB and this is a lot of data. Thus we dont cache it.

            string json = System.IO.File.ReadAllText("wwwroot/data/roundData.json");
            data = JsonSerializer.Deserialize<Dictionary<string, List<RoundEntry>>>(json) ?? new Dictionary<string, List<RoundEntry>>();
            if(data == null || data.Count == 0)
            {
                throw new FileNotFoundException("File not found. Was it forgotten to be added to the filesystem?");
            }

            if (model.StartDate == null || model.EndDate == null)
                return View(model);

            long start = ((DateTimeOffset)model.StartDate.Value).ToUnixTimeSeconds();
            long end = ((DateTimeOffset)model.EndDate.Value.AddDays(1)).ToUnixTimeSeconds();

            Dictionary<string, double[]> resolvedResult = new Dictionary<string, double[]>();
            Dictionary<string, double[]> hourlyBySteamId = new Dictionary<string, double[]>();

            foreach (string steamId in model.SteamIds)
            {
                if (!data.ContainsKey(steamId))
                    continue;

                double[] hourly = new double[24];

                foreach (RoundEntry round in data[steamId])
                {
                    if (!round.playing) continue;

                    DateTimeOffset roundStart = DateTimeOffset
                        .FromUnixTimeSeconds(round.startTime)
                        .ToLocalTime()
                        .DateTime;

                    DateTimeOffset roundEnd = DateTimeOffset
                        .FromUnixTimeSeconds(round.endTime)
                        .ToLocalTime()
                        .DateTime;

                    if (roundEnd < model.StartDate || roundStart > model.EndDate)
                        continue;

                    // Clip to selected timeframe
                    if (roundStart < model.StartDate)
                        roundStart = model.StartDate.Value;

                    if (roundEnd > model.EndDate.Value.AddDays(1))
                        roundEnd = model.EndDate.Value.AddDays(1);

                    DateTimeOffset current = roundStart;

                    while (current < roundEnd)
                    {
                        int hour = current.Hour;

                        hourly[hour] += 1; // add exactly 1 minute

                        current = current.AddMinutes(1);
                    }
                }

                hourlyBySteamId[steamId] = hourly;
            }

            // Resolve all names in one batched Steam API call (globally cached for the last 100 ids).
            List<ulong> resolveIds = hourlyBySteamId.Keys
                .Select(s => ulong.TryParse(s, out ulong sid) ? sid : 0UL)
                .Where(sid => sid != 0)
                .ToList();

            Dictionary<ulong, string> names = await _steamService.GetPrettyNamesAsync(resolveIds);

            foreach (KeyValuePair<string, double[]> entry in hourlyBySteamId)
            {
                string prettyName = ulong.TryParse(entry.Key, out ulong sid) && names.TryGetValue(sid, out string? n)
                    ? n
                    : entry.Key;
                resolvedResult[prettyName] = entry.Value;
            }

            model.Result = resolvedResult;

            return View(model);
        }
    }

}
