using System.Threading.Tasks;
using System.Linq;
using Discord;
using Discord.Interactions;
using JifBot.Models;
using System;
using System.IO;
using System.Net;
using System.Collections.Generic;


namespace JifBot.Commands
{
    public class Trials : InteractionModuleBase<SocketInteractionContext>
    {
       [SlashCommand("trial", "Begins a vote to award/deduct server points")]
        public async Task Trial(
       [Summary("user", "The user on trial")] IGuildUser user,
       [Summary("points", "The number of points to add / deduct. Negative number to deduct.")] int points,
       [Summary("reason", "The reason for assigning these points.")] string reason,
       [Summary("image", "An image to be used as evidence.")] IAttachment image = null)
        {
            if (points == 0)
            {
                await RespondAsync("Value must be non-zero.", ephemeral: true);
                return;
            }

            if (image != null && !(image.ContentType.StartsWith("image/")))
            {
                await RespondAsync("Please supply a valid image filetype", ephemeral: true);
                return;
            }

            var db = new BotBaseContext();

            var record = db.CourtRecord.Add(new CourtRecord
            {
                Status = "Pending",
                Points = points,
                Justification = reason,
                DefendantId = user.Id,
                ProsecutorId = Context.User.Id,
                YayVotes = new List<ulong>(),
                NayVotes = new List<ulong>(),
                ServerId = Context.Guild.Id,
                ChannelId = Context.Channel.Id,
                Timestamp = ((DateTimeOffset)DateTime.Now).ToUnixTimeMilliseconds(),
                ImageUrl = image != null ? image.Url : null
            });
            db.SaveChanges();

            var dbUser = db.GetUser(Context.User);

            JifBotEmbedBuilder embed = new JifBotEmbedBuilder();
            embed.PopulateAsTrial(record.Entity, user);

            var builder = new ComponentBuilder()
                .WithButton("Yay", $"yay-{record.Entity.Id}", style: ButtonStyle.Success)
                .WithButton("Nay", $"nay-{record.Entity.Id}", style: ButtonStyle.Danger);

            await RespondAsync(embed: embed.Build(), components: builder.Build());
            var response = await Context.Interaction.GetOriginalResponseAsync();
            record.Entity.MessageId = response.Id;
            db.SaveChanges();
        }

        [SlashCommand("pointboard", "Gets the server points leaderboard.")]
        public async Task Points(
        [Summary("count", "The number of users to display. Defaults to 10, max of 25.")] int count = 10)
        {
            var db = new BotBaseContext();

            var points = db.CourtRecord.AsQueryable()
                .Where(r => r.ServerId == Context.Guild.Id && r.Status == "Approved")
                .GroupBy(r => r.DefendantId)
                .Select(r => new
                {
                    Name = db.User.AsQueryable().Where(u => u.UserId == r.Key).FirstOrDefault().Name,
                    Points = r.Sum(p => p.Points)
                })
                .OrderByDescending(r => r.Points)
                .Take(count)
                .ToList();

            var config = db.ServerConfig.AsQueryable().Where(c => c.ServerId == Context.Guild.Id).FirstOrDefault();
            var pointName = "";
            if (config != null)
                pointName = config.PointName;

            var nameWidth = points.Max(p => p.Name.Length);
            var pointWidth = points.Max(p => p.Points.ToString().Length) + 2;
            var countWidth = points.Count.ToString().Length + 1;

            var msg = "```";
            var curr = 1;
            foreach (var entry in points)
            {
                msg += $"{String.Concat(curr, ".").PadRight(countWidth)} {entry.Name.PadRight(nameWidth)} {entry.Points.ToString().PadLeft(pointWidth)}\n";
                curr++;
            }
            msg += "```";

            JifBotEmbedBuilder builder = new JifBotEmbedBuilder();
            builder.Title = $"{pointName} Point Leaderboard";
            builder.Description = msg;

            await RespondAsync(embed: builder.Build());
        }

        [SlashCommand("records", "Gets the server trial records for a given user.")]
        public async Task Records(
        [Summary("user", "The user to get the records of")] IGuildUser user)
        {
            var db = new BotBaseContext();
            var config = db.ServerConfig.AsQueryable().Where(c => c.ServerId == Context.Guild.Id).FirstOrDefault();
            var pointName = "";
            if (config != null)
                pointName = config.PointName + " ";

            var defendantRecords = db.CourtRecord.AsQueryable()
                .Where(r => r.DefendantId == user.Id && r.ServerId == Context.Guild.Id)
                .OrderByDescending(r => r.Timestamp);
            var prosecutorRecords = db.CourtRecord.AsQueryable()
                .Where(r => r.ProsecutorId == user.Id && r.ServerId == Context.Guild.Id)
                .OrderByDescending(r => r.Timestamp);

            var points = GetPoints(user.Id, Context.Guild.Id);

            JifBotEmbedBuilder builder = new JifBotEmbedBuilder();
            builder.Title = $"{pointName}Point court case records for {user.DisplayName}";
            builder.ThumbnailUrl = user.GetDisplayAvatarUrl();
            builder.AddField("Total Points", points, inline: true);
            builder.AddField("Defendant Trials", defendantRecords.Count(), inline: true);
            builder.AddField("Prosecutor Trials", prosecutorRecords.Count(), inline: true);

            var defendantMsg = "";
            var prosecutorMsg = "";

            foreach (var record in defendantRecords.Take(5).ToList())
            {
                defendantMsg += $"{GetRecordEmoji(record.Status, record.Points)} [Trial](https://discord.com/channels/{record.ServerId}/{record.ChannelId}/{record.MessageId}) [{record.Points}] {record.Justification}\n";
            }

            foreach (var record in prosecutorRecords.Take(5).ToList())
            {
                prosecutorMsg += $"{GetRecordEmoji(record.Status, record.Points)} [Trial](https://discord.com/channels/{record.ServerId}/{record.ChannelId}/{record.MessageId}) [{record.Points}] {record.Justification}\n";
            }

            if (defendantMsg.Length > 0)
                builder.AddField("Defendant [Last 5]", defendantMsg);
            if (prosecutorMsg.Length > 0)
                builder.AddField("Prosecutor [Last 5]", prosecutorMsg);

            await RespondAsync(embed: builder.Build());
        }

        [SlashCommand("trialconfig", "Sets server configuration values for trials.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task TrialConfig(
            [Summary("pointname", "The name of points to be used for this server")] string name,
            [Summary("votecount", "The number of votes necessary for a trial to pass.")] int count)
        {
            if (count <= 0)
            {
                await RespondAsync("Vote Count must be greater than 0.", ephemeral: true);
                return;
            }

            var db = new BotBaseContext();
            var channel = Context.Channel as ITextChannel;
            var config = db.ServerConfig.AsQueryable().Where(s => s.ServerId == channel.Guild.Id).FirstOrDefault();

            if (config != null)
            {
                config.PointName = name;
                config.TrialCount = count;
            }
            else
            {
                config = new ServerConfig { ServerId = channel.GuildId, PointName = name, TrialCount = count };
                db.Add(config);
            }
            db.SaveChanges();
            await RespondAsync("Values set successfully", ephemeral: true);
        }
        private int GetPoints(ulong userId, ulong guildId)
        {
            var db = new BotBaseContext();
            return db.CourtRecord.AsQueryable()
                .Where(r => r.DefendantId == userId && r.ServerId == guildId && r.Status == "Approved")
                .Sum(r => r.Points);
        }

        private string GetRecordEmoji(string status, int points)
        {
            switch (status)
            {
                case "Approved":
                    if (points > 0)
                        return "😇";
                    else
                        return "👿";
                case "Denied":
                    return "❌";
                default:
                    return "🤔";
                }
        }
    }
}
