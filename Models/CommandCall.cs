namespace JifBot.Models
{
    public partial class CommandCall
    {
        public string Command { get; set; }
        public long Timestamp { get; set; }
        public ulong ServerId { get; set; }
        public ulong UserId { get; set; }
    }
}
