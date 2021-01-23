using System;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using Discord.Commands;
using JifBot.Models;
using System.Text.RegularExpressions;

namespace JifBot.Commands
{
    public class Miscellaneous : ModuleBase
    {
        [Command("reese")]
        [Remarks("-c-")]
        [Summary("Prompts ladies to hit him up.")]
        public async Task Reese()
        {
            await Context.Channel.SendMessageAsync("Ladies hmu");
        }

        [Command("lobster")]
        [Remarks("-c-")]
        [Summary("Displays the best image on the internet.")]
        public async Task Lobster()
        {
            await Context.Channel.SendFileAsync("Media/lobster.jpg");
        }

        [Command("neeko")]
        [Remarks("-c-")]
        [Summary("A command to celebrate Neeko.")]
        public async Task Neko()
        {
            await Context.Channel.SendFileAsync("Media/neeko.jpg");
        }

        [Command("smoochie")]
        [Remarks("-c-")]
        [Summary("Reese gives a smoochie.")]
        public async Task Smoochie()
        {
            await Context.Channel.SendFileAsync("Media/smoochie.mp4");
        }

        [Command("flat")]
        [Remarks("-c-")]
        [Summary("Heralds the unseen truth.")]
        public async Task Flat()
        {
            await Context.Channel.SendFileAsync("Media/flat.png");
        }

        [Command("attention")]
        [Remarks("-c-")]
        [Summary("Gives Dee the attention she craves.")]
        public async Task Attention()
        {
            await Context.Channel.SendFileAsync("Media/attention.gif");
        }

        [Command("shrug")]
        [Remarks("-c-")]
        [Summary("Shrugs.")]
        public async Task Shrug()
        {
            await Context.Channel.SendFileAsync("Media/shrug.png");
        }

        [Command("lunch")]
        [Remarks("-c-")]
        [Summary("lunch.")]
        public async Task Lunch()
        {
            await Context.Channel.SendFileAsync("Media/lunch.gif");
        }

        [Command("banterwtf")]
        [Remarks("-c-")]
        [Summary("A video to be played when Banter does something stupid.")]
        public async Task BanterWTF()
        {
            await ReplyAsync("https://www.youtube.com/watch?v=-qRsiHfWh1w");
        }

        [Command("rammus")]
        [Remarks("-c-")]
        [Summary("PRAISE RAMMUS.")]
        public async Task Rammus()
        {
            await Context.Channel.SendFileAsync("Media/rammus.png");
            await ReplyAsync("**P  R  A  I  S  E          R  A  M  M  U  S**");
        }

        [Command("whisper")]
        [Remarks("-c- \"name\" message")]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [Summary("Sends a private message to someone on the server. The message containing your command call will be deleted for anonymity. NOTE: the \"name\" is the person's Discord username without the numbers.")]
        public async Task Whisper([Remainder] string contents)
        {
            int spot = contents.IndexOf("\"");
            if (spot == -1)
                await ReplyAsync("improper usage, please use: -p-whisper \"username\" message. Do not include numbers with the user name.");
            else
            {
                IUser user = Context.User;
                await Context.Message.DeleteAsync();
                contents = contents.Remove(0, spot + 1);
                spot = contents.IndexOf("\"");
                string copy = contents.Remove(0, spot + 1);
                contents = contents.Remove(spot);
                if (copy[0] == ' ')
                    copy = copy.Remove(0, 1);
                IGuildUser dm = null;
                var list = Context.Guild.GetUsersAsync();
                for (int i = 0; i < list.Result.Count; i++)
                {
                    if (list.Result.ElementAt(i).Username.ToLower() == contents.ToLower())
                        dm = list.Result.ElementAt(i);
                }
                if (dm == null)
                {
                    await ReplyAsync("That is not a name for anybody on this server. Your message was not sent");
                    Console.WriteLine(user + " attempted to send \"" + copy + "\" to " + contents);
                }
                else
                {
                    await dm.SendMessageAsync(copy);
                    Console.WriteLine(user + " successfully sent \"" + copy + "\" to " + contents);
                }
            }
        }

        [Command("meancount")]
        [Remarks("-c-")]
        [Summary("Reports the number of times Jif has said \"I mean\"")]
        public async Task meanCount([Remainder] string useless = "")
        {
            var db = new BotBaseContext();
            var count = db.Variable.AsQueryable().Where(v => v.Name == "meanCount").First();
            await ReplyAsync("I mean, I've said it " + count.Value + " times since 12/13/18.");
        }

        [Command("honkcount")]
        [Remarks("-c-, -c- @user")]
        [Summary("Reports the number of times you have said \"honk\". If provided with a user ID (or by pinging a user), their honk count will be reported instead.")]
        public async Task honkCount([Remainder] string msg = "")
        {
            var db = new BotBaseContext();
            Honk honk = new Honk();
            if (Regex.IsMatch(msg, @"[0-9]+"))
            {
                var id = Convert.ToUInt64(Regex.Match(msg, @"[0-9]+").Value);
                honk = db.Honk.AsQueryable().Where(user => user.UserId == id).FirstOrDefault();
            }
            else
                honk = db.Honk.AsQueryable().Where(user => user.UserId == Context.Message.Author.Id).FirstOrDefault();

            if (honk != null)
                await ReplyAsync($"Honked {honk.Count} times!");
            else
                await ReplyAsync("This user has never honked! For shame!");
        }

        [Command("honkboard")]
        [Remarks("-c-, -c- 10, -c- -a")]
        [Alias("totalhonks")]
        [Summary("Reports the top number of users who have honked the most. If a number is not specified, the top 5 will be reported. Additionally, to get all users, use -a.")]
        public async Task honkBoard([Remainder] string msg = "")
        {
            int results = 5;
            var db = new BotBaseContext();
            var honks = db.Honk.AsQueryable().OrderByDescending(honk => honk.Count);
            int count = 1;
            long total = 0;
            string message = "";
            if (Regex.IsMatch(msg, @"[0-9]+"))
                results = Convert.ToInt32(Regex.Match(msg, @"[0-9]+").Value);
            if (Regex.IsMatch(msg, @"-a"))
                results = honks.Count() + 1;
            
            foreach (Honk honk in honks)
            {
                var user = db.User.AsQueryable().Where(user => user.UserId == honk.UserId).FirstOrDefault();
                if (count <= results)
                {
                    if (count == 1)
                        message += "🥇";
                    else if (count == 2)
                        message += "🥈";
                    else if (count == 3)
                        message += "🥉";
                    else if (count <= results)
                        message += $"  {count}  ";
                    message += $" {user.Name}#{user.Number} - **{honk.Count}** honks\n";
                }
                total += honk.Count;
                if (count == results)
                    message += $"**Top {count} honks:** {total}\n";
                count++;
            }
            message += $"**Total honks:** {total}";
            await ReplyAsync(message);
        }
    }
}