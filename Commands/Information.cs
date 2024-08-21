using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using Discord;
using Discord.Interactions;
using Newtonsoft.Json.Linq;
using System.Web;
using JifBot.Models;
using JIfBot;

namespace JifBot.Commands
{
    enum LeagueQueue: int
    {
        Custom = 0,
        Draft = 400,
        Ranked = 420,
        Blind = 430,
        Flex = 440,
        ARAM = 450,
        Quickplay = 490,
        Clash = 700,
        Arena = 1700
    }

    public class Information : InteractionModuleBase<SocketInteractionContext>
    {
        private bool leagueUpToDate = false;

        [SlashCommand("commands", "Shows all available commands.")]
        public async Task Commands()
        {
            var db = new BotBaseContext();
            var commands = db.Command.AsQueryable().OrderBy(command => command.Category );
            var config = db.Configuration.AsQueryable().Where(cfg => cfg.Name == Program.configName).First();

            var embed = new JifBotEmbedBuilder();
            
            embed.Title = "For more information on individual commands, use: /help";
            embed.Description = "Contact Jif#3952 with any suggestions for more commands. To see all command defintions together, visit https://jifbot.com/commands";

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
            await RespondAsync(embed: embed.Build());
        }

        [SlashCommand("help", "Gets information for a specified Jif Bot command.")]
        public async Task Help(
            [Summary("command","The command you would like help with")] string commandName)
        {
            var db = new BotBaseContext();
            var command = db.Command.AsQueryable().Where(cmd => cmd.Name == commandName).FirstOrDefault();
            var config = db.Configuration.AsQueryable().Where(cfg => cfg.Name == Program.configName).First();
            if(command == null)
            {
                await RespondAsync($"{commandName} is not a command, make sure the spelling is correct.", ephemeral: true);
                return;
            }
            var parameters = db.CommandParameter.AsQueryable().Where(p => p.Command == command.Name);
            string msg = $"{command.Description}";
            if(parameters.Any())
            {
                msg += "\n\n**Parameters:**\n";
                foreach (CommandParameter parameter in parameters)
                {
                    msg += $"{(parameter.Required ? "[Required]" : "[Optional]")} **{parameter.Name}**: {parameter.Description}\n";
                    var choices = db.CommandParameterChoice.AsQueryable().Where(p => p.Command == command.Name && p.Parameter == parameter.Name);
                    if(choices.Any())
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
            [Summary("count", "The number of changes to pull. Defaults to 3, max of 20.")] int count=3)
        {
            count = count > 20 ? 20 : count;
            var db = new BotBaseContext();
            var embed = new JifBotEmbedBuilder();
            var totalEntries = db.ChangeLog.AsQueryable().OrderByDescending(e => e.Date);
            var entriesToPrint = new Dictionary<string, string>();

            foreach(ChangeLog entry in totalEntries)
            {
                if(!entriesToPrint.ContainsKey(entry.Date))
                {
                    if (entriesToPrint.Count >= count)
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

            embed.Title = $"Last {count} Jif Bot updates";
            embed.Description = "For a list of all updates, visit https://jifbot.com/changelog";
            foreach (var entry in entriesToPrint)
            {
                var pieces = entry.Key.Split("-");
                embed.AddField($"{pieces[1]}.{pieces[2]}.{pieces[0]}", entry.Value);
            }
            await RespondAsync(embed: embed.Build());
        }

        [SlashCommand("invitelink", "Provides a link which can be used should you want to spread Jif Bot to another server.")]
        public async Task InviteLink()
        {
            var db = new BotBaseContext();
            var config = db.Configuration.AsQueryable().Where(cfg => cfg.Name == Program.configName).First();
            await RespondAsync($"The following is a link to add me to another server. NOTE: You must have permissions on the server in order to add. Once on the server I must be given permission to send and delete messages, otherwise I will not work.\nhttps://discordapp.com/oauth2/authorize?client_id={config.Id}&scope=bot");
        }

        [SlashCommand("define", "Defines any word in the dictionary.")]
        public async Task Define(
            [Summary("word", "The word you would like the definition for.")] string word)
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

                        await RespondAsync(embed: embed.Build());
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
                        await RespondAsync(word + " is not an existing word, or is, or relates to a proper noun.");
                    }
                    else
                    {
                        await RespondAsync("Something has gone wrong, please try again later.");
                    }
                }
            }
        }

        [SlashCommand("udefine", "Gives the top definition for a specified phrase from urbandictionary.com.")]
        public async Task DefineUrbanDictionary(
            [Summary("phrase", "The disgusting thing you're trying to look up.")] string phrase)
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
                UrbanDictionaryDefinition currDefinition = definitionList[0];
                int currVote = 0;

                foreach (UrbanDictionaryDefinition definition in definitionList)
                {
                    if (definition.thumbs_up > currVote)
                    {
                        currDefinition = definition;
                        currVote = definition.thumbs_up;
                    }
                }

                // Urban Dictionary uses square brackets for links in its markup; they'll never appear as part of the definition text.
                var cleanDefinition = currDefinition.definition.Replace("[", "").Replace("]", "");
                var cleanExample = currDefinition.example.Replace("[", "").Replace("]", "");
                var year = currDefinition.written_on.Substring(0, definitionList[0].written_on.IndexOf("-"));
                var dayMonth = currDefinition.written_on.Substring(definitionList[0].written_on.IndexOf("-") + 1, 5);
                var cleanDate = dayMonth.Replace("-", "/") + "/" + year;
                var word = currDefinition.word;

                embed.Title = word;
                embed.Description = $"Written: {cleanDate}\n⬆️ {currDefinition.thumbs_up:n0}   ⬇️ {currDefinition.thumbs_down:n0}";
                embed.Url = definitionList[0].permalink;

                if(cleanDefinition.Length >= 1024)
                    cleanDefinition = cleanDefinition.Substring(0, 1021) + "...";

                if (cleanExample.Length >= 1024)
                    cleanExample = cleanExample.Substring(0, 1021) + "...";

                embed.AddField("Definition", cleanDefinition);
                embed.AddField("Example", cleanExample);

                await RespondAsync(embed: embed.Build());
            }
            else
            {
                await RespondAsync($"{phrase} is not an existing word/phrase");
            }
        }

        [SlashCommand("movie", "Provides information for a movie as specified by name.")]
        public async Task Movie(
            [Summary("movie", "The name of the movie you'd like information on.")] string word)
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
                await RespondAsync("Movie not found");
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
            await RespondAsync(embed: embed.Build());
        }

        [SlashCommand("mastery", "Gives the total mastery points for the top 10 most played champions for a League of Legends player.")]
        public async Task Mastery(
            [Choice("BR1", "br1")]
            [Choice("EUN1", "eun1")]
            [Choice("EUW1", "euw1")]
            [Choice("JP1", "jp1")]
            [Choice("KR", "kr")]
            [Choice("LA1", "la1")]
            [Choice("LA2", "la2")]
            [Choice("NA1", "na1")]
            [Choice("OC1", "oc1")]
            [Choice("TR1", "tr1")]
            [Choice("RU", "ru")]
            [Choice("PH2", "ph2")]
            [Choice("SG2", "sg2")]
            [Choice("TH2", "th2")]
            [Choice("TW2", "tw2")]
            [Choice("VN2", "vn2")]
            [Summary("region", "The abbreviated name of the region the account is on.")] string platform,
            [Summary("name", "The display name of the player.")] string name,
            [Summary("tag", "The identifying tag of the account. Example: NA1")] string tag)
        {
            var db = new BotBaseContext();
            var key = db.Variable.AsQueryable().Where(v => v.Name == "leagueKey").First();
            var region = GetRegionFromPlatform(platform);
            var embed = new JifBotEmbedBuilder();

            string puuid = await GetPUUID(name, tag, region, key.Value);
            Summoner summoner = await GetSummoner(platform, puuid, key.Value);
            using (HttpClient client = new HttpClient())
            {
                using (var response = await client.GetAsync($"https://{platform}.api.riotgames.com/lol/champion-mastery/v4/champion-masteries/by-puuid/{puuid}?api_key={key.Value}"))
                {
                    embed.Title = $"Top ten mastery scores for {name}#{tag}";
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    List<Mastery> masteryResult = JsonSerializer.Deserialize<List<Mastery>>(jsonResponse);
                    int i = 1;
                    long totalMastery = 0;
                    foreach(Mastery mastery in masteryResult)
                    {
                        string champion = await GetChampionById(mastery.championId.ToString());
                        if ( i == 1 )
                        {
                            embed.ThumbnailUrl = $"https://ddragon.leagueoflegends.com/cdn/{Program.currLeagueVersion}/img/champion/{champion}.png";
                        }

                        if (i <= 10)
                        {
                            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                            dateTime = dateTime.AddSeconds(mastery.lastPlayTime / 1000);
                            string date = dateTime.ToLocalTime().ToShortDateString();
                            string descText = "";
                            descText += $"Level: {mastery.championLevel}";
                            descText += $"\n{mastery.championPoints:n0} points";
                            descText += $"\nPlayed: {date}";
                            embed.AddField($"{i}. {champion}", descText, inline: true);
                        }

                        totalMastery += mastery.championPoints;
                        i++;
                    }
                    embed.Description = $"Summoner Level: {summoner.summonerLevel}\nTotal Mastery Points: {totalMastery:n0}";
                }
            }
                
            await RespondAsync(embed: embed.Build());
        }

        [SlashCommand("match", "Returns info for the most recent matches played in league of legends.")]
        public async Task Match(
            [Choice("BR1", "br1")]
            [Choice("EUN1", "eun1")]
            [Choice("EUW1", "euw1")]
            [Choice("JP1", "jp1")]
            [Choice("KR", "kr")]
            [Choice("LA1", "la1")]
            [Choice("LA2", "la2")]
            [Choice("NA1", "na1")]
            [Choice("OC1", "oc1")]
            [Choice("TR1", "tr1")]
            [Choice("RU", "ru")]
            [Choice("PH2", "ph2")]
            [Choice("SG2", "sg2")]
            [Choice("TH2", "th2")]
            [Choice("TW2", "tw2")]
            [Choice("VN2", "vn2")]
            [Summary("region", "The abbreviated name of the region the account is on.")] string platform,
            [Summary("name", "The display name of the player.")] string name,
            [Summary("tag", "The identifying tag of the account. Example: NA1")] string tag,
            [Choice("Blind Pick", "blind")]
            [Choice("Draft Pick", "draft")]
            [Choice("Ranked Solo", "ranked")]
            [Choice("Ranked Flex", "flex")]
            [Choice("Clash", "clash")]
            [Choice("ARAM", "aram")]
            [Choice("Arena", "arena")]
            [Choice("Custom Match", "custom")]
            [Choice("Quickplay", "quickplay")]
            [Summary("mode", "The type of game to get match history for")] string mode,
            [Summary("count", "The number of matches to retrieve. Defaults to 12. Max of 25")] int count = 12)
        {
            await RespondAsync("Processing... Please wait.");
            var db = new BotBaseContext();
            var key = db.Variable.AsQueryable().Where(v => v.Name == "leagueKey").First();
            var embed = new JifBotEmbedBuilder();
            var region = GetRegionFromPlatform(platform);
            string puuid = await GetPUUID(name, tag, region, key.Value);

            var (queue, gameMode, queueTitle) = GetQueueInfo(mode);
            var champMap = new Dictionary<string, int>();
            var roleMap = new Dictionary<string, int>();
            int totalKills = 0;
            int totalDeaths = 0;
            int totalAssists = 0;
            int winCount = 0;
            int lossCount = 0;

            if (count > 25)
            {
                count = 25;
            }

            using (HttpClient client = new HttpClient())
            {
                using (var response = await client.GetAsync($"https://{region}.api.riotgames.com/lol/match/v5/matches/by-puuid/{puuid}/ids?type={gameMode}&queue={queue}&count={count}&api_key={key.Value}"))
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    List<string> matches = JsonSerializer.Deserialize<List<string>>(jsonResponse);
                    count = matches.Count;
                    int curMatch = 1;
                    if(count == 0)
                    {
                        await ModifyOriginalResponseAsync(m => { m.Content = "No matches for this game mode. Get to work!"; });
                        return;
                    }
                    foreach(var match in matches)
                    {
                        await ModifyOriginalResponseAsync(m => { m.Content = "Processing... Please wait.\n" + GetLoadBar(curMatch, count, 20); });
                        using (var matchResponse = await client.GetAsync($"https://{region}.api.riotgames.com/lol/match/v5/matches/{match}?api_key={key.Value}"))
                        {
                            string matchJsonResponse = await matchResponse.Content.ReadAsStringAsync();
                            MatchResponse matchData = JsonSerializer.Deserialize<MatchResponse>(matchJsonResponse);
                            Participant targetParticipant = new Participant();

                            foreach(var participant in matchData.info.participants)
                            {
                                if(participant.puuid == puuid)
                                {
                                    targetParticipant = participant;
                                    break;
                                }
                            }

                            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                            dateTime = dateTime.AddSeconds(matchData.info.gameStartTimestamp / 1000);
                            string date = dateTime.ToLocalTime().ToShortDateString();

                            TimeSpan timeSpan = TimeSpan.FromSeconds(matchData.info.gameDuration);
                            string gameDuration = string.Format("{0:###}:{1:D2}", timeSpan.Minutes + timeSpan.Hours*60, timeSpan.Seconds);

                            string titleEntry = "";
                            string outcome = targetParticipant.win ? "✅" : "❌";
                            string champion = Regex.Replace(targetParticipant.championName, @"([A-Z][a-z]*)([A-Z][a-z]*)*", @"$1 $2").TrimEnd(' ');
                            string position = targetParticipant.individualPosition == "Invalid" ? "" : char.ToUpper(targetParticipant.individualPosition[0]) + targetParticipant.individualPosition.Substring(1).ToLower();

                            if (position == "Utility")
                            {
                                position = "Support";
                            }

                            if (position != "")
                            {
                                if (roleMap.ContainsKey(position))
                                {
                                    roleMap[position]++;
                                }
                                else
                                {
                                    roleMap.Add(position, 1);
                                }
                            }

                            if (champMap.ContainsKey(champion))
                            {
                                champMap[champion]++;
                            }
                            else
                            {
                                champMap.Add(champion, 1);
                            }

                            if (targetParticipant.win)
                            {
                                winCount++;
                            }
                            else
                            {
                                lossCount++;
                            }
                            totalKills += targetParticipant.kills;
                            totalDeaths += targetParticipant.deaths;
                            totalAssists += targetParticipant.assists;

                            titleEntry += $"{outcome} {champion} {position}";

                            string matchEntry = "";
                            matchEntry += $"{date} ({gameDuration})";
                            matchEntry += $"\n> **{targetParticipant.kills}/{targetParticipant.deaths}/{targetParticipant.assists}** KP: {Math.Round(targetParticipant.challenges.killParticipation*100, 2)}%";
                            matchEntry += $"\nDamage dealt: {targetParticipant.totalDamageDealtToChampions:n0}";
                            matchEntry += $"\nVision Score: {targetParticipant.visionScore}";
                            matchEntry += $"\nGPM: {Math.Round(targetParticipant.challenges.goldPerMinute, 2)}";
                            matchEntry += $"\nCS: {targetParticipant.totalMinionsKilled}";
                            matchEntry += "\n";
                            if (targetParticipant.enemyMissingPings > 0)
                            {
                                matchEntry += $"<:Enemy_Missing_ping:1241517862745800884> {targetParticipant.enemyMissingPings} ";
                            }

                            if (targetParticipant.onMyWayPings > 0)
                            {
                                matchEntry += $"<:On_My_Way_ping:1241517861113958550> {targetParticipant.onMyWayPings} ";
                            }

                            if (targetParticipant.assistMePings > 0)
                            {
                                matchEntry += $"<:Assist_Me_ping:1241517861835374644> {targetParticipant.assistMePings} ";
                            }

                            if (targetParticipant.getBackPings > 0)
                            {
                                matchEntry += $"<:Retreat_ping:1241517860015046796> {targetParticipant.getBackPings} ";
                            }

                            embed.AddField(titleEntry, matchEntry, inline:true);
                        }
                        curMatch++;
                    }
                }
            }

            Summoner summoner = await GetSummoner(platform, puuid, key.Value);
            embed.ThumbnailUrl = $"https://ddragon.leagueoflegends.com/cdn/{Program.currLeagueVersion}/img/profileicon/{summoner.profileIconId}.png";
            embed.Title = $"Last {count} {queueTitle} Matches for {name}#{tag}";

            string highestChamp = GetMostUsedMapValue(champMap, "");
            string highestRole = GetMostUsedMapValue(roleMap, "Invalid");
            double avgKill = Math.Round((double)totalKills / count, 2);
            double avgDeath = Math.Round((double)totalDeaths / count, 2);
            double avgAssist = Math.Round((double)totalAssists / count, 2);
            double winPercent = Math.Round((double)winCount / count * 100.0, 2);

            embed.Description = $"Most played champion: {highestChamp}";
            if(highestRole != "")
            {
                embed.Description += $"\nMost played role: {highestRole}";
            }
            embed.Description += $"\nAverage KDA: {avgKill}/{avgDeath}/{avgAssist}";
            embed.Description += $"\n{winCount}W {lossCount}L ({winPercent}%)";


            await ModifyOriginalResponseAsync(m => { m.Embed = embed.Build(); m.Content = ""; });
            
        }

        [SlashCommand("stats", "Gives Jif Bot usage statistics. User data must be opted into via /optin. Data begins 8/15/2024.")]
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
                    if (commands.Count == 0)
                    {
                        await RespondAsync("User is not opted into data collection. Use /optin to start tracking your data!", ephemeral: true);
                        return;
                    }
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

        [SlashCommand("blorbopedia", "Looks up a saved characters by name, or by author")]
        public async Task Blorbopedia(
            [Summary("character-key", "The key for the character you are looking for")] string key = null,
            [Summary("author", "The author you would like to retreive the characters for")] IUser author = null)
        {
            var db = new BotBaseContext();
            if (author != null)
            {
                var characters = db.Character.AsQueryable().Where(c => c.UserId == author.Id).ToList();
                if (characters.Count() == 1)
                {
                    key = characters[0].Key;
                }
                else if (characters.Count() > 1)
                {
                    var msg = $"{author.Username}'s characters:";
                    foreach( var character in characters)
                    {
                        msg += $"\n{character.Key}";
                        if (character.Name != "")
                            msg += $" - [{character.Name}]";
                    }
                    await RespondAsync(msg);
                    return;
                }
                else if (key == null)
                {
                    await RespondAsync("User has no characters!", ephemeral: true);
                    return;
                }
            }
            if(key != null)
            {
                var character = db.Character.AsQueryable().Where(c => c.Key == key).FirstOrDefault();
                if (character == null)
                {
                    await RespondAsync("Invalid character key provided, please try again", ephemeral: true);
                    return;
                }
                var user = db.User.AsQueryable().Where(u => u.UserId == character.UserId).FirstOrDefault();

                JifBotEmbedBuilder embed = new JifBotEmbedBuilder();
                embed.Title = character.Name != "" ? character.Name: character.Key;
                embed.Description = "";
                if (character.Title != "")
                    embed.Description += $"*{character.Title}*\n\n";
                if (character.Description != "")
                    embed.Description += character.Description;
                if (character.Occupation != "")
                    embed.AddField("Occupation", character.Occupation, inline: true);
                if (character.Age != "")
                    embed.AddField("Age", character.Age, inline: true);
                if (character.Race != "")
                    embed.AddField("Race", character.Race, inline: true);
                if (character.Pronouns != "")
                    embed.AddField("Pronouns", character.Pronouns, inline: true);
                if (character.Sexuality != "")
                    embed.AddField("Sexuality", character.Sexuality, inline: true);
                if (character.Origin != "")
                    embed.AddField("Origin", character.Origin, inline: true);
                if (character.Residence != "")
                    embed.AddField("Residence", character.Residence, inline: true);
                if (character.Universe != "")
                    embed.AddField("Universe", character.Universe, inline: true);
                if (character.Resources != "")
                    embed.AddField("Additional Resources", character.Resources);
                embed.WithFooter($"Character by {user.Name}");
                if (character.ImageUrl != "")
                {
                    if (character.CompactImage)
                        embed.ThumbnailUrl = character.ImageUrl;
                    else
                        embed.ImageUrl = character.ImageUrl;
                }
                await RespondAsync(embed: embed.Build());
                return;
            } 
            await RespondAsync("No valid character key or user provided. Please try again.", ephemeral: true);
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

        async private Task<string> GetChampionById(string id)
        {
            if (!leagueUpToDate)
            {
                await ReloadLeagueVersion();
            }
            return Program.championLookup[id];
        }

        async private Task<Summoner> GetSummoner(string platform, string puuid, string key)
        {
            // We'll need to have this updated if the summoner icon gets used
            if (!leagueUpToDate)
            {
                await ReloadLeagueVersion();
            }
            using (HttpClient client = new HttpClient())
            {
                using (var response = await client.GetAsync($"https://{platform}.api.riotgames.com/lol/summoner/v4/summoners/by-puuid/{puuid}?api_key={key}"))
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    Summoner summoner = JsonSerializer.Deserialize<Summoner>(jsonResponse);
                    return summoner;
                }
            }
        }

        async private Task ReloadLeagueVersion()
        {
            string version = "";
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync("https://ddragon.leagueoflegends.com/api/versions.json");
                HttpContent content = response.Content;
                string stuff = await content.ReadAsStringAsync();
                version = stuff.Remove(0, 1).Split(',').ToList()[0].Replace("\"", "");
            }
            if (Program.currLeagueVersion != version)
            {
                Program.currLeagueVersion = version;
                using (HttpClient client = new HttpClient())
                {
                    using (var response = await client.GetAsync($"https://ddragon.leagueoflegends.com/cdn/{version}/data/en_US/champion.json"))
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        ChampionResult championResult = JsonSerializer.Deserialize<ChampionResult>(jsonResponse);
                        Program.championLookup.Clear();
                        foreach(var champion in championResult.data)
                        {
                            Program.championLookup.Add(champion.Value.key, champion.Value.id);
                        }
                    }
                }
            }
            leagueUpToDate = true;
        }

        private string GetRegionFromPlatform(string platform)
        {
            switch (platform)
            {
                case "eun1":
                case "euw1":
                case "ru":
                case "tr1":
                    return "europe";

                case "oc1":
                case "ph2":
                case "vn2":
                case "th2":
                case "sg2":
                    return "sea";

                case "jp1":
                case "kr":
                case "tw2":
                    return "asia";

                case "br1":
                case "la1":
                case "la2":
                case "na1":
                default:
                    return "americas";
            }
        }

        async private Task<string> GetPUUID(string name, string tag, string region, string apiKey)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync($"https://{region}.api.riotgames.com/riot/account/v1/accounts/by-riot-id/{name}/{tag}?api_key={apiKey}");
                HttpContent content = response.Content;
                string stuff = await content.ReadAsStringAsync();
                var json = JObject.Parse(stuff);
                return (string)json.SelectToken("puuid");
            }
        }

        private (int queue, string type, string name) GetQueueInfo(string queueTag)
        {
            switch(queueTag)
            {
                case "blind":
                    return ((int)LeagueQueue.Blind, "normal", "Summoner's Rift: Blind Pick");
                case "draft":
                    return ((int)LeagueQueue.Draft, "normal", "Summoner's Rift: Draft Pick");
                default:
                case "ranked":
                    return ((int)LeagueQueue.Ranked, "ranked", "Summoner's Rift: Ranked Solo");
                case "flex":
                    return ((int)LeagueQueue.Flex, "ranked", "Summoner's Rift: Ranked Flex");
                case "clash":
                    return ((int)LeagueQueue.Clash, "normal", "Summoner's Rift: Clash");
                case "aram":
                    return ((int)LeagueQueue.ARAM, "normal", "The Howling Abyss");
                case "arena":
                    return ((int)LeagueQueue.Arena, "ranked", "Rings of Wrath: Arena");
                case "custom":
                    return ((int)LeagueQueue.Custom, "normal", "Summoner's Rift: Custom Match");
                case "quickplay":
                    return ((int)LeagueQueue.Quickplay, "normal", "Summoner's Rift: Quickplay");

            }
        }

        private string GetMostUsedMapValue(Dictionary<string, int> dict, string badValue)
        {
            string mostUsed = "";
            int highestCount = 0;

            if (dict.Count > 0)
            {
                foreach (KeyValuePair<string, int> kvp in dict)
                {
                    if (kvp.Value > highestCount && kvp.Key != badValue)
                    {
                        highestCount = kvp.Value;
                        mostUsed = kvp.Key;
                    }
                }
            }

            return mostUsed;
        }

        private string GetLoadBar(int current, int end, int barLength)
        {
            double percentLoaded = ((double)current / (double)end);
            int loaded = (int)(percentLoaded * barLength);
            string bar = "`[";
            for(int i = 0; i < barLength; i++)
            {
                if(i <= loaded)
                {
                    bar += "=";
                }
                else
                {
                    bar += " ";
                }
            }
            bar += "]`";
            return bar;
        }
    }

    class UrbanDictionaryDefinition
    {
        public string definition { get; set; }
        public string example { get; set; }
        public string word { get; set; }
        public string written_on { get; set; }
        public string permalink { get; set; }
        public int thumbs_up { get; set; }
        public int thumbs_down { get; set; }
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

    class ChampionResult
    {
        public Dictionary<string, Champion> data { get; set; }
    }

    class Champion
    {
        public string id { get; set; }
        public string key { get; set; }
    }

    class Mastery
    {
        public long lastPlayTime { get; set; }
        public int championLevel { get; set; }
        public long championId { get; set; }
        public int championPoints { get; set; }
    }

    class MatchResponse
    {
        public MatchInfo info { get; set; }
    }

    class MatchInfo
    {
        public List<Participant> participants { get; set; }
        public long gameDuration { get; set; }
        public long gameStartTimestamp { get; set; }
    }

    class Participant
    {
        public string puuid { get; set; }
        public string championName { get; set; }
        public string individualPosition { get; set; }
        public bool win { get; set; }
        public int kills { get; set; }
        public int deaths { get; set; }
        public int assists { get; set; }
        public int enemyMissingPings { get; set; }
        public int assistMePings { get; set; }
        public int getBackPings { get; set; }
        public int onMyWayPings { get; set; }
        public int visionScore { get; set; }
        public int totalMinionsKilled { get; set; }
        public int totalDamageDealtToChampions { get; set; }
        public Challenge challenges { get; set; }

    }

    class Challenge
    {
        public double killParticipation { get; set; }
        public double goldPerMinute { get; set; }
    }

    class Summoner
    {
        public int profileIconId { get; set; }
        public int summonerLevel { get; set; }
    }
}
