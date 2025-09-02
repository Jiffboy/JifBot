using Discord;
using Discord.Interactions;
using JifBot.Models;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace JifBot.Commands
{
    public class Miscellaneous : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("honkcount", "Reports the number of times a user has said \"honk\".")]
        public async Task honkCount(
            [Summary("user", "The Discord user to count the honks for.")] IGuildUser user)
        {
            var db = new BotBaseContext();
            Honk honk = new Honk();
            honk = db.Honk.AsQueryable().Where(honkuser => honkuser.UserId == user.Id).FirstOrDefault();

            if (honk != null)
                await RespondAsync($"Honked {honk.Count} times!");
            else
                await RespondAsync("This user has never honked! For shame!");
        }

        [SlashCommand("honkboard", "Reports the top number of users who have honked the most.")]
        public async Task honkBoard(
            [Summary("count","Specifies the number of people to show. Defaults to 5.")] int count=5,
            [Summary("showAll","Set to true to see ALL honk counts.")] bool showAll=false)
        {
            var db = new BotBaseContext();
            var honks = db.Honk.AsQueryable().OrderByDescending(honk => honk.Count);
            int current = 1;
            long total = 0;
            string message = "";

            if(showAll)
            {
                count = db.Honk.Count();
            }
            
            foreach (Honk honk in honks)
            {
                var user = db.User.AsQueryable().Where(user => user.UserId == honk.UserId).FirstOrDefault();
                if (current <= count)
                {
                    if (current == 1)
                        message += "🥇";
                    else if (current == 2)
                        message += "🥈";
                    else if (current == 3)
                        message += "🥉";
                    else if (current <= count)
                        message += $"  {current}  ";
                    message += $" {user.Name}#{user.Number} - **{honk.Count}** honks\n";
                }
                total += honk.Count;
                if (current == count)
                    message += $"**Top {count} honks:** {total}\n";
                current++;
            }
            message += $"**Total honks:** {total}";
            await RespondAsync(message);
        }
        
        [SlashCommand("funfact", "Provides a random fact.")]
        public async Task FunFact()
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("https://useless-facts.sameerkumar.website");
            HttpResponseMessage response = await client.GetAsync("/api");
            HttpContent content = response.Content;
            string stuff = await content.ReadAsStringAsync();
            var json = JObject.Parse(stuff);
            if ((string)json.SelectToken("Response") == "False")
            {
                await RespondAsync("Error retrieving fact");
                return;
            }
            string fact = (string)json.SelectToken("data");
            await RespondAsync(fact);
        }

        [SlashCommand("tilty", "Transforms an image.")]
        public async Task Tilty(
            [Choice("Rotate", "rotate")]
            [Choice("Flip Vertical", "vertical")]
            [Choice("Flip Horizontal", "horizontal")]
            [Summary("mode", "How to transform the image.")] string mode,
            [Summary("image", "The image to transform. If unspecified, transforms a cat.")] IAttachment attachment = null,
            [Summary("degrees", "The number of degrees to rotate clockwise (if rotating)")] int degree = 0)
        {
            await DeferAsync();
            var image = SixLabors.ImageSharp.Image.Load<Rgba32>("Media/tiltycat.png");
            if (attachment != null)
            {
                if (!attachment.ContentType.StartsWith("image/"))
                {
                    await FollowupAsync("Please supply a valid image filetype", ephemeral: true);
                    return;
                }
                var client = new HttpClient();
                Stream stream = await client.GetStreamAsync(attachment.Url);
                image = SixLabors.ImageSharp.Image.Load<Rgba32>(stream);
            }

            switch (mode)
            {
                case "rotate":
                    if (degree != 0)
                        image.Mutate(x => x.Rotate(degree));
                    break;

                case "vertical":
                    image.Mutate(x => x.Flip(FlipMode.Vertical));
                    break;

                case "horizontal":
                    image.Mutate(x => x.Flip(FlipMode.Horizontal));
                    break;
            }

            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, new PngEncoder { TransparentColorMode = PngTransparentColorMode.Clear });
                await FollowupWithFileAsync(ms, "tilty.png");
            }
        }

        [SlashCommand("imean", "Reports the number of times Jif has said \"I mean\".")]
        public async Task MeanCount()
        {
            var db = new BotBaseContext();
            var count = db.Variable.AsQueryable().Where(v => v.Name == "meanCount").First();
            await ReplyAsync("I mean, I've said it " + count.Value + " times since 12/13/18.");
        }

        [SlashCommand("react","Posts a specified reaction image.")]
        public async Task React(
            [Choice("neener", "neener.gif")]
            [Choice("bully", "bully.gif")]
            [Choice("stfu", "stfu.jpg")]
            [Choice("edgy", "edgy.jpg")]
            [Choice("gay", "gay.jpg")]
            [Choice("biggay", "biggay.jpg")]
            [Choice("wheeze", "wheeze.png")]
            [Choice("no", "no.jpg")]
            [Choice("horny", "horny.jpg")]
            [Choice("wack", "wack.jpg")]
            [Choice("lobster", "lobster.jpg")]
            [Choice("flat", "flat.png")]
            [Choice("shrug", "shrug.png")]
            [Choice("rigged", "rigged.png")]
            [Summary("reaction", "The reaction image to post.")] string reaction)
        {
            await RespondWithFileAsync($"Media/react/{reaction}");
        }

        [SlashCommand("randimage", "Displays a random image from a series of categories")]
        public async Task RandImage(
            [Choice("Cat in Hat", "cat/png")]
            [Choice("Lewd", "lewd/png")]
            [Choice("Cheer", "cheer/gif")]
            [Summary("category", "The type of image to post")] string category)
        {
            var names = category.Split('/');
            string path = $"Media/{names[0]}";
            var count = Directory.GetFiles(path).Length;

            Random rnd = new Random();
            int num = rnd.Next(count);
            await RespondWithFileAsync($"{path}/{num}.{names[1]}");
        }

        [SlashCommand("reese", "Reese.")]
        public async Task Reese()
        {
            await RespondWithFileAsync("Media/smoochie.mp4", text: "Ladies hit him up.");
        }
    }
}