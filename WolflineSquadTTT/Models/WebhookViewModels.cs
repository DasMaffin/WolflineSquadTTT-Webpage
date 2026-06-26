using WolflineSquadTTT.Models.Enums;

namespace WolflineSquadTTT.Models
{
    public class WebhookIndexViewModel
    {
        public List<Webhook> Webhooks { get; set; } = new();
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }

        // Friendly label for an event, shown in the UI.
        public static string EventLabel(WebhookEvent evt) => evt switch
        {
            WebhookEvent.MarketListingCreated => "New market listing",
            WebhookEvent.PollCreated => "New poll",
            _ => evt.ToString()
        };

        // Friendly label for a target application / payload style.
        public static string FormatLabel(WebhookFormat format) => format switch
        {
            WebhookFormat.Generic => "Generic (raw text)",
            WebhookFormat.Discord => "Discord (rich embed)",
            _ => format.ToString()
        };
    }

    // Drives the "_AddWebhookModal" partial: a button + modal that adds a webhook
    // pre-scoped to one event, rendered inline on the page that owns that event.
    public class AddWebhookModalViewModel
    {
        public WebhookEvent Event { get; set; }
        public string ReturnUrl { get; set; } = "/";
        public string ButtonClass { get; set; } = "btn btn-outline-light btn-sm";
        public string ButtonText { get; set; } = "Add webhook";
    }
}
