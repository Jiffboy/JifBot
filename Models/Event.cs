namespace JifBot.Models
{
    public partial class Event
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string EventType { get; set; }
        public string EntrantType { get; set; }
        public int Limit { get; set; }
        public long Deadline { get; set; }
        public long EventTime { get; set; }
        public int EventDuration { get; set; }
        public string EventLocation { get; set; }
        public ulong UserId { get; set; }
        public ulong EmbedServerId { get; set; }
        public ulong EmbedChannelId { get; set; }
        public ulong EmbedMessageId { get; set; }
        public ulong ForumChannelId { get; set; }
        public string Status { get; set; }
        public byte[] Image { get; set; }
        public string ImageType { get; set; }
        public string ImageUrl { get; set; }
    }
}