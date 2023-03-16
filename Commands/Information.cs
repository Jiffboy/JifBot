using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Net;
using System.IO;
using Discord;
using Discord.Commands;
using Newtonsoft.Json.Linq;
using System.Web;
using JifBot.Models;
using JIfBot;

namespace JifBot.Commands
{
    public class Information : ModuleBase
    {
        [Command("commands")]
        [Remarks("-c-")]
        [Alias("help")]
        [Summary("Shows all available commands.")]
        public async Task Commands()
        {
            var db = new BotBaseContext();
            var commands = db.Command.AsQueryable().OrderBy(command => command.Category );
            var config = db.Configuration.AsQueryable().Where(cfg => cfg.Name == Program.configName).First();

            var embed = new JifBotEmbedBuilder();
            
            embed.Title = "All commands will begin with a " + config.Prefix + " , for more information on individual commands, use: " + config.Prefix + "help commandName";
            embed.Description = "Contact Jif#3952 with any suggestions for more commands. To see all command defintions together, visit https://jifbot.com/commands.html";

            string cat = commands.First().Category;
            string list = "";
            foreach(Command command in commands)
            {
                if(command.Category != cat)
                {
                    embed.AddField(cat, list.Remove(list.LastIndexOf(", ")));
                    cat = command.Category;
                    list = "";
                }
                list += command.Name + ", ";
            }
            embed.AddField(cat, list.Remove(list.LastIndexOf(", ")));
            await ReplyAsync("", false, embed.Build());
        }

        [Command("help")]
        [Remarks("-c- commandName")]
        [Summary("Used to get the descriptions of other commands.")]
        public async Task Help([Remainder] string commandName)
        {
            var db = new BotBaseContext();
            var command = db.Command.AsQueryable().Where(cmd => cmd.Name == commandName).FirstOrDefault();
            var config = db.Configuration.AsQueryable().Where(cfg => cfg.Name == Program.configName).First();
            if(command == null)
            {
                var cmdAlias = db.CommandAlias.AsQueryable().Where(cmd => cmd.Alias == commandName).FirstOrDefault();
                if(cmdAlias == null)
                {
                    await ReplyAsync($"{commandName} is not a command, make sure the spelling is correct.");
                    return;
                }
                command = db.Command.AsQueryable().Where(cmd => cmd.Name == cmdAlias.Command).First();
            }
            var alias = db.CommandAlias.AsQueryable().Where(als => als.Command == command.Name);
            string msg = $"{command.Description}\n**Usage**: {command.Usage}";
            if(alias.Any())
            {
                msg += "\nAlso works for: ";
                foreach(CommandParameter al in alias)
                    msg += $"{config.Prefix}{al.Alias} ";
            }
            await ReplyAsync(msg);
        }

        [Command("uptime")]
        [Remarks("-c-")]
        [Summary("Reports how long the bot has been running.")]
        public async Task Uptime()
        {
            TimeSpan uptime = DateTime.Now - Program.startTime;
            await ReplyAsync($"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s");
        }

        [Command("changelog")]
        [Remarks("-c-")]
        [Summary("Reports the last 3 updates made to Jif Bot.")]
        public async Task Changelog()
        {
            var db = new BotBaseContext();
            var embed = new JifBotEmbedBuilder();
            var totalEntries = db.ChangeLog.AsQueryable().OrderByDescending(e => e.Date);
            var entriesToPrint = new Dictionary<string, string>();

            foreach(ChangeLog entry in totalEntries)
            {
                if(!entriesToPrint.ContainsKey(entry.Date))
                {
                    if (entriesToPrint.Count >= 3)
                    {
                        break;
                    }
                    entriesToPrint.Add(entry.Date, $"\\> {entry.Change}");
                }
                else
                {
                    entriesToPrint[entry.Date] += $"\n\\> {entry.Change}";
                }
            }

            embed.Title = "Last 3 Jif Bot updates";
            embed.Description = "For a list of all updates, visit https://jifbot.com/changelog.html";
            foreach (var entry in entriesToPrint)
            {
                var pieces = entry.Key.Split("-");
                embed.AddField($"{pieces[1]}.{pieces[2]}.{pieces[0]}", entry.Value);
            }
            await ReplyAsync("", false, embed.Build());
        }

        [Command("invitelink")]
        [Alias("link")]
        [Remarks("-c-")]
        [Summary("Provides a link which can be used should you want to spread Jif Bot to another server.")]
        public async Task InviteLink()
        {
            var db = new BotBaseContext();
            var config = db.Configuration.AsQueryable().Where(cfg => cfg.Name == Program.configName).First();
            await ReplyAsync($"The following is a link to add me to another server. NOTE: You must have permissions on the server in order to add. Once on the server I must be given permission to send and delete messages, otherwise I will not work.\nhttps://discordapp.com/oauth2/authorize?client_id={config.Id}&scope=bot");
        }

        [Command("define")]
        [Remarks("-c- word")]
        [Summary("Defines any word in the dictionary.")]
        public async Task Define([Remainder] string word)
        {
            string DICTIONARY_ENDPOINT = "https://api.dictionaryapi.dev/api/v2/entries/en/" + word;
            List<DictionaryResult> definitionList = new List<DictionaryResult>();
            var embed = new JifBotEmbedBuilder();

            using (HttpClient client = new HttpClient())
            {
                using (var response = await client.GetAsync(DICTIONARY_ENDPOINT))
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        List<DictionaryResult> defineResult = JsonSerializer.Deserialize<List<DictionaryResult>>(jsonResponse);
                        List<String> variantsHit = new List<String>();
                        embed.Title = defineResult[0].word;
                        if (defineResult[0].phonetics.Count > 1)
                            embed.Description = "phonetically: " + defineResult[0].phonetics[1].text;
                        foreach (DictionaryMeaning meaning in defineResult[0].meanings)
                        {
                            // Some bizarre cases like plural words will cause for various types of the same part of speech, for simplicity
                            // we will ignore any duplicates
                            if (!variantsHit.Contains(meaning.partOfSpeech))
                            {
                                variantsHit.Add(meaning.partOfSpeech);
                                string definitions = "";
                                foreach (DictionaryDefinition definition in meaning.definitions)
                                {
                                    // Don't want this to spill over
                                    if ((definitions + " - " + definition.definition + "\n").Length < 1024)
                                    {
                                        definitions += " - " + definition.definition + "\n";
                                    }
                                }
                                embed.AddField(meaning.partOfSpeech, definitions);
                            }
                        }

                        await ReplyAsync("", false, embed.Build());
                        if(defineResult[0].phonetics.Count > 1 && defineResult[0].phonetics[1].audio != "")
                        {
                            byte[] soundData = null;
                            using (var wc = new System.Net.WebClient())
                                soundData = wc.DownloadData(defineResult[0].phonetics[1].audio);
                            Stream stream = new MemoryStream(soundData);
                            await Context.Channel.SendFileAsync(stream, embed.Title + ".mp3");
                        }
                    }
                    else if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        await ReplyAsync(word + " is not an existing word, or is, or relates to a proper noun.");
                    }
                    else
                    {
                        await ReplyAsync("Something has gone wrong, please try again later.");
                    }
                }
            }
        }

        [Command("udefine")]
        [Remarks("-c- term")]
        [Alias("slang")]
        [Summary("Gives the top definition for the term from urbandictionary.com.")]
        public async Task DefineUrbanDictionary([Remainder] string phrase)
        {
            string URBAN_DICTIONARY_ENDPOINT = "http://api.urbandictionary.com/v0/define?term=";

            string encodedSearchTerm = HttpUtility.UrlEncode(phrase);
            List<UrbanDictionaryDefinition> definitionList = new List<UrbanDictionaryDefinition>();
            var embed = new JifBotEmbedBuilder();

            using (HttpClient client = new HttpClient())
            {
                using (var response = await client.GetAsync(URBAN_DICTIONARY_ENDPOINT + encodedSearchTerm))
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    UrbanDictionaryResult udefineResult = JsonSerializer.Deserialize<UrbanDictionaryResult>(jsonResponse);
                    definitionList = udefineResult.list;
                }
            }

            if (definitionList.Count > 0)
            {
                // Urban Dictionary uses square brackets for links in its markup; they'll never appear as part of the definition text.
                var cleanDefinition = definitionList[0].definition.Replace("[", "").Replace("]", "");
                var cleanExample = definitionList[0].example.Replace("[", "").Replace("]", "");
                var year = definitionList[0].written_on.Substring(0, definitionList[0].written_on.IndexOf("-"));
                var dayMonth = definitionList[0].written_on.Substring(definitionList[0].written_on.IndexOf("-") + 1, 5);
                var cleanDate = dayMonth.Replace("-", "/") + "/" + year;
                var word = definitionList[0].word;

                embed.Title = word;
                embed.Description = $"Written: {cleanDate}";
                embed.Url = definitionList[0].permalink;

                if(cleanDefinition.Length >= 1024)
                    cleanDefinition = cleanDefinition.Substring(0, 1021) + "...";

                if (cleanExample.Length >= 1024)
                    cleanExample = cleanExample.Substring(0, 1021) + "...";

                embed.AddField("Definition", cleanDefinition);
                embed.AddField("Example", cleanExample);

                await ReplyAsync("", false, embed.Build());
            }
            else
            {
                await ReplyAsync($"{phrase} is not an existing word/phrase");
            }
        }

        [Command("movie")]
        [Alias("imdb")]
        [Remarks("-c- airplane!")]
        [Summary("Provides information for a movie as specified by name.")]
        public async Task Movie([Remainder] string word)
        {
            var db = new BotBaseContext();
            var omdbKey = db.Variable.AsQueryable().Where(v => v.Name == "omdbKey").First();

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("http://www.omdbapi.com");
            HttpResponseMessage response = await client.GetAsync($"?t={word}&plot=full&apikey={omdbKey.Value}");
            HttpContent content = response.Content;
            string stuff = await content.ReadAsStringAsync();
            var json = JObject.Parse(stuff);
            if ((string)json.SelectToken("Response") == "False")
            {
                await ReplyAsync("Movie not found");
                return;
            }
            var embed = new JifBotEmbedBuilder();
            string rt = (string)json.SelectToken("Ratings[1].Value");
            string imdb = (string)json.SelectToken("Ratings[0].Value");
            string plot = (string)json.SelectToken("Plot");
            if (plot.Length > 1024)
            {
                int excess = plot.Length - 1024;
                plot = plot.Remove(plot.Length - excess - 3);
                plot += "...";
            }

            embed.Title = (string)json.SelectToken("Title");
            embed.Description = (string)json.SelectToken("Genre");
            if ((string)json.SelectToken("Poster") != "N/A")
                embed.ThumbnailUrl = (string)json.SelectToken("Poster");
            if (rt != null)
                embed.AddField($"Rotten Tomatoes: {rt}, IMDb: {imdb}", plot);
            else
                embed.AddField($"IMDb Rating: {imdb}", plot);
            embed.AddField("Released", (string)json.SelectToken("Released"), inline: true);
            embed.AddField("Run Time", (string)json.SelectToken("Runtime"), inline: true);
            embed.AddField("Rating", (string)json.SelectToken("Rated"), inline: true);
            embed.AddField("Starring", (string)json.SelectToken("Actors"));
            embed.AddField("Directed By", (string)json.SelectToken("Director"), inline: true);
            embed.WithUrl("https://www.imdb.com/title/" + (string)json.SelectToken("imdbID"));
            await ReplyAsync("", false, embed.Build());
        }

        [Command("stats")]
        [Remarks("-c- region username")]
        [Summary("Gives the stats for a league player on any region. The region name is the abbreviated verson of the region name. Example: na = North America.")]
        public async Task Stats(string region, [Remainder] string name)
        {
            name = name.Replace(" ", string.Empty);

            string SearchText = "<meta name=\"description\" content=\"";
            string SearchText2 = "\"/>";

            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
            string source = "";
            if (region == "kr")
            {
                if (await RemoteFileExists("http://www.op.gg/summoner/userName=" + name))
                    source = await client.GetStringAsync("http://www.op.gg/summoner/userName=" + name);
                else
                {
                    await ReplyAsync("That is not a valid summoner name / region");
                    return;
                }
            }
            else
            {
                if (await RemoteFileExists("http://" + region + ".op.gg/summoner/userName=" + name))
                    source = await client.GetStringAsync("http://" + region + ".op.gg/summoner/userName=" + name);
                else
                {
                    await ReplyAsync("That is not a valid summoner name / region");
                    return;
                }
            }
            if (source.IndexOf("This summoner is not registered at OP.GG. Please check spelling.") != -1)
            {
                await ReplyAsync("That Summoner does not exist");
                return;
            }
            else
            {
                var db = new BotBaseContext();
                var embed = new JifBotEmbedBuilder();
                
                string kdsource = source.Remove(0, source.IndexOf("summoner-id=\"") + 13);
                kdsource = kdsource.Remove(kdsource.IndexOf("\""));
                if (region == "kr")
                    kdsource = "http://www." + "op.gg/summoner/champions/ajax/champions.most/summonerId=" + kdsource + "&season=11";
                else
                    kdsource = "http://" + region + ".op.gg/summoner/champions/ajax/champions.most/summonerId=" + kdsource + "&season=11";
                System.Net.Http.HttpClient client2 = new System.Net.Http.HttpClient();
                kdsource = await client2.GetStringAsync(kdsource);
                string url = source.Remove(0, source.IndexOf("ProfileIcon"));
                url = url.Remove(0, url.IndexOf("<img src=\"//") + 12);
                url = url.Remove(url.IndexOf("\""));
                url = "http://" + url;
                embed.ThumbnailUrl = url;
                Int32 start = source.IndexOf(SearchText) + SearchText.Length;
                source = source.Remove(0, start);
                Int32 end = source.IndexOf(SearchText2);
                source = source.Remove(end);

                source = source.Replace("&#039;", "'");
                if (source.IndexOf("Lv. ") == -1 && source.IndexOf("Unranked") == -1)
                {
                    string def = "Information for: " + source.Remove(source.IndexOf("/")) + "\n";
                    source = source.Remove(0, source.IndexOf("/") + 1);
                    embed.Title = def;
                    def = "Current Ranking: " + source.Remove(source.IndexOf("/")) + "\n";
                    source = source.Remove(0, source.IndexOf("/") + 1);
                    def = def + "Win Record: " + source.Remove(source.IndexOf("Win")) + "  (";
                    source = source.Remove(0, source.IndexOf("o") + 1);
                    def = def + source.Remove(source.IndexOf("/")) + ")\n\nTop 5 Champions:\n";
                    source = source.Remove(0, source.IndexOf("/") + 1);
                    embed.Description = def;
                    for (int i = 0; i < 4; i++)
                    {
                        if (source.IndexOf(",") != -1)
                        {
                            def = source.Remove(source.IndexOf("Win")) + "(";
                            source = source.Remove(0, source.IndexOf("Win") + 9);
                            def = def + source.Remove(source.IndexOf(",")) + " )";
                            def = def.Remove(def.IndexOf("-")) + def.Remove(0, def.IndexOf("-")).PadRight(30, ' ');
                            source = source.Remove(0, source.IndexOf(",") + 1);
                            kdsource = kdsource.Remove(0, kdsource.IndexOf("span class=\"KDA") + 17);
                            def = def + "KDA: **" + kdsource.Remove(kdsource.IndexOf(":")) + "**     ( ";
                            kdsource = kdsource.Remove(0, kdsource.IndexOf("KDAEach"));
                            kdsource = kdsource.Remove(0, kdsource.IndexOf("Kill") + 6);
                            def = def + kdsource.Remove(kdsource.IndexOf("<"));
                            kdsource = kdsource.Remove(0, kdsource.IndexOf("Death") + 7);
                            def = def + " / " + kdsource.Remove(kdsource.IndexOf("<"));
                            kdsource = kdsource.Remove(0, kdsource.IndexOf("Assist") + 8);
                            def = def + " / " + kdsource.Remove(kdsource.IndexOf("<")) + " )";
                            embed.AddField(def.Remove(def.IndexOf("-")), def.Remove(0, def.IndexOf("-") + 1));

                        }
                    }
                    def = source.Remove(source.IndexOf("Win")) + "  (";
                    source = source.Remove(0, source.IndexOf("Win") + 9);
                    def = def + source + " )";
                    def = def.Remove(def.IndexOf("-")) + def.Remove(0, def.IndexOf("-")).PadRight(30, ' ');
                    kdsource = kdsource.Remove(0, kdsource.IndexOf("span class=\"KDA") + 17);
                    def = def + "KDA: **" + kdsource.Remove(kdsource.IndexOf(":")) + "**     ( ";
                    kdsource = kdsource.Remove(0, kdsource.IndexOf("KDAEach"));
                    kdsource = kdsource.Remove(0, kdsource.IndexOf("Kill") + 6);
                    def = def + kdsource.Remove(kdsource.IndexOf("<"));
                    kdsource = kdsource.Remove(0, kdsource.IndexOf("Death") + 7);
                    def = def + " / " + kdsource.Remove(kdsource.IndexOf("<"));
                    kdsource = kdsource.Remove(0, kdsource.IndexOf("Assist") + 8);
                    def = def + " / " + kdsource.Remove(kdsource.IndexOf("<")) + " )";
                    embed.AddField(def.Remove(def.IndexOf("-")), def.Remove(0, def.IndexOf("-") + 1));
                }
                else
                {
                    await ReplyAsync("That Summoner has not been placed yet this season");
                    return;
                }
                
            await ReplyAsync("", false, embed.Build());
            }
        }

        [Command("mastery")]
        [Remarks("-c- region username")]
        [Summary("Gives the number of mastery points for the top 10 most played champions for a user on any server.")]
        public async Task Mastery(string region, [Remainder] string name)
        {
            var db = new BotBaseContext();
            var embed = new JifBotEmbedBuilder();
            {
                name = name.Replace(" ", string.Empty);
                System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
                string html = "";
                try
                {
                    html = await client.GetStringAsync("https://championmasterylookup.derpthemeus.com/summoner?summoner=" + name + "&region=" + region.ToUpper());
                }
                catch
                {
                    await ReplyAsync("That summoner does not exist");
                    return;
                }
                html = html.Remove(0, html.IndexOf("/img/profile"));
                embed.ThumbnailUrl = "https://championmasterylookup.derpthemeus.com" + html.Remove(html.IndexOf("\""));
                html = html.Remove(0, html.IndexOf("userName=") + 9);
                embed.Title = "Top ten mastery scores for " + (html.Remove(html.IndexOf("\"")).Replace("%20", " "));
                string champ = "";
                string nums = "";
                int count = 0;
                for (int i = 1; i <= 10; i++)
                {
                    if (html.IndexOf("/champion?") == html.IndexOf("/champion?champion=-1"))
                        break;
                    html = html.Remove(0, html.IndexOf("/champion?"));
                    html = html.Remove(0, html.IndexOf(">") + 1);
                    champ = html.Remove(html.IndexOf("<"));
                    champ = champ.Replace("&#x27;", "'");
                    html = html.Remove(0, html.IndexOf("\"") + 1);
                    nums = html.Remove(html.IndexOf("\""));
                    count = count + Convert.ToInt32(nums);
                    for (int j = nums.Length - 3; j > 0; j = j - 3)
                        nums = nums.Remove(j) + "," + nums.Remove(0, j);

                    embed.AddField(i + ". " + champ, nums + " points", inline: true);
                }

                nums = Convert.ToString(count);
                for (int j = nums.Length - 3; j > 0; j = j - 3)
                    nums = nums.Remove(j) + "," + nums.Remove(0, j);
                embed.Description = "Total score across top ten: " + nums;

                await ReplyAsync("", false, embed.Build());
            }
        }

        [Command("info")]
        [Remarks("-c-, -c- @person1 @person2, -c- person1id person2id")]
        [Summary("Gets varying pieces of Discord information for one or more users. Mention a user or provide their id to get their information, or do neither to get your own. To do more than 1 person, separate mentions/ids with spaces.")]
        public async Task MyInfo([Remainder] string ids = "")
        {
            await Context.Guild.DownloadUsersAsync();
            var mention = Context.Message.MentionedUserIds;
            if (mention.Count != 0)
            {
                foreach (ulong id in mention)
                {
                    var embed = ConstructEmbedInfo(Context.Guild.GetUserAsync(id).Result);
                    await ReplyAsync("", false, embed.Build());
                }
            }
            else if (ids != "")
            {
                string[] idList = ids.Split(' ');
                foreach (string id in idList)
                {
                    var embed = ConstructEmbedInfo(await Context.Guild.GetUserAsync(Convert.ToUInt64(id)));
                    await ReplyAsync("", false, embed.Build());
                }
            }
            else
            {
                var embed = ConstructEmbedInfo(await Context.Guild.GetUserAsync(Context.User.Id));
                await ReplyAsync("", false, embed.Build());
            }
        }

        async Task<bool> RemoteFileExists(string url)
        {
            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
            try
            {
                string response = await client.GetStringAsync(url);
                if (response.Length == 0) return false;
                else
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        public string FormatTime(DateTimeOffset orig)
        {
            string str = "";
            str = str + orig.LocalDateTime.DayOfWeek + ", ";
            str = str + orig.LocalDateTime.Month + "/" + orig.LocalDateTime.Day + "/" + orig.LocalDateTime.Year;
            str = str + " at " + orig.LocalDateTime.Hour + ":" + orig.LocalDateTime.Minute + " CST";
            return str;
        }

        public EmbedBuilder ConstructEmbedInfo(IGuildUser user)
        {
            var db = new BotBaseContext();
            var embed = new JifBotEmbedBuilder();
            embed.WithAuthor(user.Username + "#" + user.Discriminator, user.GetAvatarUrl());
            embed.ThumbnailUrl = user.GetAvatarUrl();
            embed.AddField("User ID", user.Id);
            if (user.Nickname == null)
                embed.AddField("Nickname", user.Username);
            else
                embed.AddField("Nickname", user.Nickname);
            /*if (user.Activity == null)
                embed.AddField("Currently Playing", "[nothing]");
            else
                embed.AddField("Currently " + user.Activity.Type.ToString(), user.Activity.Name);*/
            embed.AddField("Account Creation Date", FormatTime(user.CreatedAt));
            embed.AddField("Server Join Date", FormatTime(user.JoinedAt.Value));
            string roles = "";
            foreach (ulong id in user.RoleIds)
            {
                if (roles != "")
                    roles = roles + ", ";
                if (Context.Guild.GetRole(id).Name != "@everyone")
                    roles = roles + Context.Guild.GetRole(id).Name;
            }
            embed.AddField("Roles", roles);
            return embed;
        }
    }

    class UrbanDictionaryDefinition
    {
        public string definition { get; set; }
        public string example { get; set; }
        public string word { get; set; }
        public string written_on { get; set; }
        public string permalink { get; set; }
    }

    class UrbanDictionaryResult
    {
        public List<UrbanDictionaryDefinition> list { get; set; }
    }

    class DictionaryResult
    {
        public string word { get; set; }
        public List<DictionaryMeaning> meanings { get; set; }
        public List<DictionaryPhonetic> phonetics { get; set; }
    }

    class DictionaryPhonetic
    {
        public string text { get; set; }
        public string audio { get; set; }
    }

    class DictionaryMeaning
    {
        public string partOfSpeech { get; set; }
        public List<DictionaryDefinition> definitions { get; set; }
    }
    class DictionaryDefinition
    {
        public string definition { get; set; }
    }
}
