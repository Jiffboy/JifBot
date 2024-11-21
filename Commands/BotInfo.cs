using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Discord.Interactions;
using JifBot.Models;
using JIfBot;

namespace JifBot.Commands 
{
    public class BotInfo : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("commands", "Shows all available commands.")]
        public async Task Commands()
        {
            var db = new BotBaseContext();
            var commands = db.Command.AsQueryable().OrderBy(command => command.Category);
            var config = db.Configuration.AsQueryable().Where(cfg => cfg.Name == Program.configName).First();

            var embed = new JifBotEmbedBuilder();

            embed.Title = "For more information on individual commands, use: /help";
            embed.Description = "Contact Jif#3952 with any suggestions for more commands. To see all command defintions together, visit https://jifbot.com/commands";

            string cat = commands.First().Category;
            string list = "";
            foreach (Command command in commands)
            {
                if (command.Category != cat)
                {
                    embed.AddField($"🏷 {cat}", list.Remove(list.LastIndexOf(", ")));
                    cat = command.Category;
                    list = "";
                }
                list += command.Name + ", ";
            }
            embed.AddField($"🏷 {cat}", list.Remove(list.LastIndexOf(", ")));
            await RespondAsync(embed: embed.Build());
        }

        [SlashCommand("help", "Gets information for a specified Jif Bot command.")]
        public async Task Help(
            [Summary("command", "The command you would like help with")] string commandName)
        {
            var db = new BotBaseContext();
            var command = db.Command.AsQueryable().Where(cmd => cmd.Name == commandName).FirstOrDefault();
            var config = db.Configuration.AsQueryable().Where(cfg => cfg.Name == Program.configName).First();
            if (command == null)
            {
                await RespondAsync($"{commandName} is not a command, make sure the spelling is correct.", ephemeral: true);
                return;
            }
            var parameters = db.CommandParameter.AsQueryable().Where(p => p.Command == command.Name);
            string msg = $"**/{command.Name}**:\n{command.Description}";
            if (parameters.Any())
            {
                msg += "\n\n**Parameters:**\n";
                foreach (CommandParameter parameter in parameters)
                {
                    msg += $"{(parameter.Required ? "[Required]" : "[Optional]")} **{parameter.Name}**: {parameter.Description}\n";
                    var choices = db.CommandParameterChoice.AsQueryable().Where(p => p.Command == command.Name && p.Parameter == parameter.Name);
                    if (choices.Any())
                    {
                        string choiceString = "";
                        foreach (var choice in choices)
                        {
                            choiceString += $"{choice.Name}, ";
                        }
                        // remove last comma
                        choiceString = choiceString.Remove(choiceString.Length - 2, 2);
                        msg += $"> Options: {choiceString}\n";
                    }
                }
            }
            await RespondAsync(msg);
        }

        [SlashCommand("uptime", "Reports how long the bot has been running.")]
        public async Task Uptime()
        {
            TimeSpan uptime = DateTime.Now - Program.startTime;
            await RespondAsync($"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s");
        }

        [SlashCommand("changelog", "Reports recent updates made to Jif Bot.")]
        public async Task Changelog(
            [Summary("count", "The number of changes to pull. Defaults to 3, max of 20.")] int count = 3)
        {
            count = count > 20 ? 20 : count;
            var db = new BotBaseContext();
            var embed = new JifBotEmbedBuilder();
            var totalEntries = db.ChangeLog.AsQueryable().OrderByDescending(e => e.Date);
            var entriesToPrint = new Dictionary<string, string>();

            foreach (ChangeLog entry in totalEntries)
            {
                if (!entriesToPrint.ContainsKey(entry.Date))
                {
                    if (entriesToPrint.Count >= count)
                    {
                        break;
                    }
                    entriesToPrint.Add(entry.Date, $"{GetChangeLogIcon(entry.Type)} {entry.Change}");
                }
                else
                {
                    entriesToPrint[entry.Date] += $"\n{GetChangeLogIcon(entry.Type)} {entry.Change}";
                }
            }

            embed.Title = $"Last {count} Jif Bot updates";
            embed.Description = "For a list of all updates, visit https://jifbot.com/changelog";
            foreach (var entry in entriesToPrint)
            {
                var pieces = entry.Key.Split("-");
                embed.AddField($"{pieces[1]}.{pieces[2]}.{pieces[0]}", entry.Value);
            }
            await RespondAsync(embed: embed.Build());
        }

        [SlashCommand("stats", "Gives Jif Bot usage statistics. General data begins 8/15/2024. User data begins 8/30/2024")]
        public async Task Stats(
            [Choice("Global", "global")]
            [Choice("Server", "server")]
            [Choice("User", "user")]
            [Summary("scope", "The scope of data to pull for")] string scope)
        {
            var db = new BotBaseContext();
            var days = new TimeSpan(30, 0, 0, 0);
            var cutoff = DateTimeOffset.UtcNow.Subtract(days).ToUnixTimeSeconds();
            List<CommandCall> commands = new List<CommandCall>();
            switch (scope)
            {
                case "user":
                    commands = db.CommandCall.AsQueryable().AsQueryable().Where(c => c.UserId == Context.User.Id && c.Timestamp > cutoff).ToList();
                    break;

                case "server":
                    commands = db.CommandCall.AsQueryable().AsQueryable().Where(c => c.ServerId == Context.Guild.Id && c.Timestamp > cutoff).ToList();
                    break;

                case "global":
                    commands = db.CommandCall.AsQueryable().AsQueryable().Where(c => c.Timestamp > cutoff).ToList();
                    break;
            }

            if (commands.Count == 0)
            {
                await RespondAsync("No data found! Either something went wrong, or Jif Bot is DEAD");
            }

            Dictionary<string, int> counts = new Dictionary<string, int>();
            foreach (var command in commands)
            {
                if (counts.ContainsKey(command.Command))
                    counts[command.Command]++;
                else
                    counts[command.Command] = 1;
            }
            var sortedCounts = from c in counts orderby c.Value descending select c;
            int count = 0;
            var msg = $"Total command uses in the past 30 days: {commands.Count}\n";
            foreach (var pair in sortedCounts)
            {
                if (count == 5)
                    break;
                msg += $"{pair.Key}: {pair.Value}\n";
                count++;
            }
            await RespondAsync(msg);

        }

        private string GetChangeLogIcon(string type)
        {
            switch (type)
            {
                case "New":
                    return "✨";
                case "Improved":
                    return "📝";
                case "Bug Fix":
                    return "🛠";
                case "Removed":
                    return "❌";
                default:
                    return "";
            }
        }
    }
}
