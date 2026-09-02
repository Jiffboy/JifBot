using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JifBot.Builders;
using JifBot.Models;
using JIfBot;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

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
            var server = ((SocketGuildChannel)reaction.Channel).Guild;
            var serverConfig = db.GetServerConfig(server);
            var user = server.GetUser(reaction.UserId);
            var config = db.Configuration.AsQueryable().Where(cfg => cfg.Name == Program.configName).First();

            // This is Jif Bot
            if (config == null || user.Id == config.Id)
            {
                return;
            }

            // Self assigned roles
            if (serverConfig.ReactMessageId == cache.Id)
            {
                var role = db.ReactRole.AsQueryable().Where(s => s.ServerId == serverConfig.ServerId && s.Emote == reaction.Emote.ToString()).FirstOrDefault();
                if (role != null)
                {
                    var serverRole = server.GetRole(role.RoleId);

                    if(serverRole != null)
                        await user.AddRoleAsync(serverRole);
                }
            }

            // Star Board
            if (reaction.Emote.ToString() == "⭐")
            {
                var author = (await reaction.Channel.GetMessageAsync(reaction.MessageId)).Author;
                if (StarValid(serverConfig, author, user))
                {
                    var jifBotUser = db.GetUser(author as SocketUser);
                    var starCount = db.StarCount.Where(s => s.UserId == jifBotUser.UserId && s.ServerId == server.Id).FirstOrDefault();

                    if (starCount == null)
                    {
                        starCount = new StarCount { UserId = jifBotUser.UserId, ServerId = server.Id, Count = 0 };
                        db.Add(starCount);
                    }

                    starCount.Count++;
                    db.SaveChanges();
                    var msg = await server.GetTextChannel(serverConfig.StarChannelId).GetMessageAsync(serverConfig.StarMessageId) as IUserMessage;
                    if (msg != null)
                    {
                        var embed = new StarBoardEmbedBuilder();
                        embed.Populate(server);
                        await msg.ModifyAsync(msg => msg.Embed = embed.Build());
                    }
                }
            }
        }

        public async Task HandleReactionRemoved(Cacheable<IUserMessage, ulong> cache, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            var db = new BotBaseContext();
            var server = ((SocketGuildChannel)reaction.Channel).Guild;
            var serverConfig = db.GetServerConfig(server);
            var user = server.GetUser(reaction.UserId);
            var config = db.Configuration.AsQueryable().Where(cfg => cfg.Name == Program.configName).First();

            // This is Jif Bot
            if (config == null || user.Id == config.Id)
            {
                return;
            }

            // Self assigned roles
            if (serverConfig.ReactMessageId == cache.Id)
            {
                var role = db.ReactRole.AsQueryable().Where(s => s.ServerId == serverConfig.ServerId && s.Emote == reaction.Emote.ToString()).FirstOrDefault();
                if (role != null)
                {
                    var serverRole = server.GetRole(role.RoleId);

                    if (serverRole != null)
                        await user.RemoveRoleAsync(serverRole);
                }
            }

            // Star Board
            if (reaction.Emote.ToString() == "⭐")
            {
                var author = (await reaction.Channel.GetMessageAsync(reaction.MessageId)).Author;
                    
                if (StarValid(serverConfig, author, user))
                {
                    var starCount = db.StarCount.Where(s => s.UserId == author.Id && s.ServerId == server.Id).FirstOrDefault();
                    if (starCount == null)
                        return;

                    starCount.Count--;
                    if (starCount.Count == 0)
                        db.Remove(starCount);

                    db.SaveChanges();

                    var msg = await server.GetTextChannel(serverConfig.StarChannelId).GetMessageAsync(serverConfig.StarMessageId) as IUserMessage;
                    if (msg != null)
                    {
                        var embed = new StarBoardEmbedBuilder();
                        embed.Populate(server);
                        await msg.ModifyAsync(msg => msg.Embed = embed.Build());
                    }
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

        private bool StarValid(ServerConfig config, IUser author, SocketGuildUser user)
        {
            if (config.StarMessageId != 0 && config.StarChannelId != 0)
            {
                switch (config.StarPermissions)
                {
                    case "owner":
                        if (user.Guild.OwnerId == user.Id)
                            return true;
                        break;
                    case "admin":
                        if (user.GuildPermissions.Administrator)
                            if (author.Id != user.Id)
                                return true;
                        break;
                    case "everyone":
                    default:
                        if (author.Id != user.Id)
                            return true;
                        break;
                }
            }
            return false;
        }
    }
}
