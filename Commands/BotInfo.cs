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
        public async Task Commands(
            [Summary("show", "Whether or not to show to the entire server, instead of just yourself. Defaults to false.")]bool ephemeral = false)
        {
            var db = new BotBaseContext();
            var categories = db.Command.AsEnumerable().GroupBy(command => command.Category).OrderByDescending(g => g.Count());

            var embed = new JifBotEmbedBuilder();

            embed.Title = "For more information on individual commands, use: /help";
            embed.Description = "Contact jiffboy with any suggestions for more commands. For a more detailed overview, visit https://jifbot.com/commands";

            foreach(var category in categories)
            {
                var commands = string.Join('\n', category.Select(e => $"- {e.Name}"));
                embed.AddField($"🏷 {category.Key}", commands, inline: true);
            }
            
            await RespondAsync(embed: embed.Build(), ephemeral: !ephemeral);
        }

        [SlashCommand("help", "Gets information for a specified Jif Bot command.")]
        public async Task Help(
            [Summary("command", "The command you would like help with")] string commandName,
            [Summary("show", "Whether or not to show to the entire server, instead of just yourself. Defaults to false.")] bool ephemeral = false)
        {
            var db = new BotBaseContext();
            var embed = new JifBotEmbedBuilder();
            var command = db.Command.AsQueryable().Where(cmd => cmd.Name == commandName).FirstOrDefault();

            if (command == null)
            {
                await RespondAsync($"{commandName} is not a command, make sure the spelling is correct.", ephemeral: true);
                return;
            }

            var parameters = db.CommandParameter.AsQueryable().Where(p => p.Command == command.Name);
            embed.Title = $"/{command.Name}";
            embed.Description = command.Description;

            if (command.Permissions != null)
            {
                embed.AddField("Permission Requirements", command.Permissions);
            }

            var plist = "";
            if (parameters.Any())
            {
                plist += "";
                foreach (CommandParameter parameter in parameters)
                {
                    plist += $"- {(parameter.Required ? "[Required]" : "[Optional]")} **{parameter.Name}**: {parameter.Description}\n";
                    var choices = db.CommandParameterChoice.AsQueryable().Where(p => p.Command == command.Name && p.Parameter == parameter.Name);
                    if (choices.Any())
                    {
                        var olist = string.Join(", ", choices.Select(c => c.Name));
                        plist += $"  > {olist}\n";
                    }
                }
                embed.AddField("Parameters", plist);
            }

            await RespondAsync(embed: embed.Build(), ephemeral: !ephemeral);
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
            [Summary("scope", "The scope of data to pull for")] string scope,
            [Summary("count", "The number of commands to display. Defaults to 10")] int count = 10)
        {
            var db = new BotBaseContext();
            var embed = new JifBotEmbedBuilder();
            embed.Title = "Total command counts";

            List<CommandCall> commands = new List<CommandCall>();
            switch (scope)
            {
                case "user":
                    commands = db.CommandCall.AsQueryable().Where(c => c.UserId == Context.User.Id).ToList();
                    embed.Title += $" for {Context.User.Username}";
                    break;

                case "server":
                    commands = db.CommandCall.AsQueryable().Where(c => c.ServerId == Context.Guild.Id).ToList();
                    embed.Title += $" for {Context.Guild.Name}";
                    break;

                case "global":
                    commands = db.CommandCall.ToList();
                    embed.Title += " globally";
                    break;
            }

            if (commands.Count == 0)
            {
                await RespondAsync("No data found!", ephemeral: true);
            }

            // All Time
            var all = commands.GroupBy(c => c.Command).OrderByDescending(c => c.Count()).Take(count).ToList();
            embed.AddField("All Time", $"```{GetStatsField(all)}```", inline: true);
            embed.Description = $"**All Time:** {commands.Count}";

            // Month
            var monthCutoff = DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds();
            commands = commands.Where(c => c.Timestamp > monthCutoff).ToList();
            var month = commands.GroupBy(c => c.Command).OrderByDescending(c => c.Count()).Take(count).ToList();
            embed.AddField("Last 30 days", $"```{GetStatsField(month)}```", inline: true);
            embed.Description += $"\n**Last 30 Days:** {commands.Count}";

            // Week
            var weekCutoff = DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds();
            commands = commands.Where(c => c.Timestamp > weekCutoff).ToList();
            var week = commands.GroupBy(c => c.Command).OrderByDescending(c => c.Count()).Take(count).ToList();
            embed.AddField("Last week", $"```{GetStatsField(week)}```", inline: true);
            embed.Description += $"\n**Last Week:** {commands.Count}";

            await RespondAsync(embed: embed.Build());

        }

        private string GetStatsField(List<IGrouping<string, CommandCall>> groupings)
        {
            const int maxlen = 18;
            List<string> entries = new List<string>();
            foreach (var group in groupings)
            {
                var cutoff = maxlen - group.Count().ToString().Count() - 1;
                var cutCommand = group.Key.Count() > cutoff ? group.Key.Substring(0, cutoff) : group.Key;
                var padCommand = cutCommand.PadRight(cutoff + 1);
                entries.Add(padCommand + group.Count().ToString());
            }
            return string.Join("\n", entries);
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
