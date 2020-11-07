using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
using System;
using JifBot.Models;
using System.Linq;

namespace JifBot
{
    public class EventHandler
    {
        private DiscordSocketClient bot;

        public EventHandler(DiscordSocketClient client)
        {
            bot = client;
        }

        public async Task AnnounceUserJoined(SocketGuildUser user)
        {
            Console.WriteLine("User " + user.Username + " Joined " + user.Guild.Name);

            var db = new BotBaseContext();
            var config = db.ServerConfig.AsQueryable().AsQueryable().Where(s => s.ServerId == user.Guild.Id).FirstOrDefault();

            if (config != null && config.JoinId != 0)
            {
                IGuild server = user.Guild;
                ITextChannel channel = await server.GetTextChannelAsync(config.JoinId);

                var embed = new EmbedBuilder();
                var color = db.Variable.AsQueryable().AsQueryable().Where(V => V.Name == "embedColor").FirstOrDefault();
                embed.WithColor(new Color(Convert.ToUInt32(color.Value, 16)));
                embed.ThumbnailUrl = user.GetAvatarUrl();
                embed.Title = $"**{user.Username} Joined The Server:**";
                embed.Description = ($"**User:** {user.Mention}");
                embed.WithCurrentTimestamp();
                await channel.SendMessageAsync("", false, embed: embed.Build());
            }
        }

        public async Task AnnounceLeftUser(SocketGuildUser user)
        {
            Console.WriteLine("User " + user.Username + " Left " + user.Guild.Name);

            var db = new BotBaseContext();
            var config = db.ServerConfig.AsQueryable().AsQueryable().Where(s => s.ServerId == user.Guild.Id).FirstOrDefault();

            if (config != null && config.LeaveId != 0)
            {
                IGuild server = user.Guild;
                ITextChannel channel = await server.GetTextChannelAsync(config.LeaveId);

                var embed = new EmbedBuilder();
                var color = db.Variable.AsQueryable().AsQueryable().Where(V => V.Name == "embedColor").FirstOrDefault();
                embed.WithColor(new Color(Convert.ToUInt32(color.Value, 16)));
                embed.ThumbnailUrl = user.GetAvatarUrl();
                embed.Title = $"**{user.Username} Left The Server:**";
                embed.Description = $"**User:**{user.Mention}";
                embed.WithCurrentTimestamp();
                await channel.SendMessageAsync("", false, embed.Build());
            }
        }

        public async Task SendMessageReport(Cacheable<IMessage, ulong> cache, ISocketMessageChannel channel)
        {
            SocketGuildChannel socketChannel = (SocketGuildChannel)channel;
            var db = new BotBaseContext();
            var config = db.ServerConfig.AsQueryable().AsQueryable().Where(s => s.ServerId == socketChannel.Guild.Id).FirstOrDefault();

            if (config != null && config.MessageId != 0)
            {
                IGuild server = bot.GetGuild(config.ServerId);
                ITextChannel sendChannel = await server.GetTextChannelAsync(config.MessageId);

                var message = await cache.GetOrDownloadAsync();
                var embed = new EmbedBuilder();
                var color = db.Variable.AsQueryable().AsQueryable().Where(V => V.Name == "embedColor").FirstOrDefault();
                embed.WithColor(new Color(Convert.ToUInt32(color.Value, 16)));
                embed.Title = "A message has been deleted";
                embed.Description = "\"" + message.Content + "\"";
                embed.WithCurrentTimestamp();
                embed.AddField("in " + channel.Name, "sent by: " + message.Author);
                embed.ThumbnailUrl = message.Author.GetAvatarUrl();
                await sendChannel.SendMessageAsync("", false, embed.Build());
            }
        }
    }
}
