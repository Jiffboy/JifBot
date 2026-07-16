namespace JifBot.Models
{
    public partial class EventParticipant
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public ulong UserId { get; set; }
        public ulong CharacterId { get; set; }
        public string RoleName { get; set; }
    }
}
