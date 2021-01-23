using System.Threading.Tasks;
using System.Linq;
using System;
using Discord;
using Discord.Commands;
using JifBot.Models;
using JIfBot;
using System.Text.RegularExpressions;

namespace JifBot.Commands
{
    public class Customization : ModuleBase
    {
        [Command("message")]
        [Remarks("-c-")]
        [Summary("Displays your previously set message. To set a message, use the -p-setmessage command.")]
        public async Task Message()
        {
            var db = new BotBaseContext();
            var message = db.Message.AsQueryable().Where(msg => msg.UserId == Context.User.Id).FirstOrDefault();
            var config = db.Configuration.AsQueryable().Where(cfg => cfg.Name == Program.configName).First();
            if (message == null)
                await ReplyAsync($"User does not have a message yet! use {config.Token}setmessage to set a message.");
            else
                await ReplyAsync(message.Message1);
        }

        [Command("setmessage")]
        [Remarks("-c- This is my message")]
        [Alias("resetmessage")]
        [Summary("Allows you to set a message that can be displayed at any time using the -p-message command.")]
        public async Task SetMessage([Remainder] string mess)
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
                db.Add(new Message { UserId = Context.User.Id, Message1 = mess });
            else
            {
                await ReplyAsync("Replacing old message:");
                await ReplyAsync(message.Message1);
                message.Message1 = mess;
            }
            db.SaveChanges();
            await ReplyAsync("Message Added!");
        }

        [Command("togglesignature")]
        [Remarks("-c- :fox:")]
        [Alias("resetsignature", "setsignature")]
        [Summary("Sets for a specific emote to be reacted to every message you send. To remove a signature, call the command without specifying an emote, or using the emote you already have set. NOTE: Jif Bot does NOT have nitro, this will only work with emotes that are available on this server.")]
        public async Task ToggleSignature([Remainder] string sig = "")
        {
            var db = new BotBaseContext();
            var signature = db.Signature.AsQueryable().Where(s => s.UserId == Context.User.Id).FirstOrDefault();
            var user = db.User.AsQueryable().Where(usr => usr.UserId == Context.User.Id).FirstOrDefault();
            sig = sig.Replace("<", string.Empty);
            sig = sig.Replace(">", string.Empty);
            if (user == null)
                db.Add(new User { UserId = Context.User.Id, Name = Context.User.Username, Number = long.Parse(Context.User.Discriminator) });
            else
            {
                user.Name = Context.User.Username;
                user.Number = long.Parse(Context.User.Discriminator);
            }
            if (sig == "")
            {
                if (signature == null)
                {
                    await ReplyAsync("User does not have a signature to remove. Doing nothing.");
                    return;
                }
                db.Signature.Remove(signature);
            }
            else
            {
                if (signature == null)
                    db.Add(new Signature { UserId = Context.User.Id, Signature1 = sig });
                else if (signature.Signature1 == sig)
                    db.Signature.Remove(signature);
                else
                    signature.Signature1 = sig;
            }
            db.SaveChanges();
            await ReplyAsync("Signature updated.");

        }

        [Command("togglereactions")]
        [Remarks("-c-, -c- all")]
        [Summary("Toggles between enabling and disabling reactions for the channel the command was issued in. Reactions are set keywords that Jif Bot will respond to. This does not include commands. To disable/enable for all channels, follow the command with \"all\". If there is at least one channel in which reactions are disabled when using \"all\", all channels will be enabled, otherwise, all will be disabled. Only the server owner can execute this command.")]
        public async Task ToggleReactions([Remainder] string args = "")
        {
            if (Context.Guild.OwnerId != Context.User.Id)
            {
                await ReplyAsync("Command can only be used by server owner");
                return;
            }

            var db = new BotBaseContext();
            if (args.ToLower().Contains("all"))
            {
                var channels = db.ReactionBan.AsQueryable().Where(c => c.ServerId == Context.Guild.Id).ToList();
                if (channels.Count == 0)
                {
                    foreach (var c in await Context.Guild.GetTextChannelsAsync())
                        db.Add(new ReactionBan { ChannelId = c.Id, ServerId = Context.Guild.Id, ChannelName = c.Name });
                    await ReplyAsync("Reactions are now disabled for all currently available channels in the server");
                }
                else
                {
                    foreach (var c in channels)
                        db.Remove(c);
                    await ReplyAsync("Reactions are now enabled for all channels in this server");
                }
            }
            else
            {
                var channel = db.ReactionBan.AsQueryable().Where(s => s.ChannelId == Context.Channel.Id).FirstOrDefault();
                if (channel == null)
                {
                    db.Add(new ReactionBan { ChannelId = Context.Channel.Id, ServerId = Context.Guild.Id, ChannelName = Context.Channel.Name });
                    await ReplyAsync($"Reactions are now disabled for {Context.Channel.Name}");
                }
                else
                {
                    db.ReactionBan.Remove(channel);
                    await ReplyAsync($"Reactions are now enabled for {Context.Channel.Name}");
                }
            }
            db.SaveChanges();
        }

        [Command("setwelcome")]
        [Remarks("-c-")]
        [Summary("Sets a channel to send messages to when new users join the server. To remove, issue the command in the channel the welcome is currently set to. Only the server owner can execute this command.")]
        public async Task SetWelcome([Remainder] string args = "")
        {
            if (Context.Guild.OwnerId != Context.User.Id)
            {
                await ReplyAsync("Command can only be used by server owner");
                return;
            }

            var db = new BotBaseContext();
            var config = db.ServerConfig.AsQueryable().Where(s => s.ServerId == Context.Guild.Id).FirstOrDefault();
            if (config != null)
            {
                if (config.JoinId == Context.Channel.Id)
                {
                    config.JoinId = 0;
                    await ReplyAsync($"Welcome messages will no longer be sent in {Context.Channel.Name}");
                }
                else
                {
                    var old = await Context.Guild.GetTextChannelAsync(config.JoinId);
                    config.JoinId = Context.Channel.Id;
                    if (old != null)
                        await ReplyAsync($"Welcome messages will no longer be sent in {old.Name}, will now be sent in {Context.Channel.Name}");
                    else
                        await ReplyAsync($"Welcome messages will now be sent in {Context.Channel.Name}");
                }
            }
            else
            {
                db.Add(new ServerConfig { ServerId = Context.Guild.Id, JoinId = Context.Channel.Id });
                await ReplyAsync($"Welcome messages will now be sent in {Context.Channel.Name}");
            }
            db.SaveChanges();
        }

        [Command("setgoodbye")]
        [Remarks("-c-")]
        [Summary("Sets a channel to send messages to when users leave the server. To remove, issue the command in the channel the goodbye is currently set to. Only the server owner can execute this command.")]
        public async Task SetGoodbye([Remainder] string args = "")
        {
            if (Context.Guild.OwnerId != Context.User.Id)
            {
                await ReplyAsync("Command can only be used by server owner");
                return;
            }

            var db = new BotBaseContext();
            var config = db.ServerConfig.AsQueryable().Where(s => s.ServerId == Context.Guild.Id).FirstOrDefault();
            if (config != null)
            {
                if (config.LeaveId == Context.Channel.Id)
                {
                    config.LeaveId = 0;
                    await ReplyAsync($"Goodbye messages will no longer be sent in {Context.Channel.Name}");
                }
                else
                {
                    var old = await Context.Guild.GetTextChannelAsync(config.LeaveId);
                    config.LeaveId = Context.Channel.Id;
                    if (old != null)
                        await ReplyAsync($"Goodbye messages will no longer be sent in {old.Name}, will now be sent in {Context.Channel.Name}");
                    else
                        await ReplyAsync($"Goodbye messages will now be sent in {Context.Channel.Name}");
                }
            }
            else
            {
                db.Add(new ServerConfig { ServerId = Context.Guild.Id, LeaveId = Context.Channel.Id });
                await ReplyAsync($"Goodbye messages will now be sent in {Context.Channel.Name}");
            }
            db.SaveChanges();
        }

        [Command("setsnoop")]
        [Remarks("-c-")]
        [Summary("Sets a channel to send messages to whenever a message gets deleted in the server. To remove, issue the command in the channel the goodbye is currently set to. Only the server owner can execute this command.")]
        public async Task SetMessageReport([Remainder] string args = "")
        {
            if (Context.Guild.OwnerId != Context.User.Id)
            {
                await ReplyAsync("Command can only be used by server owner");
                return;
            }

            var db = new BotBaseContext();
            var config = db.ServerConfig.AsQueryable().Where(s => s.ServerId == Context.Guild.Id).FirstOrDefault();
            if (config != null)
            {
                if (config.MessageId == Context.Channel.Id)
                {
                    config.MessageId = 0;
                    await ReplyAsync($"Message deletion reports will no longer be sent in {Context.Channel.Name}");
                }
                else
                {
                    var old = await Context.Guild.GetTextChannelAsync(config.MessageId);
                    config.MessageId = Context.Channel.Id;
                    if (old != null)
                        await ReplyAsync($"Message deletion reports will no longer be sent in {old.Name}, will now be sent in {Context.Channel.Name}");
                    else
                        await ReplyAsync($"Message deletion reports will now be sent in {Context.Channel.Name}");
                }
            }
            else
            {
                db.Add(new ServerConfig { ServerId = Context.Guild.Id, MessageId = Context.Channel.Id });
                await ReplyAsync($"Message deletion reports will now be sent in {Context.Channel.Name}");
            }
            db.SaveChanges();
        }

        [Command("placereactmessage")]
        [Remarks("-c-, -c- -d")]
        [Summary("Places a message in the current channel that users can react to in order to assign themselves roles. To assign roles to be added to this message, see -p-reactroles. Using this command again will delete the old message, and send a new message. Using this command with -d will delete the message (but keep all pairings set with -p-reactroles)")]
        public async Task PlaceReactMessage([Remainder] string command = "")
        {
            if (Context.Guild.OwnerId != Context.User.Id)
            {
                await ReplyAsync("Command can only be used by server owner");
                return;
            }

            var db = new BotBaseContext();
            var config = db.ServerConfig.AsQueryable().Where(s => s.ServerId == Context.Guild.Id).FirstOrDefault();
            
            if(command == "-d")
            {
                if (config != null && config.ReactMessageId != 0 && config.ReactChannelId != 0)
                {
                    var channel = await Context.Guild.GetTextChannelAsync(config.ReactChannelId);
                    var msg = await channel.GetMessageAsync(config.ReactMessageId);
                    await msg.DeleteAsync();
                    config.ReactChannelId = 0;
                    config.ReactMessageId = 0;
                    db.SaveChanges();
                    await ReplyAsync("Message removed. Any withstanding roles will remain.");
                    return;
                }
                await ReplyAsync("Message does not exist, or cannot be found. Not deleting");
                return;
            }

            var message = await ReplyAsync("", false, BuildReactMessage(Context.Guild));

            if (config == null)
            {
                db.Add(new ServerConfig { ServerId = Context.Guild.Id, ReactMessageId = message.Id, ReactChannelId = message.Channel.Id });
            }
            else 
            {
                if (config.ReactChannelId != 0)
                {
                    var channel = await Context.Guild.GetTextChannelAsync(config.ReactChannelId);
                    var msg = await channel.GetMessageAsync(config.ReactMessageId);
                    await msg.DeleteAsync();
                }

                config.ReactMessageId = message.Id;
                config.ReactChannelId = message.Channel.Id;
            }
            db.SaveChanges();
        }

        [Command("reactrole")]
        [Remarks("-c- @role 🦊\n-c- @role 🦊 description\n-c- @role -d")]
        [Summary("Assigns role-reaction pairings to show in the message sent by -p-placereactmessage. Ensure that Jif Bot is a higher role than any of the roles you would like to be assigned. Any use of -p-reactrole will be represented in the message sent by -p-placereactmessage, and can be used in a seperate channel. The entry for a role can be changed by calling the command again with a different values. A role can be removed by using -d in place of an emote.")]
        public async Task ReactRole(string role, string emote, [Remainder] string description = "")
        {
            if (Context.Guild.OwnerId != Context.User.Id)
            {
                await ReplyAsync("Command can only be used by server owner");
                return;
            }

            // Check to ensure the role is valid
            var db = new BotBaseContext();
            if(!Regex.IsMatch(role, @"[0-9]+"))
            {
                await ReplyAsync("Not a valid role");
                return;
            }
            ulong roleId = Convert.ToUInt64(Regex.Match(role, @"[0-9]+").Value);
            if(Context.Guild.Roles.Where(r => r.Id == roleId).FirstOrDefault() == null)
            {
                await ReplyAsync("Not a valid role");
                return;
            }

            var react = db.ReactRole.AsQueryable().Where(s => s.RoleId == roleId).FirstOrDefault();

            // specified to delete the react role
            if (emote == "-d")
            {
                if (react != null)
                {
                    db.Remove(react);
                    db.SaveChanges();
                    await ReplyAsync("Role removed");
                }
                else
                {
                    await ReplyAsync("Role does not exist. Not removing");
                    return;
                }
            }
            else
            {
                // Check to ensure the emote is in a valid format
                Emote useless;
                if (Regex.IsMatch(emote, @"(\u00a9|\u00ae|[\u2000-\u3300]|\ud83c[\ud000-\udfff]|\ud83d[\ud000-\udfff]|\ud83e[\ud000-\udfff])"))
                {
                    Match match = Regex.Match(emote, "(\u00a9|\u00ae|[\u2000-\u3300] |\ud83c[\ud000 -\udfff] |\ud83d[\ud000-\udfff]|\ud83e[\ud000-\udfff])");
                    emote = match.Value;
                }
                else if (!Emote.TryParse(emote, out useless))
                {
                    await ReplyAsync("Not a valid reaction emote");
                    return;
                }

                var rRole = db.ReactRole.AsQueryable().Where(r => r.Emote == emote && r.ServerId == Context.Guild.Id).FirstOrDefault();

                // if there exists an entry in this server with the specified emote that is not the specified role
                if (rRole != null && rRole.RoleId != roleId)
                {
                    await ReplyAsync("Emote already being used");
                    return;
                }

                // if there is no entry for this role
                else if (react == null)
                {
                    db.Add(new ReactRole { RoleId = roleId, Emote = emote, Description = description, ServerId = Context.Guild.Id });
                    db.SaveChanges();
                    await ReplyAsync("Role Added");
                }

                // update emote and description
                else
                {
                    react.Emote = emote;
                    react.Description = description;
                    db.SaveChanges();
                    await ReplyAsync($"Updated entry for <@&{react.RoleId}>");
                }
            }

            var config = db.ServerConfig.AsQueryable().Where(s => s.ServerId == Context.Guild.Id).FirstOrDefault();
            var channel = await Context.Guild.GetTextChannelAsync(config.ReactChannelId);
            var msg = (IUserMessage)await channel.GetMessageAsync(config.ReactMessageId);
            await msg.ModifyAsync(m => m.Embed = BuildReactMessage(Context.Guild));
        }

        public Embed BuildReactMessage(IGuild server)
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
