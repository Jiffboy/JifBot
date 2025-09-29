using System.Threading.Tasks;
using Discord.WebSocket;
using System.Linq;
using JifBot.Models;
using JifBot.Embeds;
using Discord;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace JifBot
{
    public class ButtonHandler
    {
        private DiscordSocketClient client;

        public ButtonHandler(IServiceProvider service)
        {
            client = service.GetService<DiscordSocketClient>();
        }

        public async Task HandleButton(SocketMessageComponent component)
        {
            string[] pieces = component.Data.CustomId.Split("-");

            if (pieces[0].Equals("yay") || pieces[0].Equals("nay"))
            {
                await HandleVote(component, pieces[0].Equals("yay"), int.Parse(pieces[1]));
            } 
            else if (pieces[0].Equals("qotd"))
            {
                if (pieces[1].Equals("submit"))
                {
                    await HandleQotdSubmit(component);
                }
                else if (pieces[1].Equals("role"))
                {
                    await HandleQotdRole(component, pieces[2].Equals("add"));
                }
            }
        }
        private async Task HandleVote(SocketMessageComponent component, bool isYay, int id)
        {
            bool pollClosed = false;

            var db = new BotBaseContext();
            var record = db.CourtRecord.AsQueryable().Where(p => p.Id == id).FirstOrDefault();
            var server = (component.Channel as SocketGuildChannel)?.Guild.Id;
            var config = db.ServerConfig.AsQueryable().Where(c => c.ServerId == server).FirstOrDefault();
            var pollCount = 3;
            if (config != null)
                pollCount = config.TrialCount;

            var target = db.User.AsQueryable().AsQueryable().Where(user => user.UserId == record.DefendantId).FirstOrDefault();
            var user = db.GetUser(component.User);

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
            var embed = new TrialEmbedBuilder();
            var defendant = (component.Channel as SocketGuildChannel)?.Guild.GetUser(record.DefendantId);
            embed.Populate(record, defendant);


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

        private async Task HandleQotdSubmit(SocketMessageComponent component)
        {
            var mb = new ModalBuilder()
                .WithTitle("Submit a QOTD!")
                .WithCustomId("qotd-submit")
                .AddTextInput("Question", "question", TextInputStyle.Paragraph)
                .AddTextInput("Image Link", "image", TextInputStyle.Short, placeholder: "Has to be a link cause Discord sucks. Sorry :(", required: false);
            await component.RespondWithModalAsync(mb.Build());
        }

        // If add is false, we remove
        private async Task HandleQotdRole(SocketMessageComponent component, bool add)
        {
            var db = new BotBaseContext();
            var guild = client.GetGuild(component.GuildId.Value);
            var config = db.GetServerConfig(guild);
            var user = component.User as SocketGuildUser;
            var role = guild.GetRole(config.QotdRoleId);

            if (add)
            {
                await user.AddRoleAsync(role);
                await component.RespondAsync("Subscribed!", ephemeral: true);
            }
            else
            {
                await user.RemoveRoleAsync(role);
                await component.RespondAsync("Unsubscribed!", ephemeral: true);
            }
        }
    }
}
