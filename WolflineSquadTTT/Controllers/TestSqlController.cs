using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WolflineSquadTTT;
using WolflineSquadTTT.Models;
using WolflineSquadTTT.Services;

[ApiController]
[Route("TestSQL")]
public class TestSqlController : ControllerBase
{
    private readonly IUserRightService _userRightService;
    private readonly AppDbContext _db;

    public TestSqlController(AppDbContext db, IUserRightService userRightService)
    {
        _userRightService = userRightService;
        _db = db;
    }

    [HttpGet]
    public async Task<object> Get()
    {
        User? user = await _db.User
            .OrderBy(u => Guid.NewGuid())
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound();

        return Ok(user);
    }

    [Route("GetRights")]
    public async Task<object> GetRights()
    {
        string steamID = HttpContext.Session.GetString("SteamID") ?? "";
        if (steamID == null)
        {
            return RedirectToAction("Index", "Home");
        }

        List<UserRight> userRights = await _userRightService.GetUserRightsAsync(steamID);

        return Ok(userRights);
    }
}
