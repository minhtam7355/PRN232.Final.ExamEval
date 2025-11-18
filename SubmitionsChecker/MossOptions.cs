namespace SubmitionsChecker
{
    public class MossOptions
    {
        public string? Host { get; set; } = "moss.stanford.edu";
        public int Port { get; set; } = 769;
        public string? DefaultUserId { get; set; }
        public string? Language { get; set; } = "text";
        public int TimeoutMinutes { get; set; } = 5;
        public int? MaxMatches { get; set; } = 250;
    }
}
