using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

[ApiController]
[Route("TestSQL")]
public class TestSqlController : ControllerBase
{
    private readonly MySqlConnection _db;

    public TestSqlController(MySqlConnection db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<object> Get()
    {
        await _db.OpenAsync();

        MySqlCommand cmd = new MySqlCommand(
            "SELECT NOW() AS serverTime, DATABASE() AS dbName",
            _db);

        using MySqlDataReader reader = await cmd.ExecuteReaderAsync();

        if (!reader.Read())
            return new { ok = false };

        return new
        {
            ok = true,
            serverTime = reader["serverTime"],
            database = reader["dbName"]
        };
    }
}
