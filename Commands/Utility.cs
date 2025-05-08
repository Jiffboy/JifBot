﻿using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Diagnostics;
using Discord;
using Discord.Interactions;
using JifBot.Models;
using JIfBot;
using System.Data;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace JifBot.Commands
{
    public class Utility : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("timer", "Sets a reminder to ping you after a certain amount of time has passed.")]
        public async Task Timer(
            [Summary("minutes","The number of minutes to wait.")] int minutes=0,
            [Summary("hours", "The number of hours to wait.")] int hours=0,
            [Summary("days", "The number of days to wait.")]int days=0,
            [Summary("weeks", "The number of weeks to wait.")] int weeks=0,
            [Summary("message", "The message to ping you with after the time runs out")] string message="")

        {
            int waitTime = 0;
            waitTime += minutes;
            waitTime += hours * 60;
            waitTime += days * 1440;
            waitTime += weeks * 10080;

            if (waitTime == 0)
            {
                var db = new BotBaseContext();
                var config = db.Configuration.AsQueryable().Where(cfg => cfg.Name == Program.configName).First();
                await RespondAsync($"Please provide an amount of time to wait for.", ephemeral: true);
                return;
            }

            if (message.Replace(" ", "") == "")
                message = "Times up!";
            Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = "/bin/bash";
            proc.StartInfo.Arguments = "../../../Scripts/sendmessage.sh " + Context.Channel.Id + " \"" + Context.User.Mention + " " + message + "\" " + waitTime;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.Start();

            await RespondAsync("Setting timer for " + formatMinutesToString(waitTime) + " from now.");
        }

        [SlashCommand("choose", "Randomly makes a choice for you.")]
        public async Task Choose(
            [Summary("choices", "The choices to choose from, separated by spaces. Surround options with spaces with quotation marks.")] string choices)
        {
            List<string> choiceList = listFromString(choices);

            if (choiceList.Count < 2)
            {
                await RespondAsync("Invalid choices. Ensure all quotes are closed, and there are at least two options.", ephemeral: true);
                return;
            }

            Random rnd = new Random();
            int num = rnd.Next(choiceList.Count);
            await RespondAsync("The robot overlords have chosen: **" + choiceList[num] + "**");
        }

        
        [SlashCommand("youtube", "Takes whatever you give it and searches for it on YouTube, returning the first result.")]
        public async Task Youtube(
            [Summary("search", "The video title you wish to search for.")] string search)
        {
            search = "https://www.youtube.com/results?search_query=" + search.Replace(" ", "+");
            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
            string html = await client.GetStringAsync(search);
            html = html.Remove(0, html.IndexOf("?v=") + 3);
            html = html.Remove(html.IndexOf("\""));
            await RespondAsync("https://www.youtube.com/watch?v=" + html);
        }

        
        [SlashCommand("8ball", "Asks the magic 8 ball a question.")]
        public async Task eightBall(
            [Summary("Question", "The question to ask the Magic 8 ball. This isn't necessary, but it is more fun!")] string question="")
        {
            string[] responses = new string[] { "it is certain", "It is decidedly so", "Without a doubt", "Yes definitely", "You may rely on it", "As I see it, yes", "Most likely", "Outlook good", "Yes", "Signs point to yes", "Reply hazy try again", "Ask again later", "Better not tell you now", "Cannot predict now", "Concentrate and ask again", "Don't count on it", "My reply is no", "My sources say no", "Outlook not so good", "Very doubtful" };
            Random rnd = new Random();
            int num = rnd.Next(20);
            await RespondAsync(responses[num]);
        }

        [SlashCommand("random", "Generates a random number between bounds.")]
        public async Task random(
            [Summary("upperBound", "Defaults to 999")] int upper=999,
            [Summary("lowerBound", "Defaults to 0")] int lower=0)
        {
            if (upper <= lower)
            {
                await RespondAsync("Invalid Range. Make sure Upper bound is greater than Lower bound", ephemeral: true);
                return;
            }

            Random rnd = new Random();
            await RespondAsync($"-# {lower}-{upper}\n{rnd.Next(lower, upper+1)}");
        }

        [SlashCommand("roll", "Rolls a specified number of dice, with a specified number of sides. Rolls a 1d20 if unspecified.")]
        public async Task Dice(
            [Summary("dice", "The number of dice to be rolled.")] int dice=1,
            [Summary("sides", "The number of sides on the dice being rolled.")] int sides=20,
            [Summary("modifier", "The value to be added onto the end result. This value can be negative.")] int modifier=0,
            [Choice("Advantage", "a")]
            [Choice("Disadvantage", "d")]
            [Choice("Drop Highest", "dh")]
            [Choice("Drop Lowest", "dl")]
            [Choice("Keep Highest", "kh")]
            [Choice("Keep Lowest", "kl")]
            [Summary("options", "Modifies how the final value is chosen.")] string option="",
            [Summary("dropkeepcount", "Designates the number of rolls to drop or keep if specified. Defaults to 1.")] int dropkeepcount=1)
        {

            Random rnd = new Random();
            List<int> rolls = new List<int>();
            String rollsPrint = "";

            if (dice <= 0 || sides <= 0)
            {
                await RespondAsync("Dice and sides must be greater than 0.", ephemeral: true);
                return;
            }

            if ((option == "kh" || option == "kl" || option == "dh" || option == "dl") && dropkeepcount >= dice)
            {
                await RespondAsync("Drop/Keep count must be fewer than the number of rolls.", ephemeral: true);
                return;
            }

            if (option == "a" || option == "d")
            {
                // Can't trust em
                dice = 1;
            }

            for (int i = 0; i < dice; i++)
            {
                rolls.Add(rnd.Next(1, sides));
            }

            if (option == "kh" || option == "kl" || option == "dh" || option == "dl")
            {
                List<int> sortedList = new List<int>(rolls);
                sortedList.Sort();
                switch(option)
                {
                    case "kh":
                        sortedList.RemoveRange(0, sortedList.Count - dropkeepcount);
                        break;
                    case "kl":
                        sortedList.RemoveRange(dropkeepcount, sortedList.Count - dropkeepcount);
                        break;
                    case "dh":
                        sortedList.RemoveRange(sortedList.Count - dropkeepcount, dropkeepcount);
                        break;
                    case "dl":
                        sortedList.RemoveRange(0, dropkeepcount);
                        break;
                    default:
                        break;
                }
                List<string> formattedRolls = new List<string>();
                for (int i = rolls.Count-1; i >= 0; i--)
                {
                    if (sortedList.Contains(rolls[i]))
                    {
                        formattedRolls.Add($"__**{rolls[i]}**__");
                        sortedList.Remove(rolls[i]);
                    }
                    else
                    {
                        formattedRolls.Add($"{rolls[i]}");
                        rolls.RemoveAt(i);
                    }
                }
                rollsPrint = String.Join("   ", formattedRolls);
            }
            else if (option == "a" || option == "d")
            {
                rolls.Add(rnd.Next(1, sides));
                int failIndex = 0;
                if (option == "d")
                    failIndex = rolls[0] <= rolls[1] ? 1 : 0;
                else if (option == "a")
                    failIndex = rolls[0] >= rolls[1] ? 1 : 0;

                rollsPrint = failIndex == 0 ? $"{rolls[0]}   __**{rolls[1]}**__" : $"__**{rolls[0]}**__   {rolls[1]}";
                rolls.RemoveAt(failIndex);
            }
            else
            {
                rollsPrint = String.Join("   " , rolls);
            }

            int total = rolls.Sum() + modifier;
            string modPrint = modifier > 0 ? $" + {modifier}" : modifier < 0 ? $" - {Math.Abs(modifier)}" : "";
            string dicePrint = $"{dice}d{sides}{option}{modPrint}";
            string totalPrint = dice > 1 || modifier != 0 || option != "" ? $"\n## {total}" : "";

            await RespondAsync($"-# {dicePrint}\nRolled: {rollsPrint}{totalPrint}");
        }

        [SlashCommand("calculator", "Solves an arithmetic equation.")]
        public async Task Calculator(
            [Summary("equation", "The arithmetic equation to solve in plain text.")] string equation)
        {
            DataTable dt = new DataTable();
            var result = dt.Compute(equation,"");
            await RespondAsync(result.ToString());
        }

        [SlashCommand("poll", "Creates a strawpoll and returns the link.")]
        public async Task Poll(
            [Summary("question", "The question being asked.")] string question,
            [Summary("answers", "The answers to choose from, separated by spaces.Surround options with spaces with quotation marks.")] string answers)
        {
            List<string> answerList = listFromString(answers);

            if (answerList.Count < 2)
            {
                await RespondAsync("Invalid choices. Ensure all quotes are closed, and there are at least two options.", ephemeral: true);
                return;
            }

            var db = new BotBaseContext();
            string key = db.Variable.AsQueryable().Where(v => v.Name == "strawpollKey").First().Value;
            string stringToSend = "{ \"poll\": {\"title\": \"" + question + "\",\"answers\": [";
            foreach (string answer in answerList)
            {
                stringToSend += "\"" + answer + "\"";
                
                if(answer == answerList.Last())
                {
                    stringToSend += "]}}";
                }
                else
                {
                    stringToSend += ",";
                }
            }

            var stringContent = new StringContent(stringToSend);

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("API-KEY", key);
            HttpResponseMessage response = await client.PostAsync("https://strawpoll.com/api/poll", stringContent);
            HttpContent content = response.Content;
            string stuff = await content.ReadAsStringAsync();
            var json = JObject.Parse(stuff);
            await RespondAsync("https://strawpoll.com/" + (string)json.SelectToken("content_id"));
        }

        [SlashCommand("avatar", "Gets the avatar for a user.")]
        public async Task Avatar(
            [Summary("user", "The Discord user to retrieve the avatar for.")] IGuildUser user,
            [Summary("serverAvatar", "Specifies to get the server avatar instead of the profile avatar. Defaults to false.")] bool serverAvatar=false)
        {
            var embed = new EmbedBuilder();
            string url = "";
            if (serverAvatar)
            {
                url = user.GetGuildAvatarUrl();
            }
            else
            {
                url = user.GetAvatarUrl();
            }
            url = url.Remove(url.IndexOf("?size=128"));
            url = url + "?size=256";
            embed.ImageUrl = url;
            await RespondAsync(embed: embed.Build());
        }

        [SlashCommand("teamup", "Used to randomly select people into teams")]
         public async Task TeamUp(
            [Choice("League of Legends", "league")]
            [Choice("VALORANT", "valorant")]
            [Choice("FFXIV Light Party", "lightparty")]
            [Choice("FFXIV Full Party", "fullparty")]
            [Choice("Custom", "custom")]
            [Summary("template", "The template to assign members to standard team compositions. Select custom to create your own")] string groupType,
            [Summary("players", "The players to choose from, separated by spaces. Surround options with spaces with quotation marks.")] string players,
            [Summary("customTeamCount", "Only viable if custom template selected. The number of teams to create. Defaults to 2")] int customTeamCount=2,
            [Summary("customTeamSize", "Only viable if custom template selected. The maximum number of players on a team. Defaults to 5")] int customTeamSize=5)
        {
            List<string> playerList = listFromString(players);
            if (playerList.Count < 2)
            {
                await RespondAsync("Invalid choices. Ensure all quotes are closed, and there are at least two options.", ephemeral: true);
                return;
            }

            List<Team> teams = new List<Team>();
            Random rnd = new Random();
            int currTeam = 0;

            switch(groupType)
            {
                case "league":
                    teams.Add(new Team("Red Side", 5));
                    teams.Add(new Team("Blue Side", 5));
                    break;
                case "valorant":
                    teams.Add(new Team("Attackers", 5));
                    teams.Add(new Team("Defenders", 5));
                    break;
                case "lightparty":
                    for (int i = 0; i < playerList.Count / 4; i++)
                    {
                        teams.Add(new Team("Tank", 1));
                        teams.Add(new Team("Healer", 1));
                        teams.Add(new Team("DPS", 2));
                    }
                    if (teams.Count == 0)
                    {
                        await RespondAsync("Please specify enough people to fill at least one party.", ephemeral: true);
                        return;
                    }
                    break;
                case "fullparty":
                    for (int i = 0; i < playerList.Count / 8; i++)
                    {
                        teams.Add(new Team("Tanks", 2));
                        teams.Add(new Team("Healers", 2));
                        teams.Add(new Team("DPS", 4));
                    }
                    if (teams.Count == 0)
                    {
                        await RespondAsync("Please specify enough people to fill at least one party.", ephemeral: true);
                        return;
                    }
                    break;
                default:
                case "custom":
                    for (int i = 0; i < customTeamCount; i++)
                    {
                        teams.Add(new Team($"Team {i+1}", customTeamSize));
                    }
                    break;

            }

            while (playerList.Count > 0)
            {
                // We've exceeded max team size, check to see if there are any left with room
                if (teams[currTeam].members.Count == teams[currTeam].maxCount)
                {
                    bool canContinue = false;
                    for(int i = 0; i < teams.Count; i++)
                    {
                        if (teams[i].members.Count != teams[i].maxCount)
                        {
                            currTeam = i;
                            canContinue = true;
                        }
                    }
                    if (!canContinue)
                    {
                        break;
                    }
                }

                int num = rnd.Next(playerList.Count);
                teams[currTeam].members.Add(playerList[num]);
                playerList.RemoveAt(num);
                currTeam++;

                // We've gone through all teams, back to the first
                if(currTeam == teams.Count)
                {
                    currTeam = 0;
                }
            }

            var embed = new JifBotEmbedBuilder();
            embed.Title = "Team Allocation";
            embed.Description = "Groups are as follows. Good luck!";

            currTeam = 1;

            foreach (Team team in teams)
            {
                string teamString = "";
                foreach (string teammate in team.members)
                {
                    teamString += $"- {teammate}\n";
                }
                embed.AddField(team.name, teamString, inline: true);
                currTeam++;
            }

            if(playerList.Count > 0)
            {
                string teamString = "";
                foreach (string teammate in playerList)
                {
                    teamString += $"- {teammate}\n";
                }
                embed.AddField("Benched", teamString, inline: true);
            }

            await RespondAsync(embed: embed.Build());
        }

        public async Task<IGuildUser> getUser(IGuild guild, ulong id)
        {
            await guild.DownloadUsersAsync();
            RequestOptions request = new RequestOptions();
            CancellationToken cancel = request.CancelToken;
            IGuildUser user = await guild.GetUserAsync(id, mode: CacheMode.CacheOnly, options: request);

            return user;
        }
        
        string formatMinutesToString(int minutes)
        {
            string format = "";

            if (minutes / 10080 > 0)
            {
                format += Convert.ToString(minutes / 10080) + " week";
                if (minutes / 10080 > 1)
                    format += "s";
                format += ", ";
                minutes = minutes % 10080;
            }

            if (minutes / 1440 > 0)
            {
                format += Convert.ToString(minutes / 1440) + " day";
                if (minutes / 1440 > 1)
                    format += "s";
                format += ", ";
                minutes = minutes % 1440;
            }

            if (minutes / 60 > 0)
            {
                format += Convert.ToString(minutes / 60) + " hour";
                if (minutes / 60 > 1)
                    format += "s";
                format += ", ";
                minutes = minutes % 60;
            }

            if (minutes > 0)
            {
                format += Convert.ToString(minutes) + " minute";
                if (minutes > 1)
                    format += "s";
            }
            else
                format = format.Remove(format.Length - 2, 2);

            return format;
        }

        List<string> listFromString(string choices)
        {
            List<string> choiceList = new List<string>();
            choices = choices.Replace("”", "\"");
            choices = choices.Replace("“", "\"");
            int quotes = choices.Split('\"').Length - 1;
            if (quotes % 2 != 0)
            {
                return choiceList;
            }

            MatchCollection matchList = Regex.Matches(choices, @"[^\s""]+|""([^""]*)""");
            choiceList = matchList.Cast<Match>().Select(match => match.Value.Replace("\"", "")).ToList();

            return choiceList;
        }
    }
    public class Team
    {
        public Team(string name, int maxCount)
        {
            this.name = name;
            this.maxCount = maxCount;
            members = new List<string>();
        }

        public string name { get; set; }
        public int maxCount { get; set; }
        public List<string> members { get; set; }
    }
}