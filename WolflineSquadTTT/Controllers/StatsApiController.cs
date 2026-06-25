using Microsoft.AspNetCore.Mvc;
using WolflineSquadTTT.Infrastructure.Security;
using WolflineSquadTTT.Models;
using WolflineSquadTTT.Services;

namespace WolflineSquadTTT.Controllers
{
    // Consumed by the Garry's Mod server to upload the player-activity dataset shown on /Stats.
    [ApiController]
    [Route("api/Stats")]
    [RequiresApiPrivateKey]
    public class StatsApiController : ControllerBase
    {
        private readonly DataWriterService _dataWriter;
        public StatsApiController(DataWriterService dataWriter)
        {
            _dataWriter = dataWriter;
        }

        // Full replace: the body is the complete current dataset (SteamID64 -> rounds).
        [HttpPost]
        [RequestSizeLimit(104_857_600)] // 100 MB — round data can be large
        public IActionResult Post([FromBody] Dictionary<string, List<RoundEntry>> data)
        {
            if (data == null)
                return BadRequest("Invalid data");

            _dataWriter.WriteRoundData(data);
            return Ok(new { status = "success", players = data.Count });
        }
    }
}
