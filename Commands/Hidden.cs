using System.Threading.Tasks;
using System.Linq;
using Discord.Commands;
using JifBot.Models;

namespace JifBot.Commands
{
    public class Hidden : ModuleBase
    {
        [Command("imean")]
        [Remarks("-c-")]
        [Summary("Reports the number of times Jif has said \"I mean\".")]
        public async Task iMean([Remainder] string useless = "")
        {
            var db = new BotBaseContext();
            var count = db.Variable.AsQueryable().AsQueryable().Where(v => v.Name == "meanCount").First();
            await ReplyAsync($"Jif has said \"I mean\" {count.Value} times");
        }

        [Command("gnomed")]
        [Remarks("-c-")]
        [Summary("I'm gnot a gnelf...")]
        public async Task Gnomed()
        {
            await ReplyAsync("https://www.youtube.com/watch?v=6n3pFFPSlW4");
        }

        [Command("rolligentle")]
        [Remarks("-c-")]
        [Summary("Makes the Gentlecat do a rollie.")]
        public async Task RolliCat([Remainder] string useless = "")
        {
            await ReplyAsync("<:gentlecat:302907277571260418> <:rightcat:455100361066283035> <:bottomcat:455100361120940032> <:leftcat:455100361187786752> <:gentlecat:302907277571260418>");
        }


        [Command("metroman")]
        [Remarks("-c-")]
        [Summary("Spreads the good word.")]
        public async Task MetroMan([Remainder] string useless = "")
        {
            await ReplyAsync("https://www.youtube.com/watch?v=W7urgEgXgyg");
        }

        [Command("meancount")]
        [Remarks("-c-")]
        [Summary("Reports the number of times Jif has said \"I mean\".")]
        public async Task meanCount([Remainder] string useless = "")
        {
            var db = new BotBaseContext();
            var count = db.Variable.AsQueryable().Where(v => v.Name == "meanCount").First();
            await ReplyAsync("I mean, I've said it " + count.Value + " times since 12/13/18.");
        }

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

        [Command("hang")]
        [Remarks("-c-")]
        [Summary("Get Jif Bot to come say hello")]
        public async Task Hang()
        {
            var channels = await Context.Guild.GetVoiceChannelsAsync();
            foreach (var channel in channels)
            {
                var user = await channel.GetUserAsync(Context.User.Id);
                if (user != null)
                {
                    var client = await channel.ConnectAsync();

                }
            }
        }
    }
}