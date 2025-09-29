using Discord;
using JifBot.Interfaces;
using System;
using System.Linq;
using static JifBot.Interfaces.RiotInterface;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JifBot.Embeds
{
    public class RiotEmbedBuilder : JifBotEmbedBuilder
    {
        private int masteryCount = 1;
        public void AddMatch(RiotInterface.Match match, string puuid)
        {
            var participant = match.participants.Where(p => p.puuid == puuid).First();
            string outcome = participant.win ? "✅" : "❌";
            var kd = Math.Round((double)(participant.kills + participant.assists) / (double)participant.deaths, 2);
            var titleEntry = $"{outcome} {participant.champion.name} {participant.position}";

            DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeMilliseconds(match.gameStartTimestamp);
            string date = timestamp.LocalDateTime.ToShortDateString();

            TimeSpan timeSpan = TimeSpan.FromSeconds(match.gameDuration);
            string gameDuration = string.Format("{0:###}:{1:D2}", timeSpan.Minutes + timeSpan.Hours * 60, timeSpan.Seconds);

            var matchEntry = $"{date} ({gameDuration})";
            matchEntry += $"\n> **{participant.kills}/{participant.deaths}/{participant.assists}** ({kd})";
            matchEntry += $"\nKP: {Math.Round(participant.challenges.killParticipation * 100, 2)}%";
            matchEntry += $"\nDmg: {participant.totalDamageDealtToChampions:n0}";
            matchEntry += $"\nGPM: {Math.Round(participant.challenges.goldPerMinute, 2)}";
            matchEntry += $"\nCS: {participant.totalMinionsKilled}";
            matchEntry += $"\nVision: {participant.visionScore}";
            matchEntry += "\n";

            if (participant.enemyMissingPings > 0)
                matchEntry += $"<:Enemy_Missing_ping:1241517862745800884> {participant.enemyMissingPings} ";

            if (participant.onMyWayPings > 0)
                matchEntry += $"<:On_My_Way_ping:1241517861113958550> {participant.onMyWayPings} ";

            if (participant.assistMePings > 0)
                matchEntry += $"<:Assist_Me_ping:1241517861835374644> {participant.assistMePings} ";

            if (participant.getBackPings > 0)
                matchEntry += $"<:Retreat_ping:1241517860015046796> {participant.getBackPings} ";

            AddField(titleEntry, matchEntry, inline: true);
        }
        public void AddMastery(Mastery mastery)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(mastery.lastPlayTime / 1000);
            string date = dateTime.ToLocalTime().ToShortDateString();
            string descText = "";
            descText += $"Level: {mastery.championLevel}";
            descText += $"\n{mastery.championPoints:n0} points";
            descText += $"\nPlayed: {date}";
            AddField($"{masteryCount}. {mastery.champion.name}", descText, inline: true);
            masteryCount++;
        }
    }
}
