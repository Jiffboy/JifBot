namespace JifBot.Models
{
    public partial class Character
    {
        public string Key { get; set; }
        public ulong UserId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Title { get; set; }
        public string Occupation { get; set; }
        public string Age { get; set; }
        public string Race { get; set; }
        public string Pronouns { get; set; }
        public string Sexuality { get; set; }
        public string Origin { get; set; }
        public string Residence { get; set; }
        public string Universe { get; set; }
        public string Resources { get; set; }
        public bool CompactImage { get; set; }
        public string ImageUrl { get; set; }
    }
}
