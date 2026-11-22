namespace JifBot.Models
{
    public partial class CheeseChaseTime
    {
        public string Version { get; set; }
        public ulong UserId { get; set; }
        public int Map {  get; set; }
        public int Character {  get; set; }
        public int Mode { get; set; }
        public float Time { get; set; }
        public string Seed { get; set; }
    }
}
