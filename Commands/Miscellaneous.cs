using System;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using Discord.Commands;
using JifBot.Models;

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
        [Remarks("-c-")]
        [Summary("Reports the number of times you have said honk\"")]
        public async Task honkCount([Remainder] string useless = "")
        {
            var db = new BotBaseContext();
            var honk = db.Honk.AsQueryable().Where(user => user.UserId == Context.Message.Author.Id).FirstOrDefault();
            if (honk != null)
                await ReplyAsync($"You have honked {honk.Count} times!");
            else
                await ReplyAsync("You have never honked! For shame!");
        }

        [Command("totalhonks")]
        [Remarks("-c-")]
        [Summary("Reports the total number of honks accross all users\"")]
        public async Task totalHonks([Remainder] string useless = "")
        {
            var db = new BotBaseContext();
            long count = 0;
            foreach (Honk honk in db.Honk)
            {
                count += honk.Count;
            }
            await ReplyAsync($"{count} honks");
        }

        [Command("honkboard")]
        [Remarks("-c-")]
        [Summary("Reports the top 5 users who have honked the most\"")]
        public async Task honkBoard([Remainder] string useless = "")
        {
            var db = new BotBaseContext();
            var honks = db.Honk.AsQueryable().OrderByDescending(honk => honk.Count);
            int count = 1;
            string message = "";
            foreach (Honk honk in honks)
            {
                var user = db.User.AsQueryable().Where(user => user.UserId == honk.UserId).FirstOrDefault();
                if (count == 1)
                    message += "🥇";
                else if (count == 2)
                    message += "🥈";
                else if (count == 3)
                    message += "🥉";
                else
                    message += $"  {count}  ";
                message += $" {user.Name}#{user.Number} - {honk.Count} honks\n";
                count++;
                if (count > 5)
                    break;
            }
            await ReplyAsync(message);
        }
    }
}