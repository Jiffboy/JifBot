using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace JifBot.Commands
{
    public class Reaction : ModuleBase
    {
        [Command("wtf")]
        [Remarks("-c-")]
        [Summary("Shows your disbelief as to what your fellow server goers have just done.")]
        public async Task WTF()
        {
            await ReplyAsync("https://www.youtube.com/watch?v=wKbU8B-QVZk");
        }

        [Command("neener")]
        [Remarks("-c-")]
        [Summary("Helps to prove your point that you were right.")]
        public async Task ToldYou()
        {
            await Context.Channel.SendFileAsync("Media/neener.gif");
        }

        [Command("bully")]
        [Remarks("-c-")]
        [Summary("Reminds young rapscallions that this is a bully free zone.")]
        public async Task Bully()
        {
            await Context.Channel.SendFileAsync("Media/bully.gif");
        }

        [Command("stfu")]
        [Remarks("-c-")]
        [Summary("Tells someone to shut up.")]
        public async Task STFU()
        {
            await Context.Channel.SendFileAsync("Media/stfu.jpg");
        }

        [Command("edgy")]
        [Remarks("-c-")]
        [Summary("Informs someone that their prior sent comment was perhaps a tad too mischievous.")]
        public async Task Edgy()
        {
            await Context.Channel.SendFileAsync("Media/edgy.jpg");
        }

        [Command("sorry")]
        [Remarks("-c-")]
        [Summary("A command to help you to articulate your regret for your actions.")]
        public async Task Sorry()
        {
            await ReplyAsync("I'm writing this message cause I feel really bad, thinking about the way I hurt you makes me really sad. I'm sorry for all the hurt I've caused you and I regret the things I've done. I've lost the 1 girl I've ever loved and it was cause of the things I've done. Baby I feel so bad right now, cause I tore your world apart, and now all I can think about is how I broke your heart. These tears that run down my cheek are filled with sadness and hurt, because I loved you so much and now I know that it will never work :( I messed up and now I see that you mean the absolute world to me. I know sorry's not enough because I'm such a screw up.. But for whatever its worth I wanted to say, that you cross my mind every single day...The thought of you makes me smile, and I know our love was real, so I'm writing you this letter so that you know how I truly feel. What I really want to say is that I'm sorry, I know that you didn't deserve to be hurt like that, and I know that you will find someone who will love you and treat you right, they will make you happy and that person won't hurt you like I did.");
        }

        [Command("cheer")]
        [Remarks("-c-")]
        [Summary("Displays one of several gifs of cute characters cheering you on.")]
        public async Task Cheer()
        {
            Random rnd = new Random();
            int num = rnd.Next(10);
            string gif = "Media/cheer/cheer" + num + ".gif";
            await Context.Channel.SendFileAsync(gif);
        }

        [Command("lewd")]
        [Remarks("-c-")]
        [Summary("Displays a random image to react to someones lewd comment.")]
        public async Task Lewd()
        {
            Random rnd = new Random();
            int num = rnd.Next(8);
            string png = "Media/lewd/" + num + ".png";
            await Context.Channel.SendFileAsync(png);
        }

        [Command("doghouse")]
        [Remarks("-c- name")]
        [Summary("A command to be used when someone has been imprisoned by their significant other.")]
        public async Task Doghouse([Remainder] string name)
        {
            await ReplyAsync("<:doghouse:305246514467438602> Oh no! <:doghouse:305246514467438602>\n<:doghouse:305246514467438602> Freedom is down the drain! <:doghouse:305246514467438602>\n<:doghouse:305246514467438602> That's right! <:doghouse:305246514467438602>\n<:doghouse:305246514467438602> " + name + " is in the doghouse again! <:doghouse:305246514467438602>");
        }

        [Command("gay")]
        [Remarks("-c-")]
        [Summary("For when the gaydar starts beeping.")]
        public async Task Gay()
        {
            await Context.Channel.SendFileAsync("Media/gay.jpg");
        }

        [Command("biggay")]
        [Remarks("-c-")]
        [Summary("Inform somebody that they are the big gay.")]
        public async Task BigGay()
        {
            await Context.Channel.SendFileAsync("Media/biggay.jpg");
        }

        [Command("wheeze")]
        [Remarks("-c-")]
        [Summary("For use to accompany a joke that really wasn't that good.")]
        public async Task Wheeze()
        {
            await Context.Channel.SendFileAsync("Media/wheeze.png");
        }

        [Command("no")]
        [Remarks("-c-")]
        [Summary("Inform somebody that you will not be doing that")]
        public async Task No()
        {
            await Context.Channel.SendFileAsync("Media/No.jpg");
        }
    }
}