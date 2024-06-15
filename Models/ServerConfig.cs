namespace JifBot.Models
{
    public partial class ServerConfig
    {
        public ulong ServerId { get; set; }
        public ulong JoinId { get; set; }
        public ulong LeaveId { get; set; }
        public ulong MessageId { get; set; }
        public ulong ReactMessageId { get; set; }
        public ulong ReactChannelId { get; set; }
    }
}
