﻿using System.Threading.Tasks;
using System.Linq;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using JifBot.Models;
using JIfBot;
using System.Text.RegularExpressions;
using System.Net;

namespace JifBot.Commands
{
    public class Customization : InteractionModuleBase<SocketInteractionContext>
    {       
        [SlashCommand("togglereactions","Toggles between enabling and disabling Jif Bot reactions.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ToggleReactions(
            [Summary("channel", "The discord channel to toggle. If not specified, applies to ALL channels.")] ITextChannel channel = null)
        {
            var db = new BotBaseContext();
            if (channel == null)
            {
                var channels = db.ReactionBan.AsQueryable().Where(c => c.ServerId == Context.Guild.Id).ToList();
                if (channels.Count == 0)
                {
                    // Set the channel id to the server id to indicate it is a server-wide disable. This is hacky bullshit and should be reconsidered.
                    db.Add(new ReactionBan { ChannelId = Context.Guild.Id, ServerId = Context.Guild.Id, ChannelName = Context.Guild.Name });
                    await RespondAsync("Reactions are now disabled for all available channels in this server");
                }
                else
                {
                    foreach (var c in channels)
                        db.Remove(c);
                    await RespondAsync("Reactions are now enabled for all channels in this server");
                }
            }
            else
            {
                var dbchannel = db.ReactionBan.AsQueryable().Where(s => s.ChannelId == channel.Id).FirstOrDefault();
                if (dbchannel == null)
                {
                    db.Add(new ReactionBan { ChannelId = channel.Id, ServerId = channel.GuildId, ChannelName = channel.Name });
                    await RespondAsync($"Reactions are now disabled for {channel.Name}");
                }
                else
                {
                    db.ReactionBan.Remove(dbchannel);
                    await RespondAsync($"Reactions are now enabled for {channel.Name}");
                }
            }
            db.SaveChanges();
        }

        [SlashCommand("setreport", "Sets a channel to send messages to for events in the server, or removes the current ones.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetReport(
            [Choice("User Join", "Join")]
            [Choice("User Leave", "Leave")]
            [Choice("Message Delete", "Delete")]
            [Summary("report", "The type of report you wish to set up.")] string report,
            [Summary("channel", "The channel to put the messages in. Defaults to current channel if unsepcified.")] ITextChannel channel = null,
            [Summary("delete", "Specifies to delete the current report. Defaults to false.")] bool delete = false)
        {
            if (channel == null)
            {
                channel = Context.Channel as ITextChannel;
            }

            var db = new BotBaseContext();
            var config = db.ServerConfig.AsQueryable().Where(s => s.ServerId == channel.Guild.Id).FirstOrDefault();
            if (config != null)
            {
                if (delete)
                {
                    SetConfigValue(report, config, 0);
                    await RespondAsync($"{report} messages will no longer be sent.");

                }
                else if (GetConfigValue(report, config) == channel.Id)
                {
                    await RespondAsync($"{report} messages already being sent in this channel!", ephemeral: true);
                }
                else
                {
                    SetConfigValue(report, config, channel.Id);
                    await RespondAsync($"{report} messages will now be sent in {channel.Name}.");
                }
            }
            else
            {
                if (delete)
                {
                    await RespondAsync($"{report} messages will not be sent.");
                }
                else
                {
                    config = new ServerConfig { ServerId = channel.GuildId };
                    SetConfigValue(report, config, channel.Id);
                    db.Add(config);
                    await RespondAsync($"{report} messages will now be sent in {channel.Name}");
                }
            }
            db.SaveChanges();
        }
        
        [SlashCommand("placereactmessage","Places a message in a specified channel that users can react to to assign themselves roles.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task PlaceReactMessage(
            [Summary("channel", "The channel to put the message in. Defaults to current channel if unsepcified.")] ITextChannel channel = null,
            [Summary("delete", "Specifies to delete the current message. Defaults to false.")] bool delete = false)
        {
            if (channel == null)
            {
                channel = Context.Channel as ITextChannel;
            }

            var db = new BotBaseContext();
            var config = db.GetServerConfig(Context.Guild);
            
            if(delete)
            {
                if (config.ReactMessageId != 0 && config.ReactChannelId != 0)
                {
                    var oldchannel = Context.Guild.GetTextChannel(config.ReactChannelId);
                    var msg = await oldchannel.GetMessageAsync(config.ReactMessageId);
                    await msg.DeleteAsync();
                    config.ReactChannelId = 0;
                    config.ReactMessageId = 0;
                    db.SaveChanges();
                    await RespondAsync("Message removed. Any withstanding roles will remain.");
                    return;
                }
                await RespondAsync("Message does not exist, or cannot be found. Not deleting.", ephemeral: true);
                return;
            }

            var message = await channel.SendMessageAsync(embed: BuildReactMessage(Context.Guild));

            await RespondAsync("Message placed!", ephemeral: true);

            if (config.ReactChannelId != 0)
            {
                var oldChannel = Context.Guild.GetTextChannel(config.ReactChannelId);
                if (oldChannel != null)
                {
                    var msg = await oldChannel.GetMessageAsync(config.ReactMessageId);
                    if (msg != null)
                        await msg.DeleteAsync();
                }
            }

            config.ReactMessageId = message.Id;
            config.ReactChannelId = message.Channel.Id;

            db.SaveChanges();

            var roles = db.ReactRole.AsQueryable().Where(r => r.ServerId == Context.Guild.Id).DefaultIfEmpty();
            foreach (var role in roles)
            {
                var botConfig = db.Configuration.AsQueryable().Where(b => b.Name == Program.configName).First();
                var bot = Context.Guild.GetUser(botConfig.Id);

                // Emojis and Discord Emotes are handled differently
                if (Regex.IsMatch(role.Emote, @"(\u00a9|\u00ae|[\u2000-\u3300]|\ud83c[\ud000-\udfff]|\ud83d[\ud000-\udfff]|\ud83e[\ud000-\udfff])"))
                {
                    Emoji dEmoji = new Emoji(role.Emote);
                    await message.AddReactionAsync(dEmoji);
                }
                else
                {
                    Emote dEmote = Emote.Parse(role.Emote);
                    await message.AddReactionAsync(dEmote);
                }
            }
        }
        
        [SlashCommand("reactrole", "Assigns role-reaction pairings to show in the message sent by /placereactmessage.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ReactRole(
            [Summary("role", "The desired role. Jif Bot must be higher than the role to be able to assign it.")] SocketRole role,
            [Summary("emoji", "The emoji to represent the role.")] string emote,
            [Summary("description", "A description to sit beneath this role-reaction pairing.")] string description = "",
            [Summary("delete", "Specifies to delete the provided role-reaction pairing.")] bool delete = false)
        {
            // Check to ensure the role is valid
            var db = new BotBaseContext();
            Emote dEmote;
            ulong roleId = role.Id;

            var react = db.ReactRole.AsQueryable().Where(s => s.RoleId == roleId).FirstOrDefault();

            // specified to delete the react role
            if (delete)
            {
                if (react != null)
                {
                    db.Remove(react);
                    db.SaveChanges();
                    await RespondAsync("Role removed");
                }
                else
                {
                    await RespondAsync("Role does not exist. Not removing.", ephemeral: true);
                    return;
                }
            }
            else
            {
                // Check to ensure the emote is in a valid format
                if (!Regex.IsMatch(emote, @"(\u00a9|\u00ae|[\u2000-\u3300]|\ud83c[\ud000-\udfff]|\ud83d[\ud000-\udfff]|\ud83e[\ud000-\udfff])") && !Emote.TryParse(emote, out dEmote))
                {
                    await RespondAsync("Not a valid reaction emote", ephemeral: true);
                    return;
                }

                var rRole = db.ReactRole.AsQueryable().Where(r => r.Emote == emote && r.ServerId == Context.Guild.Id).FirstOrDefault();

                // if there exists an entry in this server with the specified emote that is not the specified role
                if (rRole != null && rRole.RoleId != roleId)
                {
                    await RespondAsync("Emote already being used", ephemeral: true);
                    return;
                }

                // if there is no entry for this role
                else if (react == null)
                {
                    db.Add(new ReactRole { RoleId = roleId, Emote = emote, Description = description, ServerId = Context.Guild.Id });
                    db.SaveChanges();
                    await RespondAsync("Role Added");
                    react = db.ReactRole.AsQueryable().Where(s => s.RoleId == roleId).FirstOrDefault();
                }

                // update emote and description
                else
                {
                    react.Emote = emote;
                    react.Description = description;
                    db.SaveChanges();
                    await RespondAsync($"Updated entry for {role.Name}");
                }
            }

            var config = db.ServerConfig.AsQueryable().Where(s => s.ServerId == Context.Guild.Id).FirstOrDefault();
            var channel = Context.Guild.GetTextChannel(config.ReactChannelId);
            var msg = (IUserMessage)await channel.GetMessageAsync(config.ReactMessageId);
            await msg.ModifyAsync(m => m.Embed = BuildReactMessage(Context.Guild));

            var botConfig = db.Configuration.AsQueryable().Where(b => b.Name == Program.configName).First();
            var bot = Context.Guild.GetUser(botConfig.Id);

            // Emojis and Discord Emotes are handled differently
            if (Regex.IsMatch(react.Emote, @"(\u00a9|\u00ae|[\u2000-\u3300]|\ud83c[\ud000-\udfff]|\ud83d[\ud000-\udfff]|\ud83e[\ud000-\udfff])"))
            {
                Emoji dEmoji = new Emoji(react.Emote);
                if (emote == "-d")
                    await msg.RemoveReactionAsync(dEmoji, bot);
                else
                    await msg.AddReactionAsync(dEmoji);
            }
            else
            {
                dEmote = Emote.Parse(react.Emote);
                if (emote == "-d")
                    await msg.RemoveReactionAsync(dEmote, bot);
                else
                    await msg.AddReactionAsync(dEmote);
            }
        }

        [SlashCommand("manageqotd", "Creates, modifies or deletes a forum channel for automated qotds from user submissions.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ManageQotd(
            [Choice("Set", "s")]
            [Choice("Delete", "d")]
            [Summary("action", "The action to take")] string action,
            [Summary("forum", "The forum to post QOTDs in.")] SocketChannel channel,
            [Summary("role", "The role to ping when a QOTD is posted. Leave blank for no ping.")] IRole role = null)
        {
            var db = new BotBaseContext();
            var serverConfig = db.GetServerConfig(Context.Guild);

            if (action == "d")
            {
                var thread = Context.Guild.GetThreadChannel(serverConfig.QotdThreadId);
                if (thread != null)
                {
                    await thread.DeleteAsync();
                }

                serverConfig.QotdThreadId = 0;
                serverConfig.QotdForumId = 0;
                serverConfig.QotdRoleId = 0;

                await RespondAsync("QOTD Deleted!", ephemeral: true);
                return;
            }
            else if (action == "s")
            {
                var type = channel.GetChannelType();
                if (type != ChannelType.Forum)
                {
                    await RespondAsync("Channel must be a forum channel.", ephemeral: true);
                    return;
                }

                IForumChannel forum = (IForumChannel)channel;
                var thread = Context.Guild.GetThreadChannel(serverConfig.QotdThreadId) as IThreadChannel;
                var post = thread != null ? await thread.GetMessageAsync(thread.Id) as IUserMessage : null;
                var embed = new JifBotEmbedBuilder();
                embed.PopulateAsQotd(Context.Guild.Id);

                var component = new ComponentBuilder()
                    .WithButton("Submit a QOTD!", "qotd-submit", style: ButtonStyle.Success)
                    .WithButton("Subscribe", "qotd-role-add", style: ButtonStyle.Primary)
                    .WithButton("Unsubscribe", "qotd-role-remove", style: ButtonStyle.Secondary);

                var msgVerb = "";

                // Either we're moving the post, or the old one is missing
                if ((channel.Id != serverConfig.QotdForumId) || post == null)
                {
                    msgVerb = "Created";
                    if (thread != null)
                    {
                        await thread.DeleteAsync();
                    }

                    thread = await forum.CreatePostAsync("Submit a QOTD!", embed: embed.Build(), components: component.Build());

                    serverConfig.QotdThreadId = thread.Id;
                    serverConfig.QotdForumId = forum.Id;
                    if (role != null)
                        serverConfig.QotdRoleId = role.Id;
                }
                else
                {
                    msgVerb = "Modified";
                    // Only here to retroactively add components and honestly this can be deleted later
                    await post.ModifyAsync(msg =>
                    {
                        msg.Embed = embed.Build();
                        msg.Components = component.Build();
                    });

                    if (role != null)
                        serverConfig.QotdRoleId = role.Id;
                }

                db.SaveChanges();

                await RespondAsync($"{msgVerb} post: {thread.Mention}", ephemeral: true);
            }
        }

        [SlashCommand("submitqotd", "Submits a qotd question for this server.")]
        public async Task SubmitQotd(
            [Summary("question", "Your question of the day!")] string question,
            [Summary("image", "An image to accompany your question (if any)")] IAttachment image = null)
        {
            byte[] imageBytes = null;
            if (image != null)
            {
                if (!image.ContentType.StartsWith("image/"))
                {
                    await RespondAsync("Please supply a valid image filetype", ephemeral: true);
                    return;
                }
                var client = new WebClient();
                imageBytes = client.DownloadData(image.Url);
            }

            var db = new BotBaseContext();
            var config = db.ServerConfig.AsQueryable().Where(c => c.ServerId == Context.Guild.Id).FirstOrDefault();
            db.Add(new Qotd { 
                Question = question,
                ServerId = Context.Guild.Id,
                UserId = Context.User.Id,
                Image = imageBytes,
                ImageType = image != null ? image.ContentType.Replace("image/", "") : ""
            });
            db.SaveChanges();

            if (config != null && config.QotdThreadId != 0)
            {
                var thread = Context.Guild.GetThreadChannel(config.QotdThreadId);
                var post = await thread.GetMessageAsync(thread.Id) as IUserMessage;
                var embed = new JifBotEmbedBuilder();
                embed.PopulateAsQotd(Context.Guild.Id);
                await post.ModifyAsync(msg => msg.Embed = embed.Build());
            }
            await RespondAsync("Question recorded. Thank you!", ephemeral: true);
        }

        private ulong GetConfigValue(string field, ServerConfig config)
        {
            switch (field)
            {
                case "Join":
                    return config.JoinId;
                case "Leave":
                    return config.LeaveId;
                case "Delete":
                    return config.MessageId;
            }
            return 0;
        }

        private void SetConfigValue(string field, ServerConfig config, ulong value)
        {
            switch (field)
            {
                case "Join":
                    config.JoinId = value;
                    break;
                case "Leave":
                    config.LeaveId = value;
                    break;
                case "Delete":
                    config.MessageId = value;
                    break;
            }
        }

        public Embed BuildReactMessage(SocketGuild server)
        {
            var db = new BotBaseContext();
            var embed = new JifBotEmbedBuilder();
            embed.Title = "React with the following emojis to receive the corresponding role. Remove the reaction to remove the role.";
            embed.Description = "";

            var roles = db.ReactRole.AsQueryable().Where(r => r.ServerId == server.Id).DefaultIfEmpty();
            var config = db.Configuration.AsQueryable().Where(c => c.Name == Program.configName).First();

            if (roles.First() == null)
            {
                embed.Description = $"No emotes currently set. Use {config.Prefix}reactrole to set roles";
            }
            else
            {
                foreach (var role in roles)
                {
                    var dRole = server.GetRole(role.RoleId);
                    embed.Description += $"{role.Emote} -- {dRole.Mention}";
                    if (role.Description != "")
                        embed.Description += $"\n     {role.Description}";
                    embed.Description += "\n\n";
                }
            }

            return embed.Build();
        }
    }
}
