using System.Threading.Tasks;
using System.Reflection;
using Discord.Commands;
using Discord.WebSocket;
using Discord;
using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JifBot.Models;
using System.Linq;
using JIfBot;

namespace JifBot.CommandHandler
{
    public class CommandHandler
    {
        public CommandService commands;
        private DiscordSocketClient bot;
        private IServiceProvider map;
        private string configName;
        private BotBaseContext db = new BotBaseContext();

        public CommandHandler(IServiceProvider provider)
        {
            map = provider;
            configName = Program.configName;
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
        }

        public async Task HandleCommand(SocketMessage pMsg)
        {
            //Don't handle the command if it is a system message
            var message = pMsg as SocketUserMessage;
            if (message == null)
                return;
            var context = new SocketCommandContext(bot, message);

            //Mark where the prefix ends and the command begins
            int argPos = 0;
            var config = db.Configuration.Where(cfg => cfg.Name == configName).First();

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
                await msg.Channel.SendFileAsync("media/deletthis.jpg");

            if (words.ToLower().Equals(":o") || words.Equals(":0"))
                await msg.Channel.SendMessageAsync(":O");

            if (words.ToLower().Contains("fiora"))
            {
                await msg.Channel.SendFileAsync("media/fiora.jpg");
                await msg.Channel.SendMessageAsync("**Salty Reese activated**");
            }

            if (words.ToLower().Contains(" nani ") || words.ToLower().Equals("nani") || words.ToLower().StartsWith("nani ") || words.ToLower().EndsWith(" nani"))
            {
                await msg.Channel.SendFileAsync("media/nani.jpg");
                await msg.Channel.SendMessageAsync("**NANI?!?!**");
            }

            if (words.ToLower().Contains("be") && words.ToLower().Contains("gone") && words.ToLower().Contains("thot"))
                await msg.Channel.SendFileAsync("media/thot.jpg");

            if (words.ToLower().Contains("kms"))
            {
                await msg.Channel.SendFileAsync("media/kms.png");
            }

            if (words.ToLower().Equals("stop"))
                await msg.Channel.SendFileAsync("media/stop.png");

            if (words.ToLower().Contains("bamboozle"))
                await msg.Channel.SendFileAsync("media/bamboozle.png");

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

            if (Regex.IsMatch(words.ToLower(), "fuck y?o?u jif ?bot"))
            {
                await msg.DeleteAsync();
                await msg.Channel.SendMessageAsync("Know your place, trash.");
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
                await msg.Channel.SendFileAsync("media/stale.png");

            if (words.ToLower().Contains(" honk ") || words.ToLower().Equals("honk") || words.ToLower().StartsWith("honk ") || words.ToLower().EndsWith(" honk"))
            {
                await msg.Channel.SendFileAsync("media/honk.jpg");
                await msg.Channel.SendMessageAsync("**HONK**");

                var user = db.User.Where(user => user.UserId == msg.Author.Id).FirstOrDefault();
                var honk = db.Honk.Where(honk => honk.UserId == msg.Author.Id).FirstOrDefault();

                if (user == null)
                    db.Add(new User { UserId = msg.Author.Id, Name = msg.Author.Username, Number = long.Parse(msg.Author.Discriminator) });
                else
                {
                    user.Name = msg.Author.Username;
                    user.Number = long.Parse(msg.Author.Discriminator);
                }
                if (honk == null)
                    db.Add(new Honk { UserId = msg.Author.Id, Count = 1 });
                else
                    honk.Count += 1;
                db.SaveChanges();
            }

            var config = db.Configuration.Where(cfg => cfg.Name == configName).First();

            if (words.ToLower().Contains(config.Prefix + "announce") && msg.Author.Id == 150084781864910848)
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
            var config = db.Configuration.Where(cfg => cfg.Name == configName).First();
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

            else foreach (Discord.Commands.CommandInfo c in this.commands.Commands)
                {
                    if (c.Name == commandName)
                    {
                        desc = c.Summary;
                        if (c.Aliases.Count > 1)
                        {
                            desc += "\nAlso works for ";
                            foreach (string alias in c.Aliases)
                            {
                                if (alias == commandName)
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
            var config = db.Configuration.Where(cfg => cfg.Name == configName).First();

            var categories = new Dictionary<string, List<string>>();
            foreach (Discord.Commands.CommandInfo c in this.commands.Commands)
            {
                if (c.Remarks == "Hidden")
                    continue;
                else if (!categories.ContainsKey(c.Remarks))
                {
                    List<string> temp = new List<string>();
                    temp.Add(c.Name);
                    categories.Add(c.Remarks, temp);
                }
                else
                    categories[c.Remarks].Add(c.Name);
            }

            var embed = new EmbedBuilder();
            embed.WithColor(new Color(0x42ebf4));
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
            await msg.Channel.SendMessageAsync("", false, embed);
        }
    }
}