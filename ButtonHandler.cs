using System.Threading.Tasks;
using Discord.WebSocket;
using System.Linq;
using JifBot.Models;
using Discord;

namespace JifBot
{
    public class ButtonHandler
    {
        public async Task HandleButton(SocketMessageComponent component)
        {
            string[] pieces = component.Data.CustomId.Split("-");
            bool isYay = pieces[0].Equals("yay");
            bool pollClosed = false;
            int id = int.Parse(pieces[1]);

            var db = new BotBaseContext();
            var poll = db.PointVote.AsQueryable().Where(p => p.Id == id).FirstOrDefault();
            var pollCount = int.Parse(db.Variable.AsQueryable().Where(v => v.Name == "pollCount").FirstOrDefault().Value);

            var user = db.User.AsQueryable().AsQueryable().Where(user => user.UserId == component.User.Id).FirstOrDefault();
            if (user == null)
                db.Add(new User { UserId = component.User.Id, Name = component.User.Username, Number = long.Parse(component.User.Discriminator) });

            if (poll == null)
            {
                await component.DeferAsync();
                return;
            }

            if (poll.UserId == component.User.Id)
            {
                await component.RespondAsync("Cannot vote for yourself! Have some integrity!", ephemeral: true);
                return;
            }

            if (isYay)
            {
                if (poll.NayVotes.Contains(component.User.Id))
                {
                    poll.NayVotes.Remove(component.User.Id);
                    db.Entry(poll).Property(p => p.NayVotes).IsModified = true;
                }

                if (!poll.YayVotes.Contains(component.User.Id))
                {
                    poll.YayVotes.Add(component.User.Id);
                    db.Entry(poll).Property(p => p.YayVotes).IsModified = true;
                }

                if (poll.YayVotes.Count >= pollCount)
                {
                    var target = db.User.AsQueryable().AsQueryable().Where(user => user.UserId == poll.UserId).FirstOrDefault();
                    target.RpPoints += poll.Points;
                    db.PointVote.Remove(poll);
                    pollClosed = true;
                }
            }
            else
            {
                if (poll.YayVotes.Contains(component.User.Id))
                {
                    poll.YayVotes.Remove(component.User.Id);
                    db.Entry(poll).Property(p => p.YayVotes).IsModified = true;
                }

                if (!poll.NayVotes.Contains(component.User.Id))
                {
                    poll.NayVotes.Add(component.User.Id);
                    db.Entry(poll).Property(p => p.NayVotes).IsModified = true;
                }

                if (poll.NayVotes.Count >= pollCount)
                {
                    db.PointVote.Remove(poll);
                    pollClosed = true;
                }
            }
            db.SaveChanges();

            var message = component.Message;
            var embed = message.Embeds.First();
            var builder = new JifBotEmbedBuilder
            {
                Title = embed.Title,
                Description = embed.Description,
                ThumbnailUrl = embed.Thumbnail?.Url
            };

            var yays = poll.YayVotes.Count > 0 ? "" : "[None]";
            var nays = poll.NayVotes.Count > 0 ? "" : "[None]";

            foreach (var yay in poll.YayVotes)
            {
                var target = db.User.AsQueryable().Where(user => user.UserId == yay).FirstOrDefault();
                yays += target.Name + "\n";
            }

            foreach(var nay in poll.NayVotes)
            {
                var target = db.User.AsQueryable().Where(user => user.UserId == nay).FirstOrDefault();
                nays += target.Name + "\n";
            }

            builder.AddField($"Yay ({poll.YayVotes.Count}/{pollCount})", yays, inline: true);
            builder.AddField($"Nay ({poll.NayVotes.Count}/{pollCount})", nays, inline: true);

            if (pollClosed)
            {
                var msg = isYay ? "APPROVED" : "DENIED";
                builder.Title = $"Case closed! [{msg}]";
                await message.ModifyAsync(msg => {
                    msg.Embed = builder.Build();
                    msg.Components = new ComponentBuilder().Build();
                });
                await component.RespondAsync($"Verdict reached: {msg}");
            }
            else
            {
                await message.ModifyAsync(msg => msg.Embed = builder.Build());
                await component.DeferAsync();
            }
        }
    }
}
