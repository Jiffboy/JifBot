using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Net.Http;
using Discord;
using Discord.Interactions;
using JifBot.Models;
using System.Data;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace JifBot.Commands
{
    public class Utility : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("timer", "Sets a reminder to ping you after a certain amount of time has passed. Use /managetimers to cancel.")]
        public async Task Timer(
            [Summary("message", "The message to ping you with after the time runs out")] string message="",
            [Summary("date-time", "The date and time to ping. Formatted as: mm/dd/yyyy hh:mm in military eastern time")] string datetime="",
            [Summary("minutes","The number of minutes to wait.")] int minutes=0,
            [Summary("hours", "The number of hours to wait.")] int hours=0,
            [Summary("days", "The number of days to wait.")]int days=0,
            [Summary("weeks", "The number of weeks to wait.")] int weeks=0,
            [Summary("cadence-hours", "The cadence this timer should repeat in hours. Leave blank if no repeat.")] int cadenceHours = 0,
            [Summary("cadence-days", "The cadence this timer should repeat in days. Leave blank if no repeat.")] int cadenceDays = 0)

        {
            long timestamp;
            int cadence = ((cadenceHours * 60) + (cadenceDays * 1440)) * 60;
            DateTimeOffset dto = DateTimeOffset.Now.AddMinutes(minutes).AddHours(hours).AddDays(days + weeks*7);

            if (datetime != "")
            {
                DateTime dt;
                if (DateTime.TryParseExact(datetime, "MM/dd/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                {
                    if (dt < DateTime.Now)
                    {
                        await RespondAsync("Please provide a time in the future.", ephemeral: true);
                        return;
                    }

                    dto = new DateTimeOffset(dt);
                }
                else
                {
                    await RespondAsync("Invalid date-time format. Please format as: mm/dd/yyyy hh:mm", ephemeral: true);
                    return;
                }
            }
            else if ((minutes + hours + days + weeks) == 0)
            {
                await RespondAsync($"Please provide an amount of time to wait for.", ephemeral: true);
                return;
            }

            if (message.Replace(" ", "") == "")
                message = "Times up!";

            var db = new BotBaseContext();
            db.Add(new Models.Timer { 
                UserId = Context.User.Id,
                ChannelId = Context.Channel.Id,
                Message = message,
                Timestamp = dto.ToUnixTimeSeconds(),
                Cadence = cadence
            });
            db.SaveChanges();

            var msg = $"Setting timer for:\n**<t:{dto.ToUnixTimeSeconds()}:f>**";

            if (cadence > 0)
            {
                msg += "\n\nThis timer will repeat every ";
                msg += cadenceDays > 0 && cadenceHours > 0 ? $"**{cadenceDays} days, {cadenceHours} hours**." : cadenceDays > 0 ? $"**{cadenceDays} days**.": $"**{cadenceHours} hours**.";
                msg += "\nTo cancel this, use /managetimers.";
            }

            await RespondAsync(msg);
        }

        [SlashCommand("managetimers", "Manages existing timers made with the /timer command.")]
        public async Task manageTimers(
            [Choice("List", "l")]
            [Choice("Remove", "r")]
            [Summary("option", "The action to take.")] string option,
            [Summary("id", "The id of the timer to manage. To see Id's, select the List option..")] ulong id = 0)
        {
            var db = new BotBaseContext();
            if (option == "l")
            {
                var timers = db.Timer.AsQueryable().Where(t => t.UserId == Context.User.Id).ToList();

                if (!timers.Any())
                {
                    await RespondAsync("You have no current timers!", ephemeral: true);
                    return;
                }

                var msg = "";
                foreach (var timer in timers)
                {
                    msg += $"**#{timer.Id}.**   <t:{timer.Timestamp}:f>\n> `{timer.Message}`\n\n";
                }

                await RespondAsync(msg);
            }
            else if (option == "r")
            {
                var timer = db.Timer.AsQueryable().Where(timer => timer.Id == id).FirstOrDefault();
                if (timer != null && timer.UserId == Context.User.Id)
                {
                    db.Timer.Remove(timer);
                    db.SaveChanges();
                    await RespondAsync("Successfully deleted timer.");
                }
            }
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

        [SlashCommand("sillytext", "Takes specified text and makes it silly.")]
        public async Task SillyText(
           [Choice("mock", "mock")]
            [Choice("owo", "owo")]
            [Choice("big", "big")]
            [Choice("tiny", "tiny")]
            [Choice("wide", "wide")]
            [Summary("style", "How to modify the text.")] string style,
           [Summary("text", "The text to be modified.")] string text)
        {
            switch (style)
            {
                case "mock":
                    await RespondAsync(MockText(text));
                    break;
                case "owo":
                    await RespondAsync(OwoText(text));
                    break;
                case "big":
                    await RespondAsync(BigText(text));
                    break;
                case "tiny":
                    await RespondAsync(TinyText(text));
                    break;
                case "wide":
                    await RespondAsync(WideText(text));
                    break;
            }
        }

        private string MockText(string text)
        {
            string end = string.Empty;
            int i = 0;
            foreach (char c in text)
            {
                if (c == ' ' || c == '"' || c == '.' || c == ',')
                    end += c;
                else if (i == 0)
                {
                    char temp = Char.ToLower(c);
                    end += temp;
                    i = 1;
                }
                else if (i == 1)
                {
                    char temp = Char.ToUpper(c);
                    end += temp;
                    i = 0;
                }
            }
            return end;
        }

        private string BigText(string text)
        {
            string[] alpha = { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z" };
            string[] big = { "🇦 ", "🅱 ", "🇨 ", "🇩 ", "🇪 ", "🇫 ", "🇬 ", "🇭 ", "🇮 ", "🇯 ", "🇰 ", "🇱 ", "🇲 ", "🇳 ", "🇴 ", "🇵 ", "🇶 ", "🇷 ", "🇸 ", "🇹 ", "🇺 ", "🇻 ", "🇼 ", "🇽 ", "🇾 ", "🇿 " };
            text = text.ToLower();
            text = text.Replace(" ", "  ");
            for (int i = 0; i < alpha.Length; i++)
            {
                text = text.Replace(alpha[i], big[i]);
            }
            return text;
        }

        private string TinyText(string text)
        {
            string alpha = "abcdefghijklmnopqrstuvwxyz";
            string small = "ᵃᵇᶜᵈᵉᶠᵍʰᶦʲᵏᶫᵐᶰᵒᵖᑫʳˢᵗᵘᵛʷˣʸᶻ";
            text = text.ToLower();

            for (int i = 0; i < alpha.Length; i++)
            {
                text = text.Replace(alpha[i], small[i]);
            }
            return text;
        }

        private string WideText(string text)
        {
            text = text.Replace(" ", "   ");
            string alpha = "QWERTYUIOPASDFGHJKLÇZXCVBNMqwertyuiopasdfghjklçzxcvbnm,.-~+´«'0987654321!\"#$%&/()=?»*`^_:;";
            string fullwidth = "ＱＷＥＲＴＹＵＩＯＰＡＳＤＦＧＨＪＫＬÇＺＸＣＶＢＮＭｑｗｅｒｔｙｕｉｏｐａｓｄｆｇｈｊｋｌçｚｘｃｖｂｎｍ,.－~ ´«＇０９８７６５４３２１！＂＃＄％＆／（）＝？»＊`＾＿：；";

            for (int i = 0; i < alpha.Length; i++)
            {
                text = text.Replace(alpha[i], fullwidth[i]);
            }
            return text;
        }

        private string OwoText(string text)
        {
            string[] faces = new string[] { "(・ω・)", ";;w;;", "owo", "UwU", ">w<", "^w^" };
            Random rnd = new Random();
            text = Regex.Replace(text, @"(?:r|l)", "w");
            text = Regex.Replace(text, @"(?:R|L)", "W");
            text = Regex.Replace(text, @"n([aeiou])", @"ny$1");
            text = Regex.Replace(text, @"N([aeiou])", @"Ny$1");
            text = Regex.Replace(text, @"N([AEIOU])", @"NY$1");
            text = Regex.Replace(text, @"ove", @"uv");
            if (!text.EndsWith("!"))
            {
                text += " " + faces[rnd.Next(faces.Length)];
            }
            text = Regex.Replace(text, @"\!+", (match) => string.Format("{0}", " " + faces[rnd.Next(faces.Length)] + " "));

            return text;
        }

        public async Task<IGuildUser> getUser(IGuild guild, ulong id)
        {
            await guild.DownloadUsersAsync();
            RequestOptions request = new RequestOptions();
            CancellationToken cancel = request.CancelToken;
            IGuildUser user = await guild.GetUserAsync(id, mode: CacheMode.CacheOnly, options: request);

            return user;
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