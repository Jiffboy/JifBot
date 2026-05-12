using Discord;
using JifBot.Utils;

namespace JifBot.Builders
{
    public class JifBotEmbedBuilder : EmbedBuilder
    {
        public JifBotEmbedBuilder()
        {
            WithColor(GlobalUtils.GetColor());
            WithFooter("Made with love");
            WithCurrentTimestamp();
        }
    }
}
