using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WolflineSquadTTT;

[ApiController]
[Route("TestSQL")]
public class TestSqlController : ControllerBase
{
    private readonly AppDbContext _db;

    public TestSqlController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<object> Get()
    {
        var user = await _db.User
            .OrderBy(u => Guid.NewGuid()) // random
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound();

        return Ok(user);
    }
}
