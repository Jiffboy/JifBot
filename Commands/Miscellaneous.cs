using System;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using Discord.Interactions;
using JifBot.Models;
using System.Drawing;
using System.Data;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace JifBot.Commands
{
    public class Miscellaneous : InteractionModuleBase<SocketInteractionContext>
    {
        List<Quote> quoteList = new List<Quote>();

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

        [SlashCommand("tiltycat", "Creates a cat at any angle you specify.")]
        public async Task TiltyCat(
            [Summary("degrees", "The number of degrees to rotate the cat clockwise.")] int degree)
        {
            Bitmap bmp = TiltyEmoji.tiltCat(degree);

            using (MemoryStream ms = new MemoryStream())
            {
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Seek(0, SeekOrigin.Begin);
                bmp.Dispose();
                await RespondWithFileAsync(ms, "tiltycat.png");
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

        class TiltyEmoji
        {
            private const string TILTY_CAT = "Media/tiltycat.png";
            private const int OUTPUT_WIDTH = 128;
            private const int OUTPUT_HEIGHT = 128;

            public static Bitmap tiltCat(int degreeCount)
            {
                return openAndRotate(TILTY_CAT, degreeCount);
            }

            private static Bitmap openAndRotate(string filePath, int degreeCount)
            {
                try
                {
                    Bitmap bmp = (Bitmap)Bitmap.FromFile(filePath);
                    return RotateImage(bmp, degreeCount);
                }
                catch (FileNotFoundException ex)
                {
                    Console.Out.WriteLine($"Couldn't find image at file path {filePath}");
                    throw ex;
                }
            }

            private static Bitmap RotateImage(Bitmap bmp, int angle)
            {
                float height = bmp.Height;
                float width = bmp.Width;
                int hypotenuse = System.Convert.ToInt32(System.Math.Floor(Math.Sqrt(height * height + width * width)));
                Bitmap rotatedImage = new Bitmap(hypotenuse, hypotenuse);
                using (Graphics g = Graphics.FromImage(rotatedImage))
                {
                    g.TranslateTransform((float)rotatedImage.Width / 2, (float)rotatedImage.Height / 2); //set the rotation point as the center into the matrix
                    g.RotateTransform(angle); //rotate
                    g.TranslateTransform(-(float)rotatedImage.Width / 2, -(float)rotatedImage.Height / 2); //restore rotation point into the matrix
                    g.DrawImage(bmp, (hypotenuse - OUTPUT_WIDTH) / 2, (hypotenuse - OUTPUT_HEIGHT) / 2, OUTPUT_WIDTH, OUTPUT_HEIGHT);
                }
                return rotatedImage;
            }
        }
        class Quote
        {
            public string text { get; set; }
            public string author { get; set; }
        }

        class QuoteResult
        {
            public List<Quote> List { get; set; }
        }
    }
}