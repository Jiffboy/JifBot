using Discord.Interactions;
using JifBot.Builders;
using JifBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace JifBot.Commands
{
    public class Cheese : InteractionModuleBase<SocketInteractionContext>
    {
        private Dictionary<int, string> mapMap = new Dictionary<int, string>
        {
            { 0, "Gouda Gardens" },
            { 1, "Mount Cheddarest" },
            { 2, "Fondue Factory" }
        };

        private Dictionary<int, string> mapIcon = new Dictionary<int, string>
        {
            { 0, "https://media.discordapp.net/attachments/1409756152127950929/1574152724600066150/map-0.png?ex=6b049b77&is=6b0349f7&hm=4a6524ad8c05cbb64e20ab17047893fd66e92f2c6bd513809ef63082cf2e2da7&format=webp&quality=lossless&width=384&height=384" },
            { 1, "https://media.discordapp.net/attachments/1409756152127950929/1574152724906516751/map-1.png?ex=6b049b77&is=6b0349f7&hm=7e312def559fd6101fed10e5cba3163b6b9b90d3f3b00b91c1af6418cfeaf490&format=webp&quality=lossless&width=384&height=384" },
            { 2, "https://media.discordapp.net/attachments/1409756152127950929/1574152724260331572/map-2.png?ex=6b049b77&is=6b0349f7&hm=7b0bcfedb0d905459c5d5a208cc1329ec4df01c67f1b91d7767aff69ba2d7ec4&format=webp&quality=lossless&width=384&height=384" }
        };

        private Dictionary<int, string> charMap = new Dictionary<int, string>
        {
            { 0, "Lightning McSqueak" },
            { 1, "Big Hoppa" },
            { 2, "Linda" },
            { 3, "Billy Dozer" }
        };

        private Dictionary<int, string> charEmoji = new Dictionary<int, string>
        {
            { 0, "<:cc0:1574181784432812192>" },
            { 1, "<:cc1:1574181781941264517>" },
            { 2, "<:cc2:1574181785821122691>" },
            { 3, "<:cc3:1574181783258402926>" }
        };

        private Dictionary<int, string> modeMap = new Dictionary<int, string>
        {
            { 0, "Chaos" },
            { 1, "Seeded" }
        };

        [SlashCommand("cheesemeup", "Generates a key to be used to connect with Cheese Chase.")]
        public async Task CheeseMeUp()
        {
            var db = new BotBaseContext();
            var user = db.GetUser(Context.User);
            if (user.CheeseChaseKey == null || user.CheeseChaseKey == "")
            {
                const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
                string key = RandomNumberGenerator.GetString(chars, 8);
                user.CheeseChaseKey = key;
                db.SaveChanges();
            }

            await RespondAsync($"Your Cheese Chase key is\n```{user.CheeseChaseKey}```", ephemeral: true);
        }

        [SlashCommand("cheesechase", "Shows top times for the hit game sweeping the nation: Cheese Chase.")]
        public async Task CheeseChase(
            [Choice("Chaos", 0)]
            [Choice("Seeded", 1)]
            [Summary("mode", "The mode to retrieve scores for.")] int mode,
            [Choice("Gouda Garden", 0)]
            [Choice("Mount Cheddarest", 1)]
            [Choice("Fondue Foundry", 2)]
            [Summary("map", "The map to retrieve scores for. Fetches all maps if left blank.")] int map = -1)
        {
            var db = new BotBaseContext();
            var embed = new JifBotEmbedBuilder();
            if (map == -1)
            {
                embed.Title = "Cheeseler Hall of Fame";
                embed.Description = $"{modeMap[mode]} mode";
                embed.ThumbnailUrl = "https://media.discordapp.net/attachments/1409756152127950929/1574173414388670585/logo.png?ex=6b04aebc&is=6b035d3c&hm=33101b6133a763878569dddd87c5c338d9e648d6ddcea5a6a4395606a5205fdf&format=webp&quality=lossless";
                var records = db.CheeseChaseTime.AsQueryable()
                    .Where(e => e.Mode == mode)
                    .OrderBy(e => e.Time)
                    .ToList();

                if (records.Count == 0)
                {
                    await RespondAsync("No records yet! Get cheesing!", ephemeral: true);
                    return;
                }

                var mapGroup = records.GroupBy(e => e.Map);
                foreach (var group in mapGroup)
                {
                    var mapStr = "";
                    var userGroup = group.GroupBy(e => e.UserId).Take(5);
                    foreach (var user in userGroup)
                    {
                        mapStr += BuildString(user.First(), db, true);
                    }
                    embed.AddField(mapMap[group.Key], mapStr);
                }
                await RespondAsync(embed: embed.Build());
            }
            else
            {
                embed.Title = $"Top times for {mapMap[map]} [{modeMap[mode]}]";
                embed.ThumbnailUrl = mapIcon[map];
                var records = db.CheeseChaseTime.AsQueryable()
                    .Where(e => e.Mode == mode && e.Map == map)
                    .OrderBy(e => e.Time)
                    .ToList();

                if (records.Count == 0)
                {
                    await RespondAsync("No records yet! Get cheesing!", ephemeral: true);
                    return;
                }

                var total = records.GroupBy(e => e.UserId);

                var descStr = "";
                foreach(var record in total.Take(5))
                {
                    var top = record.First();
                    descStr += BuildString(top, db, true);
                }
                embed.Description = descStr;

                var characters = records.GroupBy(e => e.Character);
                foreach (var character in characters)
                {
                    var mapStr = "";
                    foreach (var record in character.Take(3))
                    {
                        mapStr += BuildString(record, db);
                    }
                    embed.AddField($"{charEmoji[character.Key]} {charMap[character.Key]}", mapStr, inline: false);
                }
                await RespondAsync(embed: embed.Build());
            }
        }

        public string BuildString(CheeseChaseTime record, BotBaseContext context, bool icon=false)
        {
            TimeSpan time = TimeSpan.FromSeconds(record.Time);
            var user = context.User.Where(e => e.UserId == record.UserId).FirstOrDefault();
            var seed = record.Seed.PadLeft(5, ' ');
            var emojiStr = "";
            if (icon)
            {
                emojiStr = $"{charEmoji[record.Character]} ";
            }
            return $"1. {emojiStr}`{time.ToString(@"mm\:ss\.fff")}` `{seed}` {user.Name}\n";
        }
    }
}
