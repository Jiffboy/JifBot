using JifBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JifBot.Interfaces
{
    public class FitBitInterface
    {
        private Logger logger = new Logger();

        public async Task<List<SleepDate>> GetSleep(DateTime start, DateTime end)
        {
            using HttpClient client = new HttpClient();
            var db = new BotBaseContext();

            var fitbitTimestamp = db.Variable.AsQueryable().Where(v => v.Name == "fitbitTimestamp").First().Value;
            var oathTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(fitbitTimestamp)).ToLocalTime().DateTime;

            if (DateTime.Now > oathTime)
            {
                await logger.WriteInfo("Refreshing Fitbit Keys", "FitBitInterface");
                await RefreshKeys();
            }

            var accessToken = db.Variable.AsQueryable().Where(v => v.Name == "fitbitAccessToken").First().Value;
            var userId = db.Variable.AsQueryable().Where(v => v.Name == "fitbitUserId").First().Value;

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            var response = await client.GetAsync($"https://api.fitbit.com/1.2/user/{userId}/sleep/date/{start.ToString("yyyy-MM-dd")}/{end.ToString("yyyy-MM-dd")}.json");
            var json = await response.Content.ReadAsStringAsync();
            var sleepData = JsonSerializer.Deserialize<FitBitSleepData>(json);

            return Transform(sleepData.sleep);
        }

        private async Task RefreshKeys()
        {
            using HttpClient client = new HttpClient();
            var db = new BotBaseContext();

            var refreshToken = db.Variable.AsQueryable().Where(v => v.Name == "fitbitRefreshToken").First();
            var accessToken = db.Variable.AsQueryable().Where(v => v.Name == "fitbitAccessToken").First();
            var timestamp = db.Variable.AsQueryable().Where(v => v.Name == "fitbitTimestamp").First();

            var authKey = db.Variable.AsQueryable().Where(v => v.Name == "fitbitAuthKey").First().Value;
            var clientId = db.Variable.AsQueryable().Where(v => v.Name == "fitbitClientId").First().Value;

            client.DefaultRequestHeaders.Add("Authorization", $"Basic {authKey}");

            var content = new StringContent(
                $"grant_type=refresh_token&client_id={clientId}&refresh_token={refreshToken.Value}",
                Encoding.UTF8,
                "application/x-www-form-urlencoded"
            );

            var response = await client.PostAsync($"https://api.fitbit.com/oauth2/token", content);
            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<FitBitRefresh>(json);

            refreshToken.Value = data.refresh_token;
            accessToken.Value = data.access_token;
            timestamp.Value = (DateTimeOffset.UtcNow.ToLocalTime().ToUnixTimeSeconds() + data.expires_in).ToString();

            db.SaveChanges();
        }

        private List<SleepDate> Transform(List<FitBitSleepEntry> data)
        {
            var list = new List<SleepDate>();
            var groupings = data.AsQueryable().GroupBy(e => e.dateOfSleep);

            foreach (var grouping in groupings)
            {
                var sleepDate = new SleepDate();
                foreach (var entry in grouping)
                {
                    if (entry.isMainSleep)
                    {
                        // The first and last entries are often being awake, so we remove those, they shouldn't count.
                        var sublist = entry.levels.data.Skip(1).Take(entry.levels.data.Count - 2).ToList();
                        sleepDate.wakeCount = sublist.AsQueryable().Where(e => e.level == "wake").Count();
                        sleepDate.date = DateTime.Parse(entry.dateOfSleep);
                        sleepDate.start = DateTime.Parse(entry.startTime);
                        sleepDate.end = DateTime.Parse(entry.endTime);
                        sleepDate.deepTime = entry.levels.summary.deep.minutes;
                        sleepDate.lightTime = entry.levels.summary.light.minutes;
                        sleepDate.remTime = entry.levels.summary.rem.minutes;
                    }
                    else
                    {
                        sleepDate.napTime += entry.minutesAsleep;
                    }
                }
                sleepDate.totalTime = sleepDate.napTime + sleepDate.remTime + sleepDate.lightTime + sleepDate.deepTime;
                list.Add(sleepDate);
            }

            return list;
        }
    }

    public class SleepDate
    {
        public DateTime date { get; set; }
        public DateTime start { get; set; }
        public DateTime end { get; set; }
        public int totalTime { get; set; } = 0;
        public int wakeCount { get; set; } = 0;
        public int napTime { get; set; } = 0;
        public int deepTime { get; set; } = 0;
        public int lightTime { get; set; } = 0;
        public int remTime { get; set; } = 0;
    }

    public class FitBitSleepData
    {
        public List<FitBitSleepEntry> sleep {  get; set; }
    }

    public class FitBitSleepEntry
    {
        public string dateOfSleep { get; set; }
        public string startTime { get; set; }
        public string endTime { get; set; }
        public bool isMainSleep { get; set; }
        public int minutesAsleep { get; set; }
        public FitBitLevels levels { get; set; }
    }

    public class FitBitLevels
    {
        public List<FitBitLevelData> data { get; set; }
        public FitBitSummary summary { get; set; }
    }

    public class FitBitLevelData
    {
        public string level { get; set; }
    }

    public class FitBitSummary
    {
        public FitBitSleepStage deep { get; set; }
        public FitBitSleepStage light { get; set; }
        public FitBitSleepStage rem { get; set; }
    }

    public class FitBitSleepStage
    {
        public int minutes { get; set; }
        public int thirtyDayAvgMinutes { get; set; }
    }

    public class FitBitRefresh
    {
        public string access_token { get; set; }
        public long expires_in { get; set; }
        public string refresh_token { get; set; }
    }
}
