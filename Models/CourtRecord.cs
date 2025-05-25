using System.Collections.Generic;

namespace JifBot.Models
{
    public partial class CourtRecord
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public int Points { get; set; }
        public string Justification { get; set; }
        public ulong DefendantId { get; set; }
        public ulong ProsecutorId { get; set; }
        public List<ulong> YayVotes { get; set; }
        public List<ulong> NayVotes { get; set; }
        public ulong ServerId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public long Timestamp { get; set; }
        public string ImageUrl { get; set; }
    }
}