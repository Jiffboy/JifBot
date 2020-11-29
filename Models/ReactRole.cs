using System;
using System.Collections.Generic;

namespace JifBot.Models
{
    public partial class ReactRole
    {
        public ulong RoleId { get; set; }
        public string Emote { get; set; }
        public ulong ServerId { get; set; }
    }
}
