namespace WolflineSquadTTT.Models.Enums
{
    // Which receiving application a webhook targets — decides how the payload is formatted.
    public enum WebhookFormat
    {
        Generic = 0,   // plain, unstyled text body for arbitrary endpoints
        Discord = 1    // rich Discord embed using Discord-flavoured markdown
    }
}
