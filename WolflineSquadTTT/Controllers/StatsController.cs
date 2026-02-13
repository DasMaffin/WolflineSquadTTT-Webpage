using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WolflineSquadTTT.Infrastructure.Security;
using WolflineSquadTTT.Models;
using WolflineSquadTTT.Models.Enums;
using WolflineSquadTTT.Services;

namespace WolflineSquadTTT.Controllers
{
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
        [RequiresPermission(Permission.ViewPlayerActivity)]
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

                    string prettyName = await _steamService.GetPrettyNameAsync(ulong.Parse(steamId));
                    resolvedResult[prettyName] = hourly;
                }
                model.Result = resolvedResult;
            }

            return View(model);
        }
    }

}
