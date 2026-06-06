using Microsoft.AspNetCore.Mvc;

public class TouchgrassController : Controller
{
    [Route("touchgrass/privacy")]
    public IActionResult Privacy() => View();
}