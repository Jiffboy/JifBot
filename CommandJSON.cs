using System.ComponentModel;

namespace JifBot
{
    class CommandJSON
    {
        public CommandJSON(string commandName, string aliasName, string categoryName, string descriptionName)
        {
            command = commandName;
            alias = aliasName;
            category = categoryName;
            description = descriptionName;
        }

        [DefaultValue("")] 
        public string command { get; set; }

        [DefaultValue("")]
        public string alias { get; set; }

        [DefaultValue("")]
        public string category { get; set; }

        [DefaultValue("")]
        public string description { get; set; }
    }
}
