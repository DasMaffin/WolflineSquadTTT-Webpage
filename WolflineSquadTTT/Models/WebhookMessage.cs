namespace WolflineSquadTTT.Models
{
    // A semantic notification an event produces. The webhook service renders this differently per
    // target application: plain text for Generic, a styled embed for Discord. Callers only describe
    // *what* happened — not how it should look on any given platform.
    public class WebhookMessage
    {
        public string Emoji { get; set; } = "🔔";
        public required string Headline { get; set; }     // event summary, e.g. "New market listing"
        public required string Title { get; set; }        // the subject, e.g. the item or poll name
        public string? Description { get; set; }
        public string? RelativeUrl { get; set; }          // app-relative path; resolved to an absolute link
        public int Color { get; set; } = 0x5865F2;        // Discord embed accent (default blurple)
        public List<WebhookField> Fields { get; set; } = new();
    }

    public class WebhookField
    {
        public required string Name { get; set; }
        public required string Value { get; set; }
        public bool Inline { get; set; } = true;
    }
}
