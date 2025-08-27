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
using JifBot.Interfaces;

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
            [Summary("tag", "The identifying tag of the account. Example: NA1")] string tag,
            [Summary("count", "The number of champions to display. Default 10, max 20.")] int count=10)
        {
            if (count > 20)
                count = 20;
            if (count < 0)
                count = 10;

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
                    embed.Title = $"Top {count} mastery scores for {name}#{tag}";
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

                        if (i <= count)
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
            await DeferAsync();
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
            double kd = 0;
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
                        await FollowupAsync("No matches for this game mode. Get to work!");
                        return;
                    }
                    foreach(var match in matches)
                    {
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
                            champion = champion.Replace("Monkey King", "Wukong");
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
                            kd = Math.Round((double)(targetParticipant.kills + targetParticipant.assists) / (double)targetParticipant.deaths, 2);


                            titleEntry += $"{outcome} {champion} {position}";

                            string matchEntry = "";
                            matchEntry += $"{date} ({gameDuration})";
                            matchEntry += $"\n> **{targetParticipant.kills}/{targetParticipant.deaths}/{targetParticipant.assists}** ({kd})";
                            matchEntry += $"\nKP: {Math.Round(targetParticipant.challenges.killParticipation*100, 2)}%";
                            matchEntry += $"\nDmg: {targetParticipant.totalDamageDealtToChampions:n0}";
                            matchEntry += $"\nGPM: {Math.Round(targetParticipant.challenges.goldPerMinute, 2)}";
                            matchEntry += $"\nCS: {targetParticipant.totalMinionsKilled}";
                            matchEntry += $"\nVision: {targetParticipant.visionScore}";
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
            kd = Math.Round((double)(totalKills + totalAssists) / (double)totalDeaths, 2);
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
            var data = await fitbit.GetSleep(DateTime.Now.AddDays(-(count-1)), DateTime.Now);
            var totalTime = data.AsQueryable().Sum(e => e.totalTime);
            var targetTime = data.Count * target * 60;

            var avgStartTicks = (long)data.Select(t => t.start.TimeOfDay.Ticks).Average();
            var startTime = new TimeSpan(avgStartTicks);
            var startStr = DateTime.Today.Add(startTime).ToString("hh:mm tt");

            var avgEndTicks = (long)data.Select(t => t.end.TimeOfDay.Ticks).Average();
            var endTime = new TimeSpan(avgEndTicks);
            var endStr = DateTime.Today.Add(endTime).ToString("hh:mm tt");

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
            return $"{minutes/60}h {minutes%60}m";
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
