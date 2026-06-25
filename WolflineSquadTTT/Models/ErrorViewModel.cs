namespace WolflineSquadTTT.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        // The real exception, surfaced on the error page.
        public string? Message { get; set; }
        public string? ExceptionType { get; set; }
        public string? Path { get; set; }

        // Full stack trace — only populated in Development (not leaked to end users in production).
        public string? StackTrace { get; set; }
    }
}
