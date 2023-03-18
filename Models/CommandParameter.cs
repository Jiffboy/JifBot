using System;
using System.Collections.Generic;

namespace JifBot.Models
{
    public partial class CommandParameter
    {
        public string Command { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Required { get; set; }
    }
}
