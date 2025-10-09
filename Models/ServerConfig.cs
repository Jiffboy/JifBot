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
        public string PointName { get; set; }
        public int TrialCount { get; set; }
        public ulong QotdForumId { get; set; }
        public ulong QotdThreadId { get; set; }
        public ulong QotdRoleId { get; set; }
        public ulong StarMessageId { get; set; }
        public ulong StarChannelId { get; set; }
        public bool StarAdminRequired { get; set; }
    }
}
