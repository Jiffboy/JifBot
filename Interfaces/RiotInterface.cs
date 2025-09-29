using JifBot.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace JifBot.Interfaces
{
    public enum LeagueQueue : int
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
    public class RiotInterface
    {
        private static string currLeagueVersion = "";
        private static Dictionary<string, Champion> championLookup = new Dictionary<string, Champion>();
        private string apiKey = "";

        public RiotInterface()
        {
            var db = new BotBaseContext();
            apiKey = db.Variable.AsQueryable().Where(v => v.Name == "leagueKey").First().Value;
        }

        async public Task<List<Mastery>> GetMasteries(string platform, string puuid)
        {
            var region = GetRegionFromPlatform(platform);

            await VerifyData();

            using (HttpClient client = new HttpClient())
            {
                using (var response = await client.GetAsync($"https://{platform}.api.riotgames.com/lol/champion-mastery/v4/champion-masteries/by-puuid/{puuid}?api_key={apiKey}"))
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    List<Mastery> masteryResult = JsonSerializer.Deserialize<List<Mastery>>(jsonResponse);

                    return masteryResult;
                }
            }
        }

        async public Task<List<string>> GetMatchIds(LeagueQueue queue, int count, string puuid, string region)
        {
            // We don't need the data verified here, but only need to call once instead of per GetMatch() call
            await VerifyData();

            var (gameMode, queueTitle) = GetQueueInfo(queue);

            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetAsync($"https://{region}.api.riotgames.com/lol/match/v5/matches/by-puuid/{puuid}/ids?type={gameMode}&queue={(int)queue}&count={count}&api_key={apiKey}");
                string jsonResponse = await response.Content.ReadAsStringAsync();
                List<string> matches = JsonSerializer.Deserialize<List<string>>(jsonResponse);

                return matches;
            }
        }

        async public Task<Match> GetMatch(string region, string matchId)
        {
            using (HttpClient client = new HttpClient())
            {
                var matchResponse = await client.GetAsync($"https://{region}.api.riotgames.com/lol/match/v5/matches/{matchId}?api_key={apiKey}");
                string matchJsonResponse = await matchResponse.Content.ReadAsStringAsync();
                Match match = JsonSerializer.Deserialize<MatchResponse>(matchJsonResponse).info;

                return match;
            }
        }

        async private Task VerifyData()
        {
            string version = "";
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync("https://ddragon.leagueoflegends.com/api/versions.json");
                HttpContent content = response.Content;
                string stuff = await content.ReadAsStringAsync();
                version = stuff.Remove(0, 1).Split(',').ToList()[0].Replace("\"", "");
            }
            if (currLeagueVersion != version)
            {
                using (HttpClient client = new HttpClient())
                {
                    using (var response = await client.GetAsync($"https://ddragon.leagueoflegends.com/cdn/{version}/data/en_US/champion.json"))
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        ChampionResult championResult = JsonSerializer.Deserialize<ChampionResult>(jsonResponse);
                        championLookup.Clear();
                        foreach (var champion in championResult.data)
                        {
                            champion.Value.iconUrl = $"https://ddragon.leagueoflegends.com/cdn/{version}/img/champion/{champion.Value.id}.png";
                            championLookup.Add(champion.Value.key, champion.Value);
                        }
                    }
                }
                currLeagueVersion = version;
            }
        }

        async public Task<Summoner> GetSummoner(string platform, string puuid)
        {
            using (HttpClient client = new HttpClient())
            {
                using (var response = await client.GetAsync($"https://{platform}.api.riotgames.com/lol/summoner/v4/summoners/by-puuid/{puuid}?api_key={apiKey}"))
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    Summoner summoner = JsonSerializer.Deserialize<Summoner>(jsonResponse);
                    return summoner;
                }
            }
        }

        async public Task<Account> GetAccount(string name, string tag)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync($"https://americas.api.riotgames.com/riot/account/v1/accounts/by-riot-id/{name}/{tag}?api_key={apiKey}");
                string jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Account>(jsonResponse);
            }
        }

        public (string type, string name) GetQueueInfo(LeagueQueue queue)
        {
            switch (queue)
            {
                case LeagueQueue.Blind:
                    return ("normal", "Summoner's Rift: Blind Pick");
                case LeagueQueue.Draft:
                    return ( "normal", "Summoner's Rift: Draft Pick");
                default:
                case LeagueQueue.Ranked:
                    return ( "ranked", "Summoner's Rift: Ranked Solo");
                case LeagueQueue.Flex:
                    return ("ranked", "Summoner's Rift: Ranked Flex");
                case LeagueQueue.Clash:
                    return ("normal", "Summoner's Rift: Clash");
                case LeagueQueue.ARAM:
                    return ("normal", "The Howling Abyss");
                case LeagueQueue.Arena:
                    return ("ranked", "Rings of Wrath: Arena");
                case LeagueQueue.Custom:
                    return ("normal", "Summoner's Rift: Custom Match");
                case LeagueQueue.Quickplay:
                    return ("normal", "Summoner's Rift: Quickplay");

            }
        }

        public string GetRegionFromPlatform(string platform)
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

        class ChampionResult
        {
            public Dictionary<string, Champion> data { get; set; }
        }

        public class Champion
        {
            public string id { get; set; }
            public string key { get; set; }
            public string iconUrl { get; set; }
            public string name { get; set; }
        }

        public class Mastery
        {
            public long lastPlayTime { get; set; }
            public int championLevel { get; set; }
            public int championId
            {
                set
                {
                    champion = championLookup[value.ToString()];
                }
            }
            public int championPoints { get; set; }
            public Champion champion { get; set; }
        }

        class MatchResponse
        {
            public Match info { get; set; }
        }

        public class Match
        {
            public List<Participant> participants { get; set; }
            public long gameDuration { get; set; }
            public long gameStartTimestamp { get; set; }
        }

        public class Participant
        {
            public string puuid { get; set; }
            public int championId
            {
                set
                {
                    champion = championLookup[value.ToString()];
                }
            }
            public Champion champion { get; set; }
            public string teamPosition {
                set
                {
                    string pos = value.ToString();
                    if (pos != "")
                        pos = char.ToUpper(pos[0]) + pos.Substring(1).ToLower();
                    pos = pos.Replace("Utility", "Support");
                    position = pos;
                }
            }
            public string position { get; set; }
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

        public class Challenge
        {
            public double killParticipation { get; set; }
            public double goldPerMinute { get; set; }
        }

        public class Summoner
        {
            public int profileIconId
            {
                set
                {
                    profileIconUrl = $"https://ddragon.leagueoflegends.com/cdn/{currLeagueVersion}/img/profileicon/{value.ToString()}.png";
                }
            }
            public int summonerLevel { get; set; }
            public string profileIconUrl { get; set; }
        }

        public class Account
        {
            public string gameName { get; set; }
            public string tagLine { get; set; }
            public string puuid { get; set; }
        }
    }
}
