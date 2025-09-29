using JifBot.Models;
using System;
using System.Linq;

namespace JifBot.Embeds
{
    public class QotdEmbedBuilder : JifBotEmbedBuilder
    {
        public void Populate(ulong guildId)
        {
            var db = new BotBaseContext();

            var pool = db.Qotd.AsQueryable().Where(q => q.ServerId == guildId && q.AskTimestamp == 0).ToList();
            var postCount = db.Qotd.AsQueryable().Where(q => q.ServerId == guildId && q.AskTimestamp != 0).Count();

            Title = "Submit your own QOTD!";
            Description = "Questions will go into a pool, and one will be randomly selected every morning to be the QOTD. To submit a question, use /submitqotd below.\n\nDo your part to make sure the pool doesn't run dry!";

            var questions = "[empty]";
            if (pool.Count > 0)
            {
                questions = "";
                foreach (var question in pool)
                {
                    var line = $"- ||{question.Question.Replace("\n", " ")}||\n";
                    if (line.Length + questions.Length < 1024)
                    {
                        questions += line;
                    }
                }
            }

            AddField("Questions Posted", postCount, inline: true);
            AddField("Pool Size", pool.Count(), inline: true);
            AddField("Current Pool", questions);
            ThumbnailUrl = "https://cdn.discordapp.com/emojis/571859749860278293.png";
        }
    }
}
