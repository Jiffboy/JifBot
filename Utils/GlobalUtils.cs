using Discord;
using System;
using System.Globalization;

namespace JifBot.Utils
{
    public static class GlobalUtils
    {
        public static readonly string embedcolor = "0xe67c03";
        public static Color GetColor()
        {
            return new Color(Convert.ToUInt32(embedcolor, 16));
        }
        public static long GetTimestamp(string datetime)
        {
            DateTime dt;
            if (DateTime.TryParseExact(datetime, "MM/dd/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            {
                if (dt < DateTime.Now)
                {
                    return 0;
                }

                var dto = new DateTimeOffset(dt);
                return dto.ToUnixTimeSeconds();
            }
            else
            {
                return 0;
            }
        }
    }
}
