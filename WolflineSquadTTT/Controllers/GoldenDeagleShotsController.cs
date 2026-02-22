using Microsoft.AspNetCore.Mvc;
using WolflineSquadTTT.Models;
using WolflineSquadTTT.Services;
namespace WolflineSquadTTT.Controllers
{

    [ApiController]
    [Route("api/GoldenDeagleShots")]
    public class RoundDataController : ControllerBase
    {
        private readonly DataWriterService _dataWriter;

        public RoundDataController(DataWriterService dataWriter)
        {
            _dataWriter = dataWriter;
        }

        [HttpPost]
        public IActionResult Post([FromBody] GoldenDeagleShots data)
        {
            if (data == null)
                return BadRequest("Invalid data");

            _dataWriter.AppendData(data);
            return Ok(new { status = "success" });
        }
    }
}
