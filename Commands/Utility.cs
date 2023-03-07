using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Diagnostics;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using JifBot.Models;
using JIfBot;
using System.Data;
using Newtonsoft.Json.Linq;
using System.Threading;
using Discord.Audio;
using JifBot.HttpClients;

namespace JifBot.Commands
{
    public class Utility : InteractionModuleBase<SocketInteractionContext>
    {
        private ISpoonacularClient _spoonacularClient;

        public Utility(ISpoonacularClient spoonacularClient)
        {
            _spoonacularClient = spoonacularClient;
        }

        [SlashCommand("timer", "Sets a reminder to ping you after a certain amount of time has passed.")]
        [Remarks("-c- -h2 -m30 message, -c- 150 message")]
        public async Task Timer(int minutes=0, int hours=0, int days=0, int weeks=0, string message="")
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
        [Remarks("You can use as many choices as you want, but seperate all choices using a space. If you wish for a choice to contain spaces, surround the choice with \"\"\n. -c- choice \"choice but with spaces\"")]
        public async Task Choose(string choices)
        {
            int quotes = choices.Split('\"').Length - 1;
            if (quotes % 2 != 0)
            {
                await RespondAsync("please ensure all quotations are closed");
                return;
            }

            List<string> choiceList = new List<string>();
            int count = 0;
            choices = choices.TrimEnd();
            while (true)
            {
                choices = choices.TrimStart();
                string choice;
                if (choices == "")
                {
                    break;
                }
                if (choices[0] == '\"')
                {
                    choices = choices.Remove(0, 1);
                    choice = choices.Remove(choices.IndexOf("\""));
                    choices = choices.Remove(0, choices.IndexOf("\"") + 1);
                }
                else
                {
                    if (choices.Contains(" "))
                    {
                        choice = choices.Remove(choices.IndexOf(" "));
                        choices = choices.Remove(0, choices.IndexOf(" "));
                    }
                    else
                    {
                        choice = choices;
                        choices = "";
                    }
                }
                choiceList.Add(choice);
                count++;
            }

            if (count < 2)
            {
                await RespondAsync("Please provide at least two choices.");
                return;
            }

            Random rnd = new Random();
            int num = rnd.Next(count);
            await RespondAsync("The robot overlords have chosen: **" + choiceList[num] + "**");
        }

        
        [SlashCommand("youtube", "Takes whatever you give it and searches for it on YouTube.")]
        [Remarks("-c- video title it will return the first search result that appears.")]
        public async Task Youtube(string search)
        {
            search = "https://www.youtube.com/results?search_query=" + search.Replace(" ", "+");
            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
            string html = await client.GetStringAsync(search);
            html = html.Remove(0, html.IndexOf("?v=") + 3);
            html = html.Remove(html.IndexOf("\""));
            await RespondAsync("https://www.youtube.com/watch?v=" + html);
        }

        
        [SlashCommand("8ball", "Asks the magic 8 ball a question.")]
        [Remarks("-c-")]
        public async Task eightBall(string question="")
        {
            string[] responses = new string[] { "it is certain", "It is decidedly so", "Without a doubt", "Yes definitely", "You may rely on it", "As I see it, yes", "Most likely", "Outlook good", "Yes", "Signs point to yes", "Reply hazy try again", "Ask again later", "Better not tell you now", "Cannot predict now", "Concentrate and ask again", "Don't count on it", "My reply is no", "My sources say no", "Outlook not so good", "Very doubtful" };
            Random rnd = new Random();
            int num = rnd.Next(20);
            await RespondAsync(responses[num]);
        }

        [SlashCommand("roll", "Rolls a specified number of dice, with a specified number of sides.")]
        [Remarks("-c-, -c- 1d20, -c- 2d6a + 4  To quickly roll a 6 sided die, do not specify anything. Max values are 200 dice, 200 sides, and + 1000 modifiers")]
        public async Task Dice(int dice=1, int sides=20, int modifier=0, bool advantage=false, bool disadvantage=false)
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
        [Remarks("-c- ( 5 + 7 ) / 2")]
        public async Task Calculator(string equation)
        {
            DataTable dt = new DataTable();
            var result = dt.Compute(equation,"");
            await RespondAsync(result.ToString());
        }

        /*[Command("poll")]
        [Remarks("-c- Question | Option 1 | Option 2")]
        [Alias("strawpoll")]
        [Summary("Creates a strawpoll and returns the link.")]
        public async Task Poll([Remainder] string input)
        {
            if (input.Split("|").Length < 3)
            {
                await RespondAsync("Please provide a question and at least two options in the format: Question | Option1 | Option2 etc.");
                return;
            }

            var db = new BotBaseContext();
            string key = db.Variable.AsQueryable().Where(v => v.Name == "strawpollKey").First().Value;
            string stringToSend = "{ \"poll\": {\"title\": \"";
            int splitNum = 1;
            foreach (string substring in input.Split("|"))
            {
                if (splitNum == 1)
                    stringToSend += substring + "\",\"answers\": [";
                else
                    stringToSend += "\"" + substring + "\"";
                
                if (splitNum == input.Split("|").Length)
                    stringToSend += "]}}";
                else if (splitNum != 1)
                    stringToSend += ",";
                splitNum++;
            }

            var stringContent = new StringContent(stringToSend);

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("API-KEY", key);
            HttpResponseMessage response = await client.PostAsync("https://strawpoll.com/api/poll", stringContent);
            HttpContent content = response.Content;
            string stuff = await content.ReadAsStringAsync();
            var json = JObject.Parse(stuff);
            await RespondAsync("https://strawpoll.com/" + (string)json.SelectToken("content_id"));
        }*/

        [SlashCommand("avatar", "Gets the avatar for a user.")]
        [Remarks("-c-, -c- @person1 @person2, -c- person1id person2id")]
        public async Task Avatar(IUser user)
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

        [SlashCommand("sauce", "Searches for a kind of sauce.")]
        public async Task SearchSauce(string sauceSearchText = "")
        {
            var response = await _spoonacularClient.GetSauceRecipe(sauceSearchText);

            if (response == null || response.TotalResults == 0)
            {
                await RespondAsync($"Could not find a {sauceSearchText} sauce");
            }
            else
            {
                Random rnd = new Random();
                int num = rnd.Next(response.TotalResults);

                await RespondAsync(response.Results[num].Image);
            }
        }
    }
}