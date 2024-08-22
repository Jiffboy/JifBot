using System.Threading.Tasks;
using System.Linq;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using JifBot.Models;
using JIfBot;
using System.Text.RegularExpressions;
using System.IO;
using System.Net;

namespace JifBot.Commands
{
    public class Customization : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("message", "Displays the message set with /setmessage.")]
        public async Task Message()
        {
            var db = new BotBaseContext();
            var message = db.Message.AsQueryable().Where(msg => msg.UserId == Context.User.Id).FirstOrDefault();
            var config = db.Configuration.AsQueryable().Where(cfg => cfg.Name == Program.configName).First();
            if (message == null)
                await RespondAsync($"User does not have a message yet! use {config.Prefix}setmessage to set a message.", ephemeral: true);
            else
                await RespondAsync(message.Message1);
        }

        [SlashCommand("setmessage","Sets a message that can be displayed using /message.")]
        public async Task SetMessage(
            [Summary("message", "The message you would like to set. Must be text.")] string newmessage)
        {
            var db = new BotBaseContext();
            var message = db.Message.AsQueryable().Where(msg => msg.UserId == Context.User.Id).FirstOrDefault();
            var user = db.User.AsQueryable().Where(usr => usr.UserId == Context.User.Id).FirstOrDefault();
            if (user == null)
                db.Add(new User { UserId = Context.User.Id, Name = Context.User.Username, Number = long.Parse(Context.User.Discriminator) });
            else
            {
                user.Name = Context.User.Username;
                user.Number = long.Parse(Context.User.Discriminator);
            }
            if (message == null)
                db.Add(new Message { UserId = Context.User.Id, Message1 = newmessage });
            else
            {
                await RespondAsync($"Replacing old message:\n{message.Message1}");
                message.Message1 = newmessage;
            }
            db.SaveChanges();
            await RespondAsync("Message Added!");
        }
        
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
            var config = db.ServerConfig.AsQueryable().Where(s => s.ServerId == Context.Guild.Id).FirstOrDefault();
            
            if(delete)
            {
                if (config != null && config.ReactMessageId != 0 && config.ReactChannelId != 0)
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

            if (config == null)
            {
                db.Add(new ServerConfig { ServerId = Context.Guild.Id, ReactMessageId = message.Id, ReactChannelId = message.Channel.Id });
            }
            else 
            {
                if (config.ReactChannelId != 0)
                {
                    var oldChannel = Context.Guild.GetTextChannel(config.ReactChannelId);
                    var msg = await oldChannel.GetMessageAsync(config.ReactMessageId);
                    await msg.DeleteAsync();
                }

                config.ReactMessageId = message.Id;
                config.ReactChannelId = message.Channel.Id;
            }
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

        [SlashCommand("optin", "Opts in or out of the collection of Jif Bot usage data. By default, everyone is opted out.")]
        public async Task OptIn(
            [Choice("IN", "in")]
            [Choice("OUT AND KEEP DATA", "out")]
            [Choice("OUT AND DELETE DATA", "delete")]
            [Summary("opt", "Which action to take for future data collection.")] string scope)
        {

            var db = new BotBaseContext();
            var user = db.User.AsQueryable().AsQueryable().Where(user => user.UserId == Context.User.Id).FirstOrDefault();
            switch (scope)
            {
                case "in":
                    if (user == null)
                        db.Add(new User { UserId = Context.User.Id, Name = Context.User.Username, Number = long.Parse(Context.User.Discriminator), DataAllowed = true });
                    else
                        user.DataAllowed = true;
                    db.SaveChanges();
                    await RespondAsync("Successfully opted in! Jif Bot will now track usage statistics, which can be viewed with the stats command. This is not retroactive, and will only track future usage.", ephemeral: true);
                    break;
                case "delete":
                    if (user == null)
                    {
                        await RespondAsync("User already opted out.", ephemeral: true);
                    }
                    else
                    {
                        var entries = db.CommandCall.AsQueryable().Where(e => e.UserId == Context.User.Id).ToList();
                        foreach (var entry in entries)
                        {
                            entry.UserId = 0;
                        }
                        user.DataAllowed = false;
                        db.SaveChanges();
                        await RespondAsync("All existing entries deleted, and opted out for the future", ephemeral: true);
                    }
                    break;
                case "out":
                    if(user == null)
                    {
                        await RespondAsync("User already opted out.", ephemeral: true);
                    }
                    else
                    {
                        user.DataAllowed = false;
                        db.SaveChanges();
                        await RespondAsync("User opted out for the future. Existing entries maintained", ephemeral: true);
                        
                    }
                    break;
            }
        }

        [SlashCommand("managecharacter", "Manages characters saved to the blorbopedia")]
        public async Task ManageCharacter(
            [Choice("Add", "add")]
            [Choice("Modify", "modify")]
            [Choice("Delete", "delete")]
            [Summary("action", "The action to perform for the specified character")] string action,
            [Summary("character-key", "The key to look up your character. (Please use first name /nickname)")] string key,
            [Summary("name", "The character's full name to be displayed")] string name = "",
            [Summary("image", "An image to be used to display the character.")] IAttachment image = null,
            [Summary("title", "The characters title. i.e. 'The Savior of Eorzea'")] string title = "",
            [Summary("occupation", "The character's occupation")] string occupation = "",
            [Summary("age", "The characeter's age")] string age = "",
            [Summary("race", "The character's race")] string race = "",
            [Summary("pronouns", "The characeter's pronouns")] string pronouns = "",
            [Summary("sexuality", "The character's sexuality")] string sexuality = "",
            [Summary("origin", "Where the character is from")] string origin = "",
            [Summary("residence", "Where the character currently resides")] string residence = "",
            [Summary("universe", "The universe the character belongs to. (ffxiv, dnd, etc)")] string universe = "",
            [Summary("additional-resources", "Links for resources on the character. (carrd, lore doc, etc)")] string resources = "",
            [Choice("Default", "default")]
            [Choice("Compact", "compact")]
            [Choice("Expanded", "expanded")]
            [Summary("compact-view", "Specifies whether the image displays big or small in /blorbopedia")] string compact = "default")
        {
            key = key.ToLower();
            var db = new BotBaseContext();

            if (image != null && !(image.ContentType.StartsWith("image/")))
            {
                await RespondAsync("Please supply an image as a .png, .jpg, or .jpeg", ephemeral: true);
                return;
            }

            if (action == "add")
            {
                var user = db.User.AsQueryable().AsQueryable().Where(user => user.UserId == Context.User.Id).FirstOrDefault();
                if (user == null)
                    db.Add(new User { UserId = Context.User.Id, Name = Context.User.Username, Number = long.Parse(Context.User.Discriminator) });
                var character = db.Character.AsQueryable().AsQueryable().Where(c => c.Key == key).FirstOrDefault();
                if (character != null)
                {
                    await RespondAsync("Character already exists with this character-key. Please choose another", ephemeral: true);
                    return;
                }

                db.Add(new Character
                {
                    Key = key,
                    UserId = Context.User.Id,
                    Name = name,
                    Description = "[No description provided]",
                    Title = title,
                    Occupation = occupation,
                    Age = age,
                    Race = race,
                    Pronouns = pronouns,
                    Sexuality = sexuality,
                    Origin = origin,
                    Residence = residence,
                    Universe = universe,
                    Resources = resources,
                    CompactImage = compact != "expanded",
                    Image = image != null ? GetBytesFromAttachment(image) : null,
                    ImageType = image != null ? image.ContentType.Replace("image/", "") : ""
                });
                db.SaveChanges();
                var mb = new ModalBuilder()
                    .WithTitle("Now tell us about them!")
                    .WithCustomId($"add_character:{key}")
                    .AddTextInput("Description / Backstory", "description", TextInputStyle.Paragraph);
                await Context.Interaction.RespondWithModalAsync(mb.Build());
            }
            else if (action == "modify")
            {
                var character = db.Character.AsQueryable().Where(c => c.Key == key).FirstOrDefault();
                if (character == null)
                {
                    await RespondAsync("Character key does not exist. Please try again", ephemeral: true);
                    return;
                }
                if (character.UserId != Context.User.Id)
                {
                    await RespondAsync("That character does not belong to you! Hands off!", ephemeral: true);
                    return;
                }
                if (name != "")
                    character.Name = name;
                if (title != "")
                    character.Title = title;
                if (occupation != "")
                    character.Occupation = occupation;
                if (age != "")
                    character.Age = age;
                if (race != "")
                    character.Race = race;
                if (pronouns != "")
                    character.Pronouns = pronouns;
                if (sexuality != "")
                    character.Sexuality = sexuality;
                if (origin != "")
                    character.Origin = origin;
                if (residence != "")
                    character.Residence = residence;
                if (universe != "")
                    character.Universe = universe;
                if (resources != "")
                    character.Resources = resources;
                if (compact != "default")
                    character.CompactImage = compact != "expanded";
                if (image != null)
                {
                    character.Image = GetBytesFromAttachment(image);
                    character.ImageType = image.ContentType.Replace("image/", "");
                }
                db.SaveChanges();
                var mb = new ModalBuilder()
                    .WithTitle("Changes saved!")
                    .WithCustomId($"modify_character:{key}")
                    .AddTextInput("Description / Backstory", "description", TextInputStyle.Paragraph, placeholder: "If you wish to modify the description, copy/paste it. Otherwise, hit cancel to keep as-is. Sorry :(");
                await Context.Interaction.RespondWithModalAsync(mb.Build());
            }
            else if (action == "delete")
            {
                var character = db.Character.AsQueryable().Where(c => c.Key == key).FirstOrDefault();
                if (character == null)
                    await RespondAsync("That character does not exist!", ephemeral: true);
                else
                {
                    db.Character.Remove(character);
                    db.SaveChanges();
                    await RespondAsync("Character removed successfully", ephemeral: true);
                }    
            }
        }

        private byte[] GetBytesFromAttachment(IAttachment attachment)
        {
            var client = new WebClient();
            return client.DownloadData(attachment.Url);
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
