using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Diagnostics;
using Discord;
using Discord.Commands;
using JifBot.Models;
using JIfBot;
using System.Data;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace JifBot.Commands
{
    public class Utility : ModuleBase
    {
        [Command("timer")]
        [Remarks("-c- -h2 -m30 message, -c- 150 message")]
        [Summary("Sets a reminder to ping you after a certain amount of time has passed. A message can be specified along with the time to be printed back to you at the end of the timer. Times can be specified using any combination of -m[minutes], -h[hours], -d[days], and -w[weeks] anywhere in the message. Additionally, to set a quick timer for a number of minutes, just do -p-timer [minutes] message.")]
        public async Task Timer([Remainder]string message = "")
        {
            int waitTime = 0;

            if (Regex.IsMatch(message, @"-(m *[0-9]+|[0-9]+m)"))
            {
                waitTime += Convert.ToInt32(Regex.Match(message, @"-(m *[0-9]+|[0-9]+m)").Value.Replace("-", "").Replace("m", ""));
            }

            if (Regex.IsMatch(message, @"-(h *[0-9]+|[0-9]+h)"))
            {
                waitTime += (Convert.ToInt32(Regex.Match(message, @"-(h *[0-9]+|[0-9]+h)").Value.Replace("-", "").Replace("h", "")) * 60);
            }

            if (Regex.IsMatch(message, @"-(d *[0-9]+|[0-9]+d)"))
            {
                waitTime += (Convert.ToInt32(Regex.Match(message, @"-(d *[0-9]+|[0-9]+d)").Value.Replace("-", "").Replace("d", "")) * 1440);
            }

            if (Regex.IsMatch(message, @"-(w *[0-9]+|[0-9]+w)"))
            {
                waitTime += (Convert.ToInt32(Regex.Match(message, @"-(w *[0-9]+|[0-9]+w)").Value.Replace("-", "").Replace("w", "")) * 10080);
            }

            if (waitTime == 0)
            {
                if (Regex.IsMatch(message, @" *[0-9]+"))
                    waitTime = Convert.ToInt32(message.Split(" ")[0]);
                else
                {
                    var db = new BotBaseContext();
                    var config = db.Configuration.AsQueryable().Where(cfg => cfg.Name == Program.configName).First();
                    await ReplyAsync($"Please provide an amount of time to wait for. For assistance, use {config.Prefix}help.");
                    return;
                }
            }

            message = Regex.Replace(message, @"-([m,h,d,w] *[0-9]+|[0-9]+[m,h,d,w])", "");
            if (message.Replace(" ", "") == "")
                message = "Times up!";
            Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = "/bin/bash";
            proc.StartInfo.Arguments = "../../../Scripts/sendmessage.sh " + Context.Channel.Id + " \"" + Context.User.Mention + " " + message + "\" " + waitTime;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.Start();

            await ReplyAsync("Setting timer for " + formatMinutesToString(waitTime) + " from now.");
        }

        [Command("choose")]
        [Remarks("-c- choice \"choice but with spaces\"")]
        [Summary("Randomly makes a choice for you. You can use as many choices as you want, but seperate all choices using a space. If you wish for a choice to contain spaces, surround the choice with \"\"\n.")]
        public async Task Choose([Remainder]string message)
        {
            int quotes = message.Split('\"').Length - 1;
            if (quotes % 2 != 0)
            {
                await ReplyAsync("please ensure all quotations are closed");
                return;
            }

            List<string> choices = new List<string>();
            int count = 0;
            message = message.TrimEnd();
            while (true)
            {
                message = message.TrimStart();
                string choice;
                if (message == "")
                {
                    break;
                }
                if (message[0] == '\"')
                {
                    message = message.Remove(0, 1);
                    choice = message.Remove(message.IndexOf("\""));
                    message = message.Remove(0, message.IndexOf("\"") + 1);
                }
                else
                {
                    if (message.Contains(" "))
                    {
                        choice = message.Remove(message.IndexOf(" "));
                        message = message.Remove(0, message.IndexOf(" "));
                    }
                    else
                    {
                        choice = message;
                        message = "";
                    }
                }
                choices.Add(choice);
                count++;
            }

            if (count < 2)
            {
                await ReplyAsync("Please provide at least two choices.");
                return;
            }

            Random rnd = new Random();
            int num = rnd.Next(count);
            await ReplyAsync("The robot overlords have chosen: **" + choices[num] + "**");
        }

        [Command("youtube")]
        [Remarks("-c- video title")]
        [Summary("Takes whatever you give it and searches for it on YouTube, it will return the first search result that appears.")]
        public async Task Youtube([Remainder]string vid)
        {
            vid = "https://www.youtube.com/results?search_query=" + vid.Replace(" ", "+");
            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
            string html = await client.GetStringAsync(vid);
            html = html.Remove(0, html.IndexOf("?v=") + 3);
            html = html.Remove(html.IndexOf("\""));
            await ReplyAsync("https://www.youtube.com/watch?v=" + html);
        }

        [Command("8ball")]
        [Remarks("-c-")]
        [Summary("asks the magic 8 ball a question.")]
        public async Task eightBall([Remainder] string useless = "")
        {
            string[] responses = new string[] { "it is certain", "It is decidedly so", "Without a doubt", "Yes definitely", "You may rely on it", "As I see it, yes", "Most likely", "Outlook good", "Yes", "Signs point to yes", "Reply hazy try again", "Ask again later", "Better not tell you now", "Cannot predict now", "Concentrate and ask again", "Don't count on it", "My reply is no", "My sources say no", "Outlook not so good", "Very doubtful" };
            Random rnd = new Random();
            int num = rnd.Next(20);
            await ReplyAsync(responses[num]);
        }

        [Command("roll")]
        [Remarks("-c-, -c- 1d20, -c- 2d6a + 4")]
        [Alias("dice")]
        [Summary("Rolls a specified number of dice, with a specified number of sides, denoted as: [# rolls]d[# sides]. Dice can be rolled with advantage or disadvantage by adding a or d respectively following the dice. Multiple modifiers can be added by adding multiple \"+/- #\"s to the end. To quickly roll a 6 sided die, do not specify anything. Max values are 200 dice, 200 sides, and + 1000 modifiers")]
        public async Task Dice([Remainder] string message = "")
        {
            Match dice = Regex.Match(message, @"[0-9]+d[0-9]+ *(?:a|d)?( *(?:\+|-) *[0-9]+)*");
            if (!dice.Success && message != "")
            {
                BotBaseContext db = new BotBaseContext();
                var config = db.Configuration.AsQueryable().Where(cfg => cfg.Name == Program.configName).First();
                await ReplyAsync($"Invalid syntax, use {config.Prefix}help roll for more info");
                return;
            }

            Random rnd = new Random();
            int numDice = 1;
            int diceSides = 6;
            int modifier = 0;
            bool advantage = false;
            int numRolls = 1;
            string[] rolls = new string[] { "", "" };
            int[] totals = new int[] { 0, 0 };
            int printRoll = 0;
            string msg = "";

            if (message != "")
            {
                // Get values from Regex
                message = dice.Value;
                MatchCollection vals = Regex.Matches(message, @"[0-9]+");
                MatchCollection mods = Regex.Matches(message, @"( *(?:\+|-) *[0-9]+)");
                numDice = Convert.ToInt32(vals[0].Value);
                diceSides = Convert.ToInt32(vals[1].Value);
                foreach (Match mod in mods)
                {
                    string val = Regex.Match(mod.Value, @"[0-9]+").Value;
                    if(mod.Value.Contains(" + "))
                        modifier += Convert.ToInt32(val);
                    else
                        modifier -= Convert.ToInt32(val);
                }

                // advantage
                if (message.Contains("a"))
                {
                    advantage = true;
                    numRolls = 2;
                }

                // disadvantage
                if (message.Count(d => d == 'd') == 2)
                {
                    numRolls = 2;
                }
            }

            if (numDice == 0 || diceSides == 0)
            {
                await ReplyAsync("Cannot be 0");
                return;
            }

            if (numDice > 200 || diceSides > 200 || modifier > 1000 || modifier < -1000)
            {
                await Context.Channel.SendFileAsync("Media/joke.jpg");
                return;
            }

            for (int i = 0; i < numRolls; i++)
            {
                for (int j = 0; j < numDice; j++)
                {
                    int num = rnd.Next(diceSides) + 1;
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
                else if (!advantage && high == 0)
                    printRoll = 1;

                if (printRoll == 0)
                    msg = msg.Replace("1st Roll", "**1st Roll**");
                else
                    msg = msg.Replace("2nd Roll", "**2nd Roll**");
            }
            else
                msg = "Rolled: " + rolls[0];

            if (numDice == 1 && numRolls == 1 && modifier == 0)
                await ReplyAsync($"{totals[0]}");
            else if (modifier == 0 && numDice == 1)
                await ReplyAsync($"{msg}");
            else
                await ReplyAsync($"{msg}\nTotal: **{totals[printRoll]}**");

        }

        [Command("calculator")]
        [Remarks("-c- ( 5 + 7 ) / 2")]
        [Alias("calc", "math")]
        [Summary("Solves an arithmetic equation.")]
        public async Task Calculator([Remainder] string equation)
        {
            DataTable dt = new DataTable();
            var result = dt.Compute(equation,"");
            await ReplyAsync(result.ToString());
        }

        [Command("poll")]
        [Remarks("-c- Question | Option 1 | Option 2")]
        [Alias("strawpoll")]
        [Summary("Creates a strawpoll and returns the link.")]
        public async Task Poll([Remainder] string input)
        {
            if (input.Split("|").Length < 3)
            {
                await ReplyAsync("Please provide a question and at least two options in the format: Question | Option1 | Option2 etc.");
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
            await ReplyAsync("https://strawpoll.com/" + (string)json.SelectToken("content_id"));
        }

        [Command("avatar")]
        [Remarks("-c-, -c- @person1 @person2, -c- person1id person2id")]
        [Summary("Gets the avatar for one or more users. Mention a user or provide their id to get their avatar, or do neither to get your own. To do more than 1 person, separate mentions/ids with spaces.")]
        public async Task Avatar([Remainder] string ids = "")
        {
            await Context.Guild.DownloadUsersAsync();
            var mention = Context.Message.MentionedUserIds;
            if (mention.Count != 0)
            {
                foreach (ulong id in mention)
                {
                    var embed = new EmbedBuilder();
                    IGuildUser user = Context.Guild.GetUserAsync(id).Result;
                    string url = user.GetAvatarUrl();
                    url = url.Remove(url.IndexOf("?size=128"));
                    url = url + "?size=256";
                    embed.ImageUrl = url;
                    await ReplyAsync("", false, embed.Build());
                }
            }
            else if (ids != "")
            {
                string[] idList = ids.Split(' ');
                foreach (string id in idList)
                {
                    var embed = new EmbedBuilder();
                    IGuildUser user = await Context.Guild.GetUserAsync(Convert.ToUInt64(id));
                    string url = user.GetAvatarUrl();
                    url = url.Remove(url.IndexOf("?size=128"));
                    url = url + "?size=256";
                    embed.ImageUrl = url;
                    await ReplyAsync("", false, embed.Build());
                }
            }
            else
            {
                var embed = new EmbedBuilder();
                string url = Context.User.GetAvatarUrl();
                url = url.Remove(url.IndexOf("?size=128"));
                url = url + "?size=256";
                embed.ImageUrl = url;
                await ReplyAsync("", false, embed.Build());
            }
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
    class UrbanDictionaryDefinition
    {
        public string Definition { get; set; }
        public string Example { get; set; }
        public string Word { get; set; }
        public string Written_On { get; set; }
    }

    class UrbanDictionaryResult
    {
        public List<UrbanDictionaryDefinition> List { get; set; }
    }
}