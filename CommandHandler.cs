﻿using System.Threading.Tasks;
using System.Reflection;
using Discord.Commands;
using Discord.WebSocket;
using Discord;
using System;
using System.IO;
using JifBot.Config;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace JifBot.CommandHandler
{
    public class CommandHandler
    {
        public CommandService commands;
        private DiscordSocketClient bot;
        private IServiceProvider map;
        private bool cycle = false;

        public CommandHandler(IServiceProvider provider)
        {
            map = provider;
            bot = map.GetService<DiscordSocketClient>();
            bot.UserJoined += AnnounceUserJoined;
            bot.UserLeft += AnnounceLeftUser;
            bot.MessageDeleted += Audit;
            //Send user message to get handled
            bot.MessageReceived += HandleCommand;
            commands = map.GetService<CommandService>();
        }
        public async Task AnnounceLeftUser(SocketGuildUser user)
        {
            Console.WriteLine("User " + user.Username + " Left " + user.Guild.Name);
            var embed = new EmbedBuilder();
            embed.WithColor(new Color(0x42ebf4));

            {
                IGuild temp = user.Guild;
                var channel = await temp.GetDefaultChannelAsync();
                if (user.Guild.Id == 366129048029495296)
                    channel = await temp.GetTextChannelAsync(398305757784702977);
                if (user.Guild.Id == 374014790793691137)
                    channel = await temp.GetTextChannelAsync(442745612908232734);
                if (user.Guild.Id == 301897718824173570)
                    channel = await temp.GetTextChannelAsync(566425762089926677);
                {
                    embed.ThumbnailUrl = user.GetAvatarUrl();
                    embed.Title = $"**{user.Username} Left The Server:**";
                    embed.Description = $"**User:**{user.Mention}";
                    embed.WithCurrentTimestamp();
                    await channel.SendMessageAsync("", false, embed);
                }
            }
        }
        public async Task Audit(Cacheable<IMessage,ulong> cache, ISocketMessageChannel channel)
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
            embed.Description = "in " + channel.Name;
            embed.WithCurrentTimestamp();
            embed.AddField("\"" + message.Content + "\"","sent by: " + message.Author);
            embed.ThumbnailUrl = message.Author.GetAvatarUrl();
            await user.SendMessageAsync("", false, embed);
            await mod.SendMessageAsync("", false, embed);

        }
        public async Task AnnounceUserJoined(SocketGuildUser user)
        {
            Console.WriteLine("User " + user.Username + " Joined " + user.Guild.Name);

            IGuild temp = user.Guild;
            var channel = await temp.GetDefaultChannelAsync();
            if (user.Guild.Id == 366129048029495296)
                channel = await temp.GetTextChannelAsync(398305757784702977);
            if (user.Guild.Id == 374014790793691137)
                channel = await temp.GetTextChannelAsync(442745612908232734);
            if (user.Guild.Id == 301897718824173570)
                channel = await temp.GetTextChannelAsync(566425762089926677);

            var embed = new EmbedBuilder();
            embed.ThumbnailUrl = user.GetAvatarUrl();
            embed.WithColor(new Color(0x42ebf4));
            embed.Title = $"**{user.Username} Joined The Server:**";
            embed.Description = ($"**User:** {user.Mention}");
            embed.WithCurrentTimestamp();
            await channel.SendMessageAsync("", false, embed: embed);

        }
        public async Task ConfigureAsync()
        {
            await commands.AddModulesAsync(Assembly.GetEntryAssembly());
            await bot.SetGameAsync("~commands");
        }

        public async Task HandleCommand(SocketMessage pMsg)
        {
            if (!cycle)
            {
                cycle = true;
                Task.Run(() => StreamCycle());
            }

            //Don't handle the command if it is a system message
            var message = pMsg as SocketUserMessage;
            if (message == null)
                return;
            var context = new SocketCommandContext(bot, message);

            //Mark where the prefix ends and the command begins
            int argPos = 0;
            //Determine if the message has a valid prefix, adjust argPos
            if (message.HasStringPrefix(BotConfig.Load().Prefix, ref argPos))
            {
                if (message.Author.IsBot)
                    return;
                if (message.HasStringPrefix("~help", ref argPos))
                    await tryHelp(message);
                //Execute the command, store the result
                var result = await commands.ExecuteAsync(context, argPos, map);

                //If the command failed, notify the user
                if (!result.IsSuccess && result.ErrorReason != "Unknown command.")

                    await message.Channel.SendMessageAsync($"**Error:** {result.ErrorReason}");
            }
            await CheckSignature(message);
            await CheckKeyword(message);
        }


        public async Task CheckKeyword(SocketUserMessage msg)
        {
            if (msg.Author.IsBot)
                return;
            if (msg.Channel.Id == 532437794530787328 || msg.Channel.Id == 534141269870510110 || msg.Channel.Id == 532968642183299082 || msg.Channel.Id == 543961887914721290)
                return;
            string words = msg.Content.ToString();

            if (words.ToLower().Contains("delet this") || words.ToLower().Contains("delete this"))
                await msg.Channel.SendFileAsync("reactions/deletthis.jpg");

            if (words.ToLower().Equals(":o") || words.Equals(":0"))
                await msg.Channel.SendMessageAsync(":O");

            if (words.ToLower().Contains("fiora"))
            {
                await msg.Channel.SendFileAsync("reactions/fiora.jpg");
                await msg.Channel.SendMessageAsync("**Salty Reese activated**");
            }

            if (words.ToLower().Contains(" nani ") || words.ToLower().Equals("nani") || words.ToLower().StartsWith("nani ") || words.ToLower().EndsWith(" nani"))
            {
                await msg.Channel.SendFileAsync("reactions/nani.jpg");
                await msg.Channel.SendMessageAsync("**NANI?!?!**");
            }

            if (words.ToLower().Contains("be") && words.ToLower().Contains("gone") && words.ToLower().Contains("thot"))
                await msg.Channel.SendFileAsync("reactions/thot.jpg");

            if (words.ToLower().Contains("kys"))
            {
                await msg.AddReactionAsync(new Emoji("🇹"));
                await msg.AddReactionAsync(new Emoji("🇴"));
                await msg.AddReactionAsync(new Emoji("🇽"));
                await msg.AddReactionAsync(new Emoji("🇮"));
                await msg.AddReactionAsync(new Emoji("🇨"));
            }

            if (words.ToLower().Contains("kms"))
            {
                await msg.Channel.SendFileAsync("reactions/kms.png");
            }

            if (words.ToLower().Equals("stop"))
                await msg.Channel.SendFileAsync("reactions/stop.png");

            if (words.ToLower().Contains("bamboozle"))
                await msg.Channel.SendFileAsync("reactions/bamboozle.png");

            if (words.ToLower().Equals("hi") || words.ToLower().Equals("hello") || words.ToLower().Equals("hey") || words.ToLower().Equals("yo") || words.ToLower().Equals("henlo"))
            {
                string temp = File.ReadAllText("references/hello.txt");
                List<string> greetings = new List<string>();
                while (temp.Contains("\n"))
                {
                    greetings.Add(temp.Remove(temp.IndexOf("\r\n")));
                    temp = temp.Remove(0, temp.IndexOf("\r\n") + 2);
                }
                Random rnd = new Random();
                int num = rnd.Next(greetings.Count);
                await msg.Channel.SendMessageAsync(greetings[num]);
            }

            if (words.ToLower().Contains("ahhhhh"))
                await msg.Channel.SendMessageAsync("https://www.youtube.com/watch?v=yBLdQ1a4-JI");

            if (words.ToLower().Contains("@here") || words.ToLower().Contains("@everyone"))
                await msg.Channel.SendMessageAsync("<:ping:377208255132467233>");

            if (words.ToLower().Contains("i mean") && msg.Author.Id == 150084781864910848)
            {
                string file = "references/mean.txt";
                Int32 num = Convert.ToInt32(File.ReadAllText(file));
                num++;
                await msg.Channel.SendMessageAsync("<@150084781864910848> you've said \"I mean\" " + num + " times.");
                File.WriteAllText(file, Convert.ToString(num));
            }


            var mentionedUsers = msg.MentionedUsers;
            foreach (SocketUser mention in mentionedUsers)
            {
                if (mention.Id == 315569278101225483)
                {
                    if (words.ToLower().Contains("play despacito"))
                        await msg.Channel.SendMessageAsync("https://www.youtube.com/watch?v=kJQP7kiw5Fk");
                    else if (msg.Author.Id == 186584509226024960)
                            await msg.Channel.SendMessageAsync("you're pretty ❤");
                    else
                        await msg.Channel.SendMessageAsync("<:ping:377208255132467233>");
                }
            }


            if (words.ToLower().Equals("stale"))
                await msg.Channel.SendFileAsync("reactions/stale.png");

            if (words.ToLower().Contains(" honk ") || words.ToLower().Equals("honk") || words.ToLower().StartsWith("honk ") || words.ToLower().EndsWith(" honk"))
            {
                await msg.Channel.SendFileAsync("reactions/honk.jpg");
                await msg.Channel.SendMessageAsync("**HONK**");
            }

            if (words.ToLower().Contains("~announce") && msg.Author.Id == 150084781864910848)
            {
                words = words.Remove(0, 9);
                foreach (IGuild temp in this.bot.Guilds)
                {
                    var channel = await temp.GetDefaultChannelAsync();
                    if (temp.Id == 366129048029495296)
                        channel = await temp.GetTextChannelAsync(398305757784702977);
                    try
                    {
                        await channel.SendMessageAsync(words);
                    }
                    catch
                    {

                    }
                }
            }
        }


        public async Task CheckSignature(SocketUserMessage msg)
        {
            string temp = File.ReadAllText("references/signatures.txt");
            if (temp != "")
            {
                string name = msg.Author.Username + "#" + msg.Author.Discriminator;
                string id = Convert.ToString(msg.Author.Id);
                Int32 start = temp.IndexOf(id);
                if (start == -1)
                {
                    return;
                }
                start = start + id.Length;
                temp = temp.Remove(0, start);
                string end = "\r\n\r\n";
                start = temp.IndexOf(end);
                temp = temp.Remove(start);
                temp = temp.Replace(" ", string.Empty);
                Emoji react = new Emoji(temp);
                await msg.AddReactionAsync(react);
            }
        }

        public async Task tryHelp(SocketUserMessage msg)
        {
            string commandName = msg.Content;
            string desc = commandName.Remove(0, 5) + " is not a command, make sure the spelling is correct.";
            commandName = commandName.ToLower();
            commandName = commandName.Remove(0, 5);
            commandName = commandName.Replace(" ", string.Empty);
            if (commandName == "help")
                desc = "Used to get the descriptions of other commands.\nUsage: ~help CommandName";
            if (commandName == "")
            {
                desc = "please supply a command name.\nFor a list of available commands, type: ~commands";
            }
            else foreach (Discord.Commands.CommandInfo c in this.commands.Commands)
            {
                if (c.Name == commandName)
                {
                    desc = c.Remarks;
                }
            }
            await msg.Channel.SendMessageAsync(desc);
            return;
        }

        public async Task StreamCycle()
        {
            List<ulong> noted = new List<ulong>();
            var timeElapsed = new Dictionary<ulong,int>();
            IGuild server = bot.GetGuild(301918679262691330);
            SocketTextChannel channel = (SocketTextChannel)await server.GetChannelAsync(403569198615232522);
            while (true)
            {
                var people = server.GetUsersAsync();
                foreach (IGuildUser person in people.Result)
                {
                    if (person.Game != null && person.Game.Value.StreamUrl != null)
                    {
                        if (!noted.Contains(person.Id))
                        {
                            await channel.SendMessageAsync(person.Username + " is currently streaming at " + person.Game.Value.StreamUrl);
                            noted.Add(person.Id);
                        }
                        if (timeElapsed.ContainsKey(person.Id))
                            timeElapsed.Remove(person.Id);
                    }

                    else if (noted.Contains(person.Id))
                    {
                        if (timeElapsed.ContainsKey(person.Id))
                        {
                            timeElapsed[person.Id]++;
                            if(timeElapsed[person.Id] > 120)
                            {
                                noted.Remove(person.Id);
                                timeElapsed.Remove(person.Id);
                            }
                        }
                        else
                            timeElapsed.Add(person.Id, 0);
                    }
                }
                System.Threading.Thread.Sleep(60 * 1000);
            }
        }
    }
}