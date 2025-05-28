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
            var record = db.CourtRecord.AsQueryable().Where(p => p.Id == id).FirstOrDefault();
            var server = (component.Channel as SocketGuildChannel)?.Guild.Id;
            var config = db.ServerConfig.AsQueryable().Where(c => c.ServerId == server).FirstOrDefault();
            var pollCount = 3;
            if (config != null)
                pollCount = config.TrialCount;

            var target = db.User.AsQueryable().AsQueryable().Where(user => user.UserId == record.DefendantId).FirstOrDefault();
            var user = db.User.AsQueryable().AsQueryable().Where(user => user.UserId == component.User.Id).FirstOrDefault();
            if (user == null)
                db.Add(new User { UserId = component.User.Id, Name = component.User.Username, Number = long.Parse(component.User.Discriminator) });

            if (record == null)
            {
                await component.DeferAsync();
                return;
            }

            if (record.DefendantId == component.User.Id)
            {
                await component.RespondAsync("Cannot vote for yourself! Have some integrity!", ephemeral: true);
                return;
            }

            if (record.ProsecutorId == component.User.Id)
            {
                await component.RespondAsync("Cannot vote in your own trial! Have some integrity!", ephemeral: true);
                return;
            }

            if (isYay)
            {
                if (record.NayVotes.Contains(component.User.Id))
                {
                    record.NayVotes.Remove(component.User.Id);
                    db.Entry(record).Property(p => p.NayVotes).IsModified = true;
                }

                if (!record.YayVotes.Contains(component.User.Id))
                {
                    record.YayVotes.Add(component.User.Id);
                    db.Entry(record).Property(p => p.YayVotes).IsModified = true;
                }

                if (record.YayVotes.Count >= pollCount)
                {
                    pollClosed = true;
                    record.Status = "Approved";
                }
            }
            else
            {
                if (record.YayVotes.Contains(component.User.Id))
                {
                    record.YayVotes.Remove(component.User.Id);
                    db.Entry(record).Property(p => p.YayVotes).IsModified = true;
                }

                if (!record.NayVotes.Contains(component.User.Id))
                {
                    record.NayVotes.Add(component.User.Id);
                    db.Entry(record).Property(p => p.NayVotes).IsModified = true;
                }

                if (record.NayVotes.Count >= pollCount)
                {
                    pollClosed = true;
                    record.Status = "Denied";
                }
            }
            db.SaveChanges();

            var message = component.Message;
            var embed = new JifBotEmbedBuilder();
            var defendant = (component.Channel as SocketGuildChannel)?.Guild.GetUser(record.DefendantId);
            embed.PopulateAsTrial(record, defendant);


            if (pollClosed)
            {
                await message.ModifyAsync(msg => {
                    msg.Embed = embed.Build();
                    msg.Components = new ComponentBuilder().Build();
                });

                var msg = "";
                if (isYay)
                {
                    msg = "APPROVED";
                }
                else
                {
                    msg = "DENIED";
                }

                await component.RespondAsync($"Verdict reached: {msg}");
            }
            else
            {
                await message.ModifyAsync(msg => msg.Embed = embed.Build());
                await component.DeferAsync();
            }
        }
    }
}
