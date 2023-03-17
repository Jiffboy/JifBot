using System;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using Discord.Interactions;
using JifBot.Models;
using System.Text.RegularExpressions;
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
        
        [SlashCommand("sillytext", "Takes specified text and makes it silly.")]
        public async Task SillyText(
            [Choice("mock", "mock")]
            [Choice("owo", "owo")]
            [Choice("big", "big")]
            [Choice("tiny", "tiny")]
            [Choice("wide", "wide")]
            [Summary("style", "How to modify the text.")] string style,
            [Summary("text", "The text to be modified.")] string text)
        {
            switch (style)
            {
                case "mock":
                    await RespondAsync(MockText(text));
                    break;
                case "owo":
                    await RespondAsync(OwoText(text));
                    break;
                case "big":
                    await RespondAsync(BigText(text));
                    break;
                case "tiny":
                    await RespondAsync(TinyText(text));
                    break;
                case "wide":
                    await RespondAsync(WideText(text));
                    break;
            }
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

        [SlashCommand("joke", "Tells a joke.")]
        public async Task Joke()
        {
            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
            string source = await client.GetStringAsync("http://www.rinkworks.com/jokes/random.cgi");
            string ptr = "< div class='content'>";
            source = source.Remove(0, source.IndexOf(ptr) + ptr.Length);
            ptr = "</h2>";
            source = source.Remove(0, source.IndexOf(ptr) + ptr.Length);
            ptr = "</td><td class='ad'>";
            source = source.Remove(source.IndexOf(ptr));
            source = source.Replace("<p>", string.Empty);
            source = source.Replace("</p>", "\n");
            source = source.Replace("<ul>", "\n");
            source = source.Replace("<li>", "\n");
            source = source.Replace("<em>", "*");
            source = source.Replace("</ul>", "\n");
            source = source.Replace("</li>", "\n");
            source = source.Replace(">/em>", "*");
            await RespondAsync(source);
        }

        [SlashCommand("inspire", "Gives an inspirational quote.")]
        public async Task Inspire()
        {
            if (quoteList.Count == 0)
            {
                using (HttpClient client = new HttpClient())
                {
                    using (var response = await client.GetAsync("https://type.fit/api/quotes"))
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        jsonResponse = jsonResponse.Replace("[", "{\"list\":[");
                        jsonResponse = jsonResponse.Replace("]", "]}");
                        try
                        {
                            QuoteResult result = JsonConvert.DeserializeObject<QuoteResult>(jsonResponse);
                            quoteList = result.List;
                            var target = result.List;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                }
            }
            int count = quoteList.Count;
            Random rnd = new Random();
            int num = rnd.Next(count);
            Quote quote = quoteList[num];
            if (quote.author == null)
                quote.author = "Author Unknown";
            await RespondAsync("\"" + quote.text + "\"\n-" + quote.author);
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

        [SlashCommand("league", "Asking for help is the first step towards recovery.")]
        public async Task LeagueOfLegends()
        {
            await RespondAsync("https://www.youtube.com/watch?v=EjHKIJ90FtY");
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
            [Summary("reaction", "The reaction image to post.")] string reaction)
        {
            await RespondWithFileAsync($"Media/react/{reaction}");
        }

        [SlashCommand("wtf", "Shows your disbelief as to what your fellow server goers have just done.")]
        public async Task WTF()
        {
            await RespondAsync("https://www.youtube.com/watch?v=wKbU8B-QVZk");
        }

        [SlashCommand("cheer", "Displays one of several gifs of cute characters cheering you on.")]
        public async Task Cheer()
        {
            Random rnd = new Random();
            int num = rnd.Next(10);
            string gif = "Media/cheer/cheer" + num + ".gif";
            await RespondWithFileAsync(gif);
        }

        [SlashCommand("lewd", "Displays a random image to react to someones lewd comment.")]
        public async Task Lewd()
        {
            Random rnd = new Random();
            int num = rnd.Next(8);
            string png = "Media/lewd/" + num + ".png";
            await RespondWithFileAsync(png);
        }

        [SlashCommand("reese", "Reese.")]
        public async Task Reese()
        {
            await RespondWithFileAsync("Media/smoochie.mp4", text: "Ladies hit him up.");
        }

        private string MockText(string text)
        {
            string end = string.Empty;
            int i = 0;
            foreach (char c in text)
            {
                if (c == ' ' || c == '"' || c == '.' || c == ',')
                    end += c;
                else if (i == 0)
                {
                    char temp = Char.ToLower(c);
                    end += temp;
                    i = 1;
                }
                else if (i == 1)
                {
                    char temp = Char.ToUpper(c);
                    end += temp;
                    i = 0;
                }
            }
            return end;
        }

        private string BigText(string text)
        {
            string[] alpha = { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z" };
            string[] big = { "🇦 ", "🅱 ", "🇨 ", "🇩 ", "🇪 ", "🇫 ", "🇬 ", "🇭 ", "🇮 ", "🇯 ", "🇰 ", "🇱 ", "🇲 ", "🇳 ", "🇴 ", "🇵 ", "🇶 ", "🇷 ", "🇸 ", "🇹 ", "🇺 ", "🇻 ", "🇼 ", "🇽 ", "🇾 ", "🇿 " };
            text = text.ToLower();
            text = text.Replace(" ", "  ");
            for (int i = 0; i < alpha.Length; i++)
            {
                text = text.Replace(alpha[i], big[i]);
            }
            return text;
        }

        private string TinyText(string text)
        {
            string alpha = "abcdefghijklmnopqrstuvwxyz";
            string small = "ᵃᵇᶜᵈᵉᶠᵍʰᶦʲᵏᶫᵐᶰᵒᵖᑫʳˢᵗᵘᵛʷˣʸᶻ";
            text = text.ToLower();

            for (int i = 0; i < alpha.Length; i++)
            {
                text = text.Replace(alpha[i], small[i]);
            }
            return text;
        }

        private string WideText(string text)
        {
            text = text.Replace(" ", "   ");
            string alpha = "QWERTYUIOPASDFGHJKLÇZXCVBNMqwertyuiopasdfghjklçzxcvbnm,.-~+´«'0987654321!\"#$%&/()=?»*`^_:;";
            string fullwidth = "ＱＷＥＲＴＹＵＩＯＰＡＳＤＦＧＨＪＫＬÇＺＸＣＶＢＮＭｑｗｅｒｔｙｕｉｏｐａｓｄｆｇｈｊｋｌçｚｘｃｖｂｎｍ,.－~ ´«＇０９８７６５４３２１！＂＃＄％＆／（）＝？»＊`＾＿：；";

            for (int i = 0; i < alpha.Length; i++)
            {
                text = text.Replace(alpha[i], fullwidth[i]);
            }
            return text;
        }

        private string OwoText(string text)
        {
            string[] faces = new string[] { "(・ω・)", ";;w;;", "owo", "UwU", ">w<", "^w^" };
            Random rnd = new Random();
            text = Regex.Replace(text, @"(?:r|l)", "w");
            text = Regex.Replace(text, @"(?:R|L)", "W");
            text = Regex.Replace(text, @"n([aeiou])", @"ny$1");
            text = Regex.Replace(text, @"N([aeiou])", @"Ny$1");
            text = Regex.Replace(text, @"N([AEIOU])", @"NY$1");
            text = Regex.Replace(text, @"ove", @"uv");
            if( !text.EndsWith("!") )
            {
                text += " " + faces[rnd.Next(faces.Length)];
            }
            text = Regex.Replace(text, @"\!+", (match) => string.Format("{0}", " " + faces[rnd.Next(faces.Length)] + " "));

            return text;
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