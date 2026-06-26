using Microsoft.AspNetCore.Mvc;
using WolflineSquadTTT.Infrastructure.Security;
using WolflineSquadTTT.Models;
using WolflineSquadTTT.Models.Enums;
using WolflineSquadTTT.Services;

namespace WolflineSquadTTT.Controllers
{
    // Opening the page needs any one of the webhook permissions; each action below is gated to its own.
    [Route("webhooks")]
    [RequiresPermission(new[] { Permission.AddWebhooks, Permission.EditWebhooks, Permission.DeleteWebhooks })]
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
            return View(new WebhookIndexViewModel
            {
                Webhooks = await _webhookService.GetAllAsync(),
                CanAdd = Has(Permission.AddWebhooks),
                CanEdit = Has(Permission.EditWebhooks),
                CanDelete = Has(Permission.DeleteWebhooks)
            });
        }

        [HttpPost("add")]
        [RequiresPermission(Permission.AddWebhooks)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add([FromForm] string? name, [FromForm] string url, [FromForm] WebhookEvent eventType, [FromForm] WebhookFormat format, [FromForm] string? returnUrl)
        {
            if (!IsValidUrl(url))
            {
                TempData["Error"] = "Enter a valid http(s) webhook URL.";
                return RedirectToReturnUrl(returnUrl);
            }

            await _webhookService.AddAsync(name, url, eventType, format, HttpContext.Session.GetString("SteamID"));
            TempData["WebhookAdded"] = "Webhook added.";
            return RedirectToReturnUrl(returnUrl);
        }

        [HttpPost("edit")]
        [RequiresPermission(Permission.EditWebhooks)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm] int id, [FromForm] string? name, [FromForm] string url, [FromForm] WebhookEvent eventType, [FromForm] WebhookFormat format)
        {
            if (!IsValidUrl(url))
            {
                TempData["Error"] = "Enter a valid http(s) webhook URL.";
                return RedirectToAction("Index");
            }

            await _webhookService.UpdateAsync(id, name, url, eventType, format);
            TempData["WebhookAdded"] = "Webhook updated.";
            return RedirectToAction("Index");
        }

        [HttpPost("delete")]
        [RequiresPermission(Permission.DeleteWebhooks)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] int id)
        {
            await _webhookService.DeleteAsync(id);
            TempData["WebhookAdded"] = "Webhook deleted.";
            return RedirectToAction("Index");
        }

        // Send the user back where they triggered the add from (the market / poll pages
        // render an inline modal), falling back to the webhooks page. Local URLs only.
        private IActionResult RedirectToReturnUrl(string? returnUrl) =>
            Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl!) : RedirectToAction("Index");

        private static bool IsValidUrl(string url) =>
            !string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out Uri? parsed)
                && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps);

        private bool Has(Permission permission) =>
            PermissionHelper.HasPermission(HttpContext.Session, permission);
    }
}
