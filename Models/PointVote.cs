using System.Collections.Generic;

namespace JifBot.Models
{
    public partial class PointVote
    {
        public int Id { get; set; }
        public ulong UserId { get; set; }
        public int Points { get; set; }
        public List<ulong> YayVotes { get; set; }
        public List<ulong> NayVotes { get; set; }
    }
}