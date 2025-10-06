using Discord;
using Discord.Interactions;
using JifBot.Embeds;
using JifBot.Interfaces;
using JifBot.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace JifBot.Commands
{
    public class Information : InteractionModuleBase<SocketInteractionContext>
    {
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
                        if (defineResult[0].phonetics.Count > 1 && defineResult[0].phonetics[1].audio != "")
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
                        await RespondAsync(word + " is not in the English dictionary.");
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
                    int netThumbs = definition.thumbs_up - definition.thumbs_down;
                    if (netThumbs > currVote)
                    {
                        currDefinition = definition;
                        currVote = netThumbs;
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

                if (cleanDefinition.Length >= 1024)
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

        [SlashCommand("summoner", "Gives a general overview of a league of legends summoner.")]
        public async Task Summoner(
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
            await DeferAsync();

            var riotInterface = new RiotInterface();
            var embed = new RiotEmbedBuilder();

            var account = await riotInterface.GetAccount(name, tag);
            var summoner = await riotInterface.GetSummoner(platform, account.puuid);
            var region = riotInterface.GetRegionFromPlatform(platform);
            var matchIds = await riotInterface.GetMatchIds(LeagueQueue.None, 20, account.puuid, region);
            var masteries = await riotInterface.GetMasteries(platform, account.puuid);
            var standings = await riotInterface.GetStandings(platform, account.puuid);

            embed.ThumbnailUrl = summoner.profileIconUrl;
            embed.Title = $"League profile for {account.gameName}#{account.tagLine}";
            embed.Description = $"Summoner Level: {summoner.summonerLevel}";

            foreach(var standing in standings)
            {
                embed.AddStanding(standing);
            }

            var masteryMsg = $"Total Mastery: {masteries.Sum(m => m.championPoints):n0}";
            masteryMsg += $"\nChampions: {masteries.Count}";
            embed.AddField("======= Champion Mastery =======", masteryMsg);

            int count = 1;
            foreach (var mastery in masteries)
            {
                if (count > 3)
                    break;
                embed.AddMastery(mastery, compact: true);
                count++;
            }

            var matches = new List<RiotInterface.Match>();
            foreach(var matchId in matchIds)
            {
                var match = await riotInterface.GetMatch(region, matchId);
                matches.Add(match);
            }

            var recentMatches = "";
            foreach(var group in matches.GroupBy(m => m.queue.name))
            {
                var wins = group.Count(m => m.participants.Where(p => p.puuid == account.puuid).First().win);
                recentMatches += $"{group.First().queue.shorthand}: {wins}W - {group.Count() - wins}L\n";
            }

            embed.AddField("===== Recent Match History ======", recentMatches);

            count = 1;
            foreach (var match in matches)
            {
                if (count > 3)
                    break;
                embed.AddMatch(match, account.puuid, true, true);
                count++;
            }

            await FollowupAsync(embed: embed.Build());
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
            [Summary("tag", "The identifying tag of the account. Example: NA1")] string tag,
            [Summary("count", "The number of champions to display. Default 10, max 20.")] int count = 10)
        {
            if (count > 20)
                count = 20;
            if (count < 0)
                count = 10;

            var embed = new RiotEmbedBuilder();

            long totalMastery = 0;

            var riotInterface = new RiotInterface();
            var account = await riotInterface.GetAccount(name, tag);
            var masteries = await riotInterface.GetMasteries(platform, account.puuid);
            var summoner = await riotInterface.GetSummoner(platform, account.puuid);

            embed.Title = $"Top {count} mastery scores for {account.gameName}#{account.tagLine}";
            embed.ThumbnailUrl = masteries[0].champion.iconUrl;
            int i = 0;
            foreach (var mastery in masteries)
            {
                if (i < count)
                {
                    embed.AddMastery(mastery);
                }
                totalMastery += mastery.championPoints;
                i++;
            }
            embed.Description = $"Summoner Level: {summoner.summonerLevel}\nTotal Mastery Points: {totalMastery:n0}\nTotal Champions: {masteries.Count}";

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
            [Choice("Blind Pick", (int)LeagueQueue.Blind)]
            [Choice("Draft Pick", (int)LeagueQueue.Draft)]
            [Choice("Ranked Solo", (int)LeagueQueue.Ranked)]
            [Choice("Ranked Flex", (int)LeagueQueue.Flex)]
            [Choice("Clash", (int)LeagueQueue.Clash)]
            [Choice("ARAM", (int)LeagueQueue.ARAM)]
            [Choice("Arena", (int)LeagueQueue.Arena)]
            [Choice("Custom Match", (int)LeagueQueue.Custom)]
            [Choice("Quickplay", (int)LeagueQueue.Quickplay)]
            [Summary("mode", "The type of game to get match history for")] LeagueQueue mode,
            [Summary("count", "The number of matches to retrieve. Defaults to 12. Max of 25")] int count = 12)
        {
            if (count > 25)
            {
                count = 25;
            }

            await DeferAsync();
            var embed = new RiotEmbedBuilder();
            var riotInterface = new RiotInterface();

            var region = riotInterface.GetRegionFromPlatform(platform);
            var account = await riotInterface.GetAccount(name, tag);
            var matches = await riotInterface.GetMatchIds(mode, count, account.puuid, region);

            count = matches.Count;
            if (count == 0)
            {
                await FollowupAsync("No matches for this game mode. Get to work!");
                return;
            }

            var champMap = new Dictionary<string, int>();
            var roleMap = new Dictionary<string, int>();
            int totalKills = 0;
            int totalDeaths = 0;
            int totalAssists = 0;
            int winCount = 0;
            int lossCount = 0;

            foreach (var matchId in matches)
            {
                var match = await riotInterface.GetMatch(region, matchId);
                embed.AddMatch(match, account.puuid);

                var targetParticipant = match.participants.Where(p => p.puuid == account.puuid).First();

                if (targetParticipant.position != "")
                {
                    if (roleMap.ContainsKey(targetParticipant.position))
                        roleMap[targetParticipant.position]++;
                    else
                        roleMap.Add(targetParticipant.position, 1);
                }

                if (champMap.ContainsKey(targetParticipant.champion.name))
                    champMap[targetParticipant.champion.name]++;
                else
                    champMap.Add(targetParticipant.champion.name, 1);

                if (targetParticipant.win)
                    winCount++;
                else
                    lossCount++;

                totalKills += targetParticipant.kills;
                totalDeaths += targetParticipant.deaths;
                totalAssists += targetParticipant.assists;
            }

            var summoner = await riotInterface.GetSummoner(platform, account.puuid);
            var queue = riotInterface.GetQueueInfo(mode);

            embed.ThumbnailUrl = summoner.profileIconUrl;
            embed.Title = $"Last {count} {queue.name} Matches for {name}#{tag}";

            string highestChamp = GetMostUsedMapValue(champMap, "");
            string highestRole = GetMostUsedMapValue(roleMap, "");
            double avgKill = Math.Round((double)totalKills / count, 2);
            double avgDeath = Math.Round((double)totalDeaths / count, 2);
            double avgAssist = Math.Round((double)totalAssists / count, 2);
            double winPercent = Math.Round((double)winCount / count * 100.0, 2);

            embed.Description = $"Most played champion: {highestChamp}";
            if (highestRole != "")
            {
                embed.Description += $"\nMost played role: {highestRole}";
            }
            var kd = Math.Round((double)(totalKills + totalAssists) / (double)totalDeaths, 2);
            embed.Description += $"\nAverage KDA: {avgKill}/{avgDeath}/{avgAssist} ({kd})";
            embed.Description += $"\n{winCount}W {lossCount}L ({winPercent}%)";

            await FollowupAsync(embed: embed.Build());
        }

        public string FormatTime(DateTimeOffset orig)
        {
            string str = "";
            str = str + orig.LocalDateTime.DayOfWeek + ", ";
            str = str + orig.LocalDateTime.Month + "/" + orig.LocalDateTime.Day + "/" + orig.LocalDateTime.Year;
            str = str + " at " + orig.LocalDateTime.Hour + ":" + orig.LocalDateTime.Minute + " CST";
            return str;
        }

        [SlashCommand("isjifsleeping", "Shows whether or not Jif has been sleeping lately.")]
        public async Task IsJifSleeping(
            [Summary("days", "The number of days to view. Defaults to 9. Max of 25")] int count = 9,
            [Summary("target", "The targeted number of hours needed a night. Defaults to 8")] int target = 8)
        {
            var fitbit = new FitBitInterface();
            var data = await fitbit.GetSleep(DateTime.Now.AddDays(-(count - 1)), DateTime.Now);
            var totalTime = data.AsQueryable().Sum(e => e.totalTime);
            var avgTime = totalTime / data.Count;
            var targetTime = data.Count * target * 60;

            var avgStartTicks = (long)data.Select(t => t.start.TimeOfDay.Ticks).Average();
            var startTime = new TimeSpan(avgStartTicks);
            var startStr = DateTime.Today.Add(startTime).ToString("hh:mm tt");

            var avgEndTicks = (long)data.Select(t => t.end.TimeOfDay.Ticks).Average();
            var endTime = new TimeSpan(avgEndTicks);
            var endStr = DateTime.Today.Add(endTime).ToString("hh:mm tt");

            var avgStr = FormatMinutes(avgTime);

            var embed = new JifBotEmbedBuilder();

            if (totalTime < targetTime)
            {
                embed.ThumbnailUrl = "https://cdn.discordapp.com/attachments/782655615557697536/1409912107440406598/cooltext489523824426323.gif?ex=68af1a79&is=68adc8f9&hm=16c1e5a16cb4992d5cd6a17d3761c9dc277a9c25c64b076c9550d406fb589f48&";
            }
            else
            {
                embed.ThumbnailUrl = "https://cdn.discordapp.com/attachments/782655615557697536/1409912106794618880/cooltext489559432758058.gif?ex=68af1a79&is=68adc8f9&hm=782f8c47c4e6167f958fef80c4040461af78ab7a088c7e5e3aba91a1763aad4c&";
            }

            var header = totalTime >= targetTime ? "His ass is sleeping!!!" : "His ass is not sleeping!!";
            var modifier = totalTime >= targetTime ? "excess" : "deficit";
            embed.Title = "Is Jif sleeping?";
            embed.Description = $"# {header}";
            embed.Description += $"\n**Sleep {modifier}**: {FormatMinutes(Math.Abs(totalTime - targetTime))}  [Based on targeted {target} hours per night]";
            embed.Description += $"\n**Total time**: {FormatMinutes(totalTime)}";
            embed.Description += $"\n**Average nightly sleep**: {avgStr}";
            embed.Description += $"\n**Average time asleep**: {startStr}";
            embed.Description += $"\n**Average time awake**: {endStr}";

            var today = DateTime.Today;

            foreach (var entry in data)
            {
                while (entry.date.Date != today.Date)
                {
                    embed.AddField($"⚠️ {today.ToString("MM/dd")}", "\n\n[Data Missing]\nCheck back later!", inline: true);
                    today = today.AddDays(-1);
                }

                var emote = entry.totalTime / 60 >= target ? "✅" : "❌";
                var title = $"{emote} {entry.date.ToString("MM/dd")} [{FormatMinutes(entry.totalTime)}]";
                var msg = $"{entry.start.ToString("hh:mm tt")} - {entry.end.ToString("hh:mm tt")}";
                msg += $"\nTimes awoken: {entry.wakeCount}";
                msg += $"\nNapped: {FormatMinutes(entry.napTime)}";
                msg += $"\nDeep: {FormatMinutes(entry.deepTime)}";
                msg += $"\nLight: {FormatMinutes(entry.lightTime)}";
                msg += $"\nREM: {FormatMinutes(entry.remTime)}";
                embed.AddField(title, msg, inline: true);

                today = today.AddDays(-1);
            }

            await RespondAsync(embed: embed.Build());
        }

        public string FormatMinutes(int minutes)
        {
            return $"{minutes / 60}h {minutes % 60}m";
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
    }
}