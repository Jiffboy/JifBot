using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
using System;
using JifBot.Models;
using System.Linq;
using Discord.Commands;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
        private static string configName = Program.configName;

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

                var embed = new EmbedBuilder();
                var color = db.Variable.AsQueryable().Where(V => V.Name == "embedColor").FirstOrDefault();
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
            var config = db.ServerConfig.AsQueryable().Where(s => s.ServerId == user.Guild.Id).FirstOrDefault();

            if (config != null && config.LeaveId != 0)
            {
                IGuild server = user.Guild;
                ITextChannel channel = await server.GetTextChannelAsync(config.LeaveId);

                var embed = new EmbedBuilder();
                var color = db.Variable.AsQueryable().Where(V => V.Name == "embedColor").FirstOrDefault();
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
            var config = db.ServerConfig.AsQueryable().Where(s => s.ServerId == socketChannel.Guild.Id).FirstOrDefault();

            if (config != null && config.MessageId != 0)
            {
                IGuild server = bot.GetGuild(config.ServerId);
                ITextChannel sendChannel = await server.GetTextChannelAsync(config.MessageId);

                var message = await cache.GetOrDownloadAsync();
                var embed = new EmbedBuilder();
                var color = db.Variable.AsQueryable().Where(V => V.Name == "embedColor").FirstOrDefault();
                embed.WithColor(new Color(Convert.ToUInt32(color.Value, 16)));
                embed.Title = "A message has been deleted";
                embed.Description = "\"" + message.Content + "\"";
                embed.WithCurrentTimestamp();
                embed.AddField("in " + channel.Name, "sent by: " + message.Author);
                embed.ThumbnailUrl = message.Author.GetAvatarUrl();
                await sendChannel.SendMessageAsync("", false, embed.Build());
            }
        }

        public static Task WriteLog(LogMessage lmsg)
        {
            var cc = Console.ForegroundColor;
            switch (lmsg.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }
            Console.WriteLine($"{DateTime.Now} [{lmsg.Severity,8}] {lmsg.Source}: {lmsg.Message}");
            Console.ForegroundColor = cc;
            return Task.CompletedTask;
        }

        public async Task HandleMessage(SocketMessage pMsg)
        {
            var message = pMsg as SocketUserMessage;

            //Don't handle if system message
            if (message == null)
                return;

            await handleCommand(message);
            await reactionHandler.ParseReactions(message);
        }

        private async Task handleCommand(SocketUserMessage message)
        {
            var db = new BotBaseContext();
            var context = new SocketCommandContext(bot, message);
            //Mark where the prefix ends and the command begins
            int argPos = 0;
            var config = db.Configuration.AsQueryable().Where(cfg => cfg.Name == configName).First();

            //Determine if the message has a valid prefix, adjust argPos
            if (message.HasStringPrefix(config.Prefix, ref argPos))
            {
                if (message.Author.IsBot)
                    return;
                if (message.HasStringPrefix(config.Prefix + "help", ref argPos))
                    await tryHelp(message);
                if (message.HasStringPrefix(config.Prefix + "commands", ref argPos))
                    await printCommands(message);
                //Execute the command, store the result
                var result = await commands.ExecuteAsync(context, argPos, map);

                //If the command failed, notify the user
                if (!result.IsSuccess && result.ErrorReason != "Unknown command.")

                    await message.Channel.SendMessageAsync($"**Error:** {result.ErrorReason}");
            }
        }
        public async Task tryHelp(SocketUserMessage msg)
        {
            var db = new BotBaseContext();
            var config = db.Configuration.AsQueryable().AsQueryable().Where(cfg => cfg.Name == configName).First();
            string commandName = msg.Content;
            string desc = commandName.Remove(0, 5) + " is not a command, make sure the spelling is correct.";
            commandName = commandName.ToLower();
            commandName = commandName.Remove(0, 5);
            commandName = commandName.Replace(" ", string.Empty);
            if (commandName == "")
            {
                await printCommands(msg);
                return;
            }

            else if (commandName == "help")
                desc = "Used to get the descriptions of other commands.\nUsage: " + config.Prefix + "help CommandName";

            else if (commandName == "commands")
                desc = "Shows all available commands.\nUsage: " + config.Prefix + "commands";

            else 
            {
                Discord.Commands.CommandInfo cmd = null;
                foreach (Discord.Commands.CommandInfo c in this.commands.Commands)
                {
                    if (c.Name == commandName)
                    {
                        cmd = c;
                        break;
                    }
                    else foreach (string alias in c.Aliases)
                    {
                        if (alias == commandName)
                        {
                            cmd = c;
                            break;
                        }
                    }
                }
                if (cmd != null)
                {
                    desc = cmd.Summary.Replace("-p-", config.Prefix) + "\nUsage: " + cmd.Remarks.Replace("-c-", $"{config.Prefix}{cmd.Name}");
                    if (cmd.Aliases.Count > 1)
                    {
                        desc += "\nAlso works for ";
                        foreach (string alias in cmd.Aliases)
                        {
                            if (alias == cmd.Name)
                            {
                                continue;
                            }
                            desc += config.Prefix + alias + " ";
                        }
                    }
                }
            }

            await msg.Channel.SendMessageAsync(desc);
            return;
        }

        public async Task printCommands(SocketUserMessage msg)
        {
            var db = new BotBaseContext();
            var config = db.Configuration.AsQueryable().AsQueryable().Where(cfg => cfg.Name == configName).First();

            var categories = new Dictionary<string, List<string>>();
            foreach (Discord.Commands.CommandInfo c in this.commands.Commands)
            {
                if (c.Module.Name == "Hidden")
                    continue;
                else if (!categories.ContainsKey(c.Module.Name))
                {
                    List<string> temp = new List<string>();
                    temp.Add(c.Name);
                    categories.Add(c.Module.Name, temp);
                }
                else
                    categories[c.Module.Name].Add(c.Name);
            }

            var embed = new EmbedBuilder();
            var color = db.Variable.AsQueryable().Where(V => V.Name == "embedColor").FirstOrDefault();
            embed.WithColor(new Color(Convert.ToUInt32(color.Value, 16)));
            embed.Title = "All commands will begin with a " + config.Prefix + " , for more information on individual commands, use: " + config.Prefix + "help commandName";
            embed.Description = "Contact Jif#3952 with any suggestions for more commands. To see all command defintions together, visit https://vertigeux.github.io/jifbot.html";
            embed.WithFooter("Made with love");

            foreach (var category in categories)
            {
                string commands = "";
                foreach (string command in category.Value)
                    commands += command + ", ";
                commands = commands.Remove(commands.Length - 2);
                embed.AddField(category.Key, commands);
            }
            await msg.Channel.SendMessageAsync("", false, embed.Build());
        }
    }
}
