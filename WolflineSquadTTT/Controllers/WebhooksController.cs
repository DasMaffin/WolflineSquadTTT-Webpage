using Microsoft.AspNetCore.Mvc;
using WolflineSquadTTT.Infrastructure.Security;
using WolflineSquadTTT.Models;
using WolflineSquadTTT.Models.Enums;
using WolflineSquadTTT.Services;

namespace WolflineSquadTTT.Controllers
{
    [Route("webhooks")]
    [RequiresPermission(Permission.AddWebhooks)]
    public class WebhooksController : Controller
    {
        private readonly IWebhookService _webhookService;

        public WebhooksController(IWebhookService webhookService)
        {
            _webhookService = webhookService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            return View(new WebhookIndexViewModel { Webhooks = await _webhookService.GetAllAsync() });
        }

        [HttpPost("add")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add([FromForm] string? name, [FromForm] string url, [FromForm] WebhookEvent eventType, [FromForm] string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out Uri? parsed)
                || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
            {
                TempData["Error"] = "Enter a valid http(s) webhook URL.";
                return RedirectToReturnUrl(returnUrl);
            }

            await _webhookService.AddAsync(name, url, eventType, HttpContext.Session.GetString("SteamID"));
            TempData["WebhookAdded"] = "Webhook added.";
            return RedirectToReturnUrl(returnUrl);
        }

        // Send the user back where they triggered the add from (the market / poll pages
        // render an inline modal), falling back to the webhooks page. Local URLs only.
        private IActionResult RedirectToReturnUrl(string? returnUrl) =>
            Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl!) : RedirectToAction("Index");

        [HttpPost("delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] int id)
        {
            await _webhookService.DeleteAsync(id);
            return RedirectToAction("Index");
        }
    }
}
