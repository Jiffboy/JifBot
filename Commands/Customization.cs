using System.Threading.Tasks;
using System.Linq;
using Discord.Commands;
using JifBot.Models;
using JIfBot;

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
        [Alias("resetsignature")]
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
    }
}
