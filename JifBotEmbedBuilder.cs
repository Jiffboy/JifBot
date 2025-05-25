using Discord;
using System;
using JifBot.Models;
using System.Linq;

namespace JifBot
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

        public void PopulateAsTrial(CourtRecord record, IGuildUser user)
        {
            var db = new BotBaseContext();
            var config = db.ServerConfig.AsQueryable().Where(c => c.ServerId == user.Guild.Id).FirstOrDefault();
            var pollCount = 3;
            var pointName = "";
            if (config != null)
            {
                pollCount = config.TrialCount;
                pointName = config.PointName + " ";
            }

            string action = record.Points > 0 ? $"award {Math.Abs(record.Points)} {pointName}points to" : $"deduct {Math.Abs(record.Points)} {pointName}points from";

            if (record.Status == "Pending")
            {
                Title = $"A new trial has begun!";
            }
            else
            {
                var msg = "";
                if (record.Status == "Approved") {
                    msg = "APPROVED";
                } 
                else if (record.Status == "Denied")
                {
                    msg = "DENIED";
                }
                Title = $"Case closed! [{msg}]";
            }

            Description = $"The jury motions to {action} {user.Mention}\n\n**Justification:** {record.Justification}";
            ThumbnailUrl = user.GetDisplayAvatarUrl();

            var yays = record.YayVotes.Count > 0 ? "" : "[None]";
            var nays = record.NayVotes.Count > 0 ? "" : "[None]";

            foreach (var yay in record.YayVotes)
            {
                var target = db.User.AsQueryable().Where(user => user.UserId == yay).FirstOrDefault();
                yays += target.Name + "\n";
            }

            foreach (var nay in record.NayVotes)
            {
                var target = db.User.AsQueryable().Where(user => user.UserId == nay).FirstOrDefault();
                nays += target.Name + "\n";
            }

            AddField($"Yay ({record.YayVotes.Count}/{pollCount})", yays, inline: true);
            AddField($"Nay ({record.NayVotes.Count}/{pollCount})", nays, inline: true);

            if (record.ImageUrl != null)
            {
                WithImageUrl(record.ImageUrl);
            }
        }
    }
}
