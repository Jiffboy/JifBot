using Discord;
using Discord.WebSocket;
using JifBot.Builders;
using JifBot.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace JifBot.Utils
{
    public class EventResolver
    {
        private DiscordSocketClient client;
        public EventResolver(DiscordSocketClient socketClient)
        {
            client = socketClient;
        }

        public async Task ResolveEvent(Event ev)
        {
            var db = new BotBaseContext();
            var participants = db.EventParticipant.Where(e => e.EventId == ev.Id).ToList();
            var roles = db.EventRole.Where(r => r.EventId == ev.Id).ToList();

            var server = client.GetGuild(ev.EmbedServerId);
            var channel = server.GetTextChannel(ev.EmbedChannelId);
            var message = await channel.GetMessageAsync(ev.EmbedMessageId) as IUserMessage;
            var user = client.GetUser(ev.UserId);
            var eventBuilder = new EventUIBuilder();

            var ids = participants.DistinctBy(e => e.UserId).Where(e => e.UserId != ev.UserId).Select(e => e.UserId).Prepend(ev.UserId).ToList();
            var pingStr = string.Join(", ", ids.ConvertAll(e => $"<@!{e}>"));
            var img = new CommonImage(ev.Image, ev.ImageType);
            var desc = $"{ev.Description}\n\n**Participants:**\n{eventBuilder.GetParticipantString(ev, participants)}";

            switch (ev.EventType)
            {
                case "event":
                    var start = DateTimeOffset.FromUnixTimeSeconds(ev.EventTime);
                    Discord.Image? discImg = !img.isNull ? new Discord.Image(img.GetMS()) : null;
                    var evt = await server.CreateEventAsync(
                        name: ev.Title,
                        description: desc,
                        type: GuildScheduledEventType.External,
                        startTime: start,
                        endTime: start.AddHours(ev.EventDuration),
                        location: ev.EventLocation,
                        coverImage: discImg
                    );
                    await message.ReplyAsync($"Event posted!\n{pingStr}");
                    break;

                case "thread":
                    var forum = server.GetForumChannel(ev.ForumChannelId);
                    if (ev.Image != null)
                    {
                        var attachment = new Discord.FileAttachment(img.GetMS(), img.imgName);
                        await forum.CreatePostWithFileAsync(ev.Title, attachment, text: desc, flags: MessageFlags.SuppressEmbeds);
                    }
                    else
                    {
                        await forum.CreatePostAsync(ev.Title, text: desc, flags: MessageFlags.SuppressEmbeds);
                    }
                    break;

                default:
                case "none":
                    await message.ReplyAsync($"Recruitment complete!\n{pingStr}");
                    break;
            }
            await message.ModifyAsync(msg => { msg.Components = null; msg.Embed = eventBuilder.BuildEmbed(ev, user); });

            // Context shenanigans don't @ me I know its bad
            var newEv = db.Event.Where(e => e.Id == ev.Id).First();
            newEv.Status = "Complete";
            db.SaveChanges();
        }
    }
}
