using Discord;
using System;
using JifBot.Models;
using System.Linq;

namespace JifBot.Embeds
{
    public class JifBotEmbedBuilder : EmbedBuilder
    {
        public JifBotEmbedBuilder()
        {
            var db = new BotBaseContext();
            var color = db.Variable.AsQueryable().Where(V => V.Name == "embedColor").FirstOrDefault();
            WithColor(new Color(Convert.ToUInt32(color.Value, 16)));
            WithFooter("Made with love");
            WithCurrentTimestamp();
        }
    }
}
