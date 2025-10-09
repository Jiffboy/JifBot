namespace JifBot.Models
{
    public partial class StarCount
    {
        public ulong UserId { get; set; }
        public ulong ServerId { get; set; }
        public int Count { get; set; }
    }
}
