using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
using System;
using JifBot.Models;
using JifBot.Embeds;
using System.Linq;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using JIfBot;

namespace JifBot
{
    public class EventHandler
    {
        public CommandService commands;
        private DiscordSocketClient bot;
        private IServiceProvider map;
        private ReactionHandler reactionHandler;

        public EventHandler(IServiceProvider service)
        {
            map = service;
            bot = map.GetService<DiscordSocketClient>();
            commands = map.GetService<CommandService>();
            reactionHandler = new ReactionHandler();
        }

        public async Task AnnounceUserJoined(SocketGuildUser user)
        {
            Console.WriteLine("User " + user.Username + " Joined " + user.Guild.Name);

            var db = new BotBaseContext();
            var config = db.ServerConfig.AsQueryable().Where(s => s.ServerId == user.Guild.Id).FirstOrDefault();

            if (config != null && config.JoinId != 0)
            {
                IGuild server = user.Guild;
                ITextChannel channel = await server.GetTextChannelAsync(config.JoinId);

                var embed = new JifBotEmbedBuilder();
                embed.ThumbnailUrl = user.GetAvatarUrl();
                embed.Title = $"**{user.Username} Joined The Server:**";
                embed.Description = ($"**User:** {user.Mention}");
                await channel.SendMessageAsync("", false, embed: embed.Build());
            }
        }

        public async Task AnnounceLeftUser(SocketGuild guild, SocketUser user)
        {
            Console.WriteLine("User " + user.Username + " Left " + guild.Name);

            var db = new BotBaseContext();
            var config = db.ServerConfig.AsQueryable().Where(s => s.ServerId == guild.Id).FirstOrDefault();

            if (config != null && config.LeaveId != 0)
            {
                IGuild server = guild;
                ITextChannel channel = await server.GetTextChannelAsync(config.LeaveId);

                var embed = new JifBotEmbedBuilder();
                embed.ThumbnailUrl = user.GetAvatarUrl();
                embed.Title = $"**{user.Username} Left The Server:**";
                embed.Description = $"**User:**{user.Mention}";
                await channel.SendMessageAsync("", false, embed.Build());
            }
        }

        public async Task SendMessageReport(Cacheable<IMessage, ulong> cache, Cacheable<IMessageChannel,ulong> channelcache)
        {
            var channel = await channelcache.GetOrDownloadAsync();
            IGuildChannel socketChannel = channel as IGuildChannel;
            
            var db = new BotBaseContext();
            var config = db.ServerConfig.AsQueryable().Where(s => s.ServerId == socketChannel.Guild.Id).FirstOrDefault();

            if (config != null && config.MessageId != 0)
            {
                IGuild server = bot.GetGuild(config.ServerId);
                ITextChannel sendChannel = await server.GetTextChannelAsync(config.MessageId);

                var message = await cache.GetOrDownloadAsync();
                var embed = new JifBotEmbedBuilder();
                embed.Title = "A message has been deleted";
                if (message != null)
                {
                    embed.Description = "\"" + message.Content + "\"";
                    embed.AddField("in " + channel.Name, "sent by: " + message.Author);
                    embed.ThumbnailUrl = message.Author.GetAvatarUrl();
                }
                else
                {
                    embed.AddField("in " + channel.Name, "message unknown");
                }
                await sendChannel.SendMessageAsync("", false, embed.Build());
            }
        }

        public async Task HandleReactionAdded(Cacheable<IUserMessage, ulong> cache, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            var db = new BotBaseContext();
            var serverConfig = db.ServerConfig.AsQueryable().Where(s => s.ReactMessageId == cache.Id).FirstOrDefault();
            var config = db.Configuration.AsQueryable().Where(cfg => cfg.Name == Program.configName).First();

            if (config != null)
            {
                var role = db.ReactRole.AsQueryable().Where(s => s.ServerId == serverConfig.ServerId && s.Emote == reaction.Emote.ToString()).FirstOrDefault();
                if (role != null)
                {
                    var server = bot.GetGuild(serverConfig.ServerId);
                    var serverRole = server.GetRole(role.RoleId);
                    var user = server.GetUser(reaction.UserId);
                    
                    if(serverRole != null && user.Id != config.Id)
                        await user.AddRoleAsync(serverRole);
                }
            }
        }

        public async Task HandleReactionRemoved(Cacheable<IUserMessage, ulong> cache, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            var db = new BotBaseContext();
            var config = db.ServerConfig.AsQueryable().Where(s => s.ReactMessageId == cache.Id).FirstOrDefault();
            if (config != null)
            {
                var role = db.ReactRole.AsQueryable().Where(s => s.ServerId == config.ServerId && s.Emote == reaction.Emote.ToString()).FirstOrDefault();
                if (role != null)
                {
                    var server = bot.GetGuild(config.ServerId);
                    var serverRole = server.GetRole(role.RoleId);
                    var user = server.GetUser(reaction.UserId);

                    if (serverRole != null)
                        await user.RemoveRoleAsync((IRole)serverRole);
                }
            }
        }

        public async Task HandleMessage(SocketMessage pMsg)
        {
            if (pMsg.Type == MessageType.Reply || pMsg.Type == MessageType.Default)
            {
                var message = pMsg as SocketUserMessage;
                var channel = message.Channel as SocketGuildChannel;

                // Check if reactions have been disabled for this server/channel
                BotBaseContext db = new BotBaseContext();
                var channelreact = db.ReactionBan.AsQueryable().AsQueryable().Where(c => c.ChannelId == message.Channel.Id).FirstOrDefault();
                var serverreact = db.ReactionBan.AsQueryable().AsQueryable().Where(c => c.ChannelId == channel.Guild.Id).FirstOrDefault();

                if (channelreact != null || serverreact != null)
                    return;

                //Don't handle if system message
                if (message == null)
                    return;

                if (message.Author.IsBot)
                    return;

                await reactionHandler.ParseReactions(message);
            }
        }
    }
}
