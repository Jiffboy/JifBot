using Discord;
using Discord.Rest;
using Discord.WebSocket;
using JifBot.Builders;
using JifBot.Models;
using JifBot.Utils;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            else if (pieces[0].Equals("event"))
            {
                var db = new BotBaseContext();
                var ev = db.Event.Where(e => e.Id == int.Parse(pieces[2])).First();
                var eventUIBuilder = new EventUIBuilder();

                if (pieces[1].Equals("edit"))
                {
                    var modal = eventUIBuilder.BuildModal(ev);
                    await component.RespondWithModalAsync(modal);
                }
                else if (pieces[1].Equals("eventedit"))
                {
                    var modal = eventUIBuilder.BuildEventModal(ev);
                    await component.RespondWithModalAsync(modal);
                }
                else if (pieces[1].Equals("addrole"))
                {
                    var modal = eventUIBuilder.BuildRoleModal(ev);
                    await component.RespondWithModalAsync(modal);
                }
                else if (pieces[1].Equals("next") || pieces[1].Equals("prev"))
                {
                    var status = NextStatus(ev, eventUIBuilder.stepCount, negative: pieces[1].Equals("prev"));
                    var errorMsg = pieces[1].Equals("next") ? VerifyEvent(ev, db) : "";
                    if (errorMsg != "")
                    {
                        await component.RespondAsync(errorMsg, ephemeral: true);
                        return;
                    }
                    ev.Status = status;
                    db.SaveChanges();
                    var newComp = eventUIBuilder.BuildComponent(ev);
                    await component.UpdateAsync(c => c.Components = newComp);
                }
                else if (pieces[1].Equals("cancel"))
                {
                    var roles = db.EventRole.Where(r => r.EventId == ev.Id).ToList();
                    foreach(var role in roles)
                    {
                        db.EventRole.Remove(role);
                    }
                    db.Event.Remove(ev);
                    await component.UpdateAsync(m =>  m.Components = new ComponentBuilderV2().WithTextDisplay("Event Cancelled.").Build());
                    db.SaveChanges();
                }
                else if (pieces[1].Equals("post"))
                {
                    var errorMsg = VerifyEvent(ev, db);
                    if (errorMsg != "")
                    {
                        await component.RespondAsync(errorMsg, ephemeral: true);
                        return;
                    }
                    
                    var img = new CommonImage(ev.Image, ev.ImageType);
                    var embed = eventUIBuilder.BuildEmbed(ev, component.User);
                    var buttons = eventUIBuilder.BuildEmbedComponent(ev);
                    RestUserMessage msg;
                    if (!img.isNull)
                    {
                        msg = await component.Message.Channel.SendFileAsync(img.GetMS(), img.imgName, embed: embed, components: buttons);
                    }
                    else
                    {
                        msg = await component.Message.Channel.SendMessageAsync(embed: embed, components: buttons);
                    }

                    await component.UpdateAsync(m => m.Components = new ComponentBuilderV2().WithTextDisplay("Event Posted.").Build());
                    ev.Status = "Posted";
                    ev.EmbedServerId = component.GuildId ?? 0;
                    ev.EmbedChannelId = msg.Channel.Id;
                    ev.EmbedMessageId = msg.Id;
                    db.SaveChanges();
                }
                else if (pieces[1].Equals("signup"))
                {
                    var user = component.User;
                    var participants = db.EventParticipant.Where(e => e.EventId == ev.Id).ToList();
                    var signedup =  participants.Where(e => e.UserId == user.Id).Count() > 0;
                    var roles = db.EventRole.Where(e => e.EventId == ev.Id).ToList();
                    var availRoles = roles.Where(e => e.Limit == 0 || participants.Where(p => p.RoleName == e.Name).Count() < e.Limit).ToList();

                    var availCharacters = new List<Character>();
                    if (ev.EntrantType != "user")
                    {
                        var characters = db.Character.Where(c => c.UserId == user.Id).ToList();
                        availCharacters = characters.Where(c => participants.Where(p => p.CharacterId == c.Id).Count() == 0).ToList();
                    }
                    
                    if (signedup && ev.EntrantType != "multi")
                    {
                        await component.RespondAsync("User already signed up for this event.", ephemeral: true);
                        return;
                    }

                    if (ev.EntrantType != "user" && availCharacters.Count == 0)
                    {
                        await component.RespondAsync("No available characters to sign up with! Please visit the [Blorbopedia](https://jifbot.com/blorbopedia/edit) to create your character.", ephemeral: true, flags: MessageFlags.SuppressEmbeds);
                        return;
                    }

                    var eventBuilder = new EventUIBuilder();
                    if (availRoles.Count() > 1 || (availCharacters.Count() >= 1 && ev.EntrantType != "user"))
                    {
                        var modal = eventBuilder.BuildSignupModal(ev, availRoles, availCharacters);
                        await component.RespondWithModalAsync(modal);
                        return;
                    }

                    var character = availCharacters.Count() != 0 && !ev.EntrantType.Equals("user") ? availCharacters.First().Id : 0;
                    var role = availRoles.Count() != 0 ? availRoles.First().Name : "";

                    db.Add(new EventParticipant {
                        EventId = ev.Id,
                        UserId = user.Id,
                        CharacterId = character,
                        RoleName = role,
                    });
                    db.SaveChanges();

                    if (participants.Count + 1 >= ev.Limit && ev.Limit != 0)
                    {
                        var eventResolver = new EventResolver(client);
                        await eventResolver.ResolveEvent(ev);
                    }
                    else
                    {
                        await component.Message.ModifyAsync(msg => msg.Embed = eventBuilder.BuildEmbed(ev, client.GetUser(ev.UserId)));
                    }
                    await component.DeferAsync();
                    return;
                }
                else if (pieces[1].Equals("leave"))
                {
                    var user = component.User;
                    var participants = db.EventParticipant.Where(e => e.EventId == ev.Id).ToList();
                    var entry = participants.Where(e => e.UserId == user.Id).FirstOrDefault();
                    if (entry != null)
                    {
                        db.EventParticipant.Remove(entry);
                        db.SaveChanges();
                        var eventBuilder = new EventUIBuilder();
                        await component.Message.ModifyAsync(msg => msg.Embed = eventBuilder.BuildEmbed(ev, client.GetUser(ev.UserId)));
                        await component.DeferAsync();
                        return;
                    }
                    await component.RespondAsync("Not signed up!", ephemeral: true);
                    await component.DeferAsync();
                    return;
                }
                else
                {
                    return;
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

        private string NextStatus(Event ev, int steps, bool negative=false)
        {
            var status = ev.Status;
            if (status.Contains("Setup"))
            {
                var step = int.Parse(status.Split('-')[1]);
                if (negative)
                {
                    status = $"Setup-{step - 1}";
                }
                else
                {
                    status = $"Setup-{step + 1}";
                }
            }
            return status;
        }
        private string VerifyEvent(Event ev, BotBaseContext db)
        {
            if (ev.Status == "Setup-1")
            {
                if (ev.Limit == 0 && ev.Deadline == 0)
                {
                    return "Must have either deadline or attendant limit";
                }
                if (ev.EventTime < ev.Deadline)
                {
                    ev.EventTime = ev.Deadline;
                }
            }
            else if (ev.Status == "Setup-2")
            {
                if (ev.EventType == "event")
                {
                    if (ev.EventTime == 0)
                    {
                        return "Must have an event start time.";
                    }
                    if (ev.EventDuration == 0)
                    {
                        return "Must have an event duration greater than 0";
                    }
                    if (ev.EventTime < ev.Deadline || ev.Deadline == 0)
                    {
                        ev.Deadline = ev.EventTime;
                    }
                }
                else if (ev.EventType == "thread")
                {
                    if (ev.ForumChannelId == 0)
                    {
                        return "Must select a forum channel.";
                    }
                }
            }
            else if (ev.Status == "Setup-3")
            {
                var roles = db.EventRole.Where(r => r.EventId == ev.Id).ToList();
                var unlimitedRoles = roles.Where(r => r.Limit == 0).ToList();
                if (unlimitedRoles.Count == 0 && roles.Count != 0)
                {
                    var total = roles.Sum(r => r.Limit);
                    if (total < ev.Limit)
                    {
                        return $"Role limits must meet or exceed total event limit ({ev.Limit}), or have an unlimited role.";
                    }
                }
            }

            db.SaveChanges();
            return "";
        }
    }
}
