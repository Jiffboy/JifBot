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
        public async Task AnnounceLeftUser(SocketGuildUser user)
        {
            Console.WriteLine("User " + user.Username + " Left " + user.Guild.Name);

            var db = new BotBaseContext();
            var config = db.ServerConfig.Where(s => s.ServerId == user.Guild.Id).FirstOrDefault();

            if (config != null && config.LeaveId != 0)
            {
                IGuild server = user.Guild;
                ITextChannel channel = await server.GetTextChannelAsync(config.LeaveId);

                var embed = new EmbedBuilder();
                embed.WithColor(new Color(0x42ebf4));
                embed.ThumbnailUrl = user.GetAvatarUrl();
                embed.Title = $"**{user.Username} Left The Server:**";
                embed.Description = $"**User:**{user.Mention}";
                embed.WithCurrentTimestamp();
                await channel.SendMessageAsync("", false, embed);
            }
        }

        public async Task Audit(Cacheable<IMessage, ulong> cache, ISocketMessageChannel channel)
        {
            var message = await cache.GetOrDownloadAsync();
            var embed = new EmbedBuilder();
            SocketGuild server = bot.GetGuild(301918679262691330);
            if (server.GetChannel(channel.Id) == null)
            {
                return;
            }
            IUser user = bot.GetUser(186584509226024960);
            ISocketMessageChannel mod = server.GetTextChannel(457250924318949378);

            embed.WithColor(new Color(0x13e89d));
            embed.Title = "A message has been deleted";
            embed.Description = "\"" + message.Content + "\"";
            embed.WithCurrentTimestamp();
            embed.AddField("in " + channel.Name, "sent by: " + message.Author);
            embed.ThumbnailUrl = message.Author.GetAvatarUrl();
            await user.SendMessageAsync("", false, embed);
            await mod.SendMessageAsync("", false, embed);

        }
        public async Task AnnounceUserJoined(SocketGuildUser user)
        {
            Console.WriteLine("User " + user.Username + " Joined " + user.Guild.Name);

            var db = new BotBaseContext();
            var config = db.ServerConfig.Where(s => s.ServerId == user.Guild.Id).FirstOrDefault();

            if (config != null  && config.JoinId != 0)
            {
                IGuild server = user.Guild;
                ITextChannel channel = await server.GetTextChannelAsync(config.JoinId);

                var embed = new EmbedBuilder();
                embed.ThumbnailUrl = user.GetAvatarUrl();
                embed.WithColor(new Color(0x42ebf4));
                embed.Title = $"**{user.Username} Joined The Server:**";
                embed.Description = ($"**User:** {user.Mention}");
                embed.WithCurrentTimestamp();
                await channel.SendMessageAsync("", false, embed: embed);
            }

        }
    }
}
