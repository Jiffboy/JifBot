namespace JifBot.Models
{
    public partial class Timer
    {
        public ulong Id { get; set; }
        public ulong UserId { get; set; }
        public ulong ChannelId { get; set; }
        public string Message { get; set; }
        public long Timestamp { get; set; }
        public long Cadence { get; set; }
    }
}
