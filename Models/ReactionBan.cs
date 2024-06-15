namespace JifBot.Models
{
    public partial class ReactionBan
    {
        public ulong ChannelId { get; set; }
        public ulong ServerId { get; set; }
        public string ChannelName { get; set; }
    }
}
