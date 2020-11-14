using System;
using System.Collections.Generic;

namespace JifBot.Models
{
    public partial class Command
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string Usage { get; set; }
        public string Description { get; set; }
    }
}
