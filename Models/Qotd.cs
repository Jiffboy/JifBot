namespace JifBot.Models
{
    public partial class Qotd
    {
        public ulong Id { get; set; }
        public ulong ServerId { get; set; }
        public string Question { get; set; }
        public ulong UserId { get; set; }
        public long AskTimestamp { get; set; }
        public byte[] Image { get; set; }
        public string ImageType { get; set; }
    }
}
