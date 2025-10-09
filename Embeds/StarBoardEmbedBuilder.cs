using Discord;
using JifBot.Models;
using System.Linq;

namespace JifBot.Embeds
{
    internal class StarBoardEmbedBuilder : JifBotEmbedBuilder
    {
        public void Populate(IGuild server)
        {
            var db = new BotBaseContext();
            var stars = db.StarCount.Where(s => s.ServerId == server.Id).OrderByDescending(s => s.Count).ToList();
            var count = 1;

            if (stars.Count > 0)
            {
                Title = $"Total star counts for {server.Name}";
                Description = "As totaled by the number of star reactions given to the user";
                ThumbnailUrl = "https://cdn.discordapp.com/attachments/782655615557697536/1425974413244891166/your-did-it-star.png?ex=68e989a9&is=68e83829&hm=56315f2bd98fd5e4be399a751e3389a1430420c1ed8db3a5146c81da18fce12b";

                foreach (var star in stars)
                {
                    if (count > 10)
                        break;

                    var user = db.User.Where(u => u.UserId == star.UserId).FirstOrDefault();
                    string number = $"{count}.";
                    string starPlural = star.Count > 1 ? "stars" : "star";

                    if (count == 1)
                        number = "🥇";
                    else if (count == 2)
                        number = "🥈";
                    else if (count == 3)
                        number = "🥉";

                    AddField($"{number} {user.Name}", $"> {star.Count} {starPlural}", inline: true);
                    count++;
                }
            }
            else
            {
                Title = "No stars have been awarded yet!";
                Description = "Get to it!";
            }
        }
    }
}