using System;
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
                await RespondAsync($"Please provide an amount of time to wait for. For assistance, use {config.Prefix}help.");
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
            int quotes = choices.Split('\"').Length - 1;
            if (quotes % 2 != 0)
            {
                await RespondAsync("please ensure all quotations are closed");
                return;
            }

            MatchCollection matchList = Regex.Matches(choices, @"[^\s""]+|""([^""]*)""");
            List<string> choiceList = matchList.Cast<Match>().Select(match => match.Value.Replace("\"","")).ToList();

            if (choiceList.Count < 2)
            {
                await RespondAsync("Please provide at least two choices.");
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

        [SlashCommand("roll", "Rolls a specified number of dice, with a specified number of sides. Rolls a 1d20 if unspecified.")]
        public async Task Dice(
            [Summary("dice", "The number of dice to be rolled. Maximum of 200.")] int dice=1,
            [Summary("sides", "The number of sides on the dice being rolled. Maximum of 200.")] int sides=20,
            [Summary("modifier", "The value to be added onto the end result. This value can be negative. Maximum value of 1000.")] int modifier=0,
            [Summary("advantage", "When set to true, rolls the values twice, and keeps the higher number.")] bool advantage=false,
            [Summary("disadvantage", "When set to true, rolls the values twice, and keeps the lower number.")] bool disadvantage=false)
        {

            Random rnd = new Random();
            string[] rolls = new string[] { "", "" };
            int[] totals = new int[] { 0, 0 };
            int printRoll = 0;
            string msg = "";
            int numRolls = 1;
            if(advantage || disadvantage)
            {
                numRolls = 2;
            }

            if (dice == 0 || sides == 0)
            {
                await RespondAsync("Cannot be 0");
                return;
            }

            if (dice > 200 || sides > 200 || modifier > 1000 || modifier < -1000)
            {
                await Context.Channel.SendFileAsync("Media/joke.jpg");
                return;
            }

            for (int i = 0; i < numRolls; i++)
            {
                for (int j = 0; j < dice; j++)
                {
                    int num = rnd.Next(sides) + 1;
                    totals[i] += num;
                    rolls[i] += $"{num}, ";
                }
                totals[i] += modifier;
                rolls[i] = rolls[i].Remove(rolls[i].LastIndexOf(", "));
            }

            if (numRolls > 1)
            {
                int high = 0;
                msg = $"1st Roll: {rolls[0]}\n2nd Roll: {rolls[1]}";

                if (totals[1] > totals[0])
                    high = 1;

                //advantage
                if (advantage && high == 1)
                    printRoll = 1;

                //disadvantage
                else if (disadvantage && high == 0)
                    printRoll = 1;

                if (printRoll == 0)
                    msg = msg.Replace("1st Roll", "**1st Roll**");
                else
                    msg = msg.Replace("2nd Roll", "**2nd Roll**");
            }
            else
                msg = "Rolled: " + rolls[0];

            if (dice == 1 && numRolls == 1 && modifier == 0)
                await RespondAsync($"{totals[0]}");
            else if (modifier == 0 && dice == 1)
                await RespondAsync($"{msg}");
            else
                await RespondAsync($"{msg}\nTotal: **{totals[printRoll]}**");

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
            MatchCollection matchList = Regex.Matches(answers, @"[^\s""]+|""([^""]*)""");
            List<string> answerList = matchList.Cast<Match>().Select(match => match.Value.Replace("\"", "")).ToList();

            if (answerList.Count < 2)
            {
                await RespondAsync("Please provide at least two answer options.");
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
            [Summary("user", "The Discord user to retrieve the avatar for.")] IUser user)
        {
            var embed = new EmbedBuilder();
            string url = user.GetAvatarUrl();
            url = url.Remove(url.IndexOf("?size=128"));
            url = url + "?size=256";
            embed.ImageUrl = url;
            await RespondAsync("", embed:embed.Build());
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
    }
}