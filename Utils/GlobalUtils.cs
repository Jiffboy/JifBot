using Discord;
using System;

namespace JifBot.Utils
{
    public static class GlobalUtils
    {
        public static readonly string embedcolor = "0xe67c03";
        public static Color GetColor()
        {
            return new Color(Convert.ToUInt32(embedcolor, 16));
        }
    }
}
