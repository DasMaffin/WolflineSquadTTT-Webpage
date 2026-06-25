using WolflineSquadTTT.Models.Enums;

namespace WolflineSquadTTT.Models
{
    public class WebhookIndexViewModel
    {
        public List<Webhook> Webhooks { get; set; } = new();

        // Friendly label for an event, shown in the UI.
        public static string EventLabel(WebhookEvent evt) => evt switch
        {
            WebhookEvent.MarketListingCreated => "New market listing",
            WebhookEvent.PollCreated => "New poll",
            _ => evt.ToString()
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
