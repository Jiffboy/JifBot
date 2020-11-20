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
    }
}