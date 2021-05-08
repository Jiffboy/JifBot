using System;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using Discord.Commands;
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
    public class Miscellaneous : ModuleBase
    {
        List<Quote> quoteList = new List<Quote>();

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

        [Command("mock")]
        [Remarks("-c-, -c- message, -c- message -d")]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [Summary("Mocks the text you provide it. If you end your command call with -d, it will delete your message calling the bot. If you do not specify any message, it will mock the most recent message sent in the text channel, and delete your command call.")]
        public async Task Mock([Remainder] string words = "")
        {
            if (words == "")
            {
                await Context.Message.DeleteAsync();
                var msg = Context.Channel.GetMessagesAsync(1).FlattenAsync().Result;
                words = msg.ElementAt(0).Content;
            }
            else if (words.EndsWith("-d"))
            {
                words = words.Remove(words.Length - 2);
                await Context.Message.DeleteAsync();
            }
            string end = string.Empty;
            int i = 0;
            foreach (char c in words)
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
            await ReplyAsync(end);
        }
        
        [Command("bigtext")]
        [Remarks("-c- phrase, -c- phrase -d")]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [Summary("Takes the user input for messages and turns it into large letters using emotes. If you end your command call with -d, it will delete your message calling the bot.")]
        public async Task bigtext([Remainder] String message)
        {
            if (message.EndsWith("-d"))
            {
                message = message.Remove(message.Length - 2);
                await Context.Message.DeleteAsync();
            }
            string[] alpha = {"a","b","c","d","e","f","g","h","i","j","k","l","m","n","o","p","q","r","s","t","u","v","w","x","y","z"};
            string[] big = { "🇦 ", "🅱 ", "🇨 ", "🇩 ", "🇪 ", "🇫 ", "🇬 ", "🇭 ", "🇮 ", "🇯 ", "🇰 ", "🇱 ", "🇲 ", "🇳 ", "🇴 ", "🇵 ", "🇶 ", "🇷 ", "🇸 ", "🇹 ", "🇺 ", "🇻 ", "🇼 ", "🇽 ", "🇾 ", "🇿 " };
            message = message.ToLower();
            message = message.Replace(" ", "  ");
            for (int i = 0; i < alpha.Length; i++)
            {
                message = message.Replace(alpha[i], big[i]);
            }
            await ReplyAsync(message);
        }

        [Command("tinytext")]
        [Alias("smalltext")]
        [Remarks("-c- phrase, -c- phrase -d")]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [Summary("Takes the user input for messages and turns it into small letters. If you end your command call with -d, it will delete your message calling the bot.")]
        public async Task tinytext([Remainder] string message)
        {
            if (message.EndsWith("-d"))
            {
                message = message.Remove(message.Length - 2);
                await Context.Message.DeleteAsync();
            }
            string alpha = "abcdefghijklmnopqrstuvwxyz";
            string small = "ᵃᵇᶜᵈᵉᶠᵍʰᶦʲᵏᶫᵐᶰᵒᵖᑫʳˢᵗᵘᵛʷˣʸᶻ";
            message = message.ToLower();

            for (int i = 0; i < alpha.Length; i++)
            {
                message = message.Replace(alpha[i], small[i]);
            }
            await ReplyAsync(message);
        }

        [Command("widetext")]
        [Remarks("-c- phrase, -c- phrase -d")]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [Summary("Takes the user input for messages and turns it into a ＷＩＤＥ  ＢＯＩ. If you end your command call with -d, it will delete your message calling the bot.")]
        public async Task WideText([Remainder] string message)
        {
            if (message.EndsWith("-d"))
            {
                message = message.Remove(message.Length - 2);
                await Context.Message.DeleteAsync();
            }
            message = message.Replace(" ", "   ");
            string alpha = "QWERTYUIOPASDFGHJKLÇZXCVBNMqwertyuiopasdfghjklçzxcvbnm,.-~+´«'0987654321!\"#$%&/()=?»*`^_:;";
            string fullwidth = "ＱＷＥＲＴＹＵＩＯＰＡＳＤＦＧＨＪＫＬÇＺＸＣＶＢＮＭｑｗｅｒｔｙｕｉｏｐａｓｄｆｇｈｊｋｌçｚｘｃｖｂｎｍ,.－~ ´«＇０９８７６５４３２１！＂＃＄％＆／（）＝？»＊`＾＿：；";

            for (int i = 0; i < alpha.Length; i++)
            {
                message = message.Replace(alpha[i], fullwidth[i]);
            }
            await ReplyAsync(message);
        }

        [Command("owo")]
        [Alias("uwu")]
        [Remarks("-c- phrase, -c- phrase -d")]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [Summary("Takes the user input, and translates it into degenerate owo speak. If you end your command call with -d, it will delete your message calling the bot..")]
        public async Task Owo([Remainder] string message)
        {
            if (message.EndsWith("-d"))
            {
                message = message.Remove(message.Length - 2);
                await Context.Message.DeleteAsync();
            }
            string[] faces = new string[] { "(・ω・)", ";;w;;", "owo", "UwU", ">w<", "^w^" };
            Random rnd = new Random();
            message = Regex.Replace(message, @"(?:r|l)", "w");
            message = Regex.Replace(message, @"(?:R|L)", "W");
            message = Regex.Replace(message, @"n([aeiou])", @"ny$1");
            message = Regex.Replace(message, @"N([aeiou])", @"Ny$1");
            message = Regex.Replace(message, @"N([AEIOU])", @"NY$1");
            message = Regex.Replace(message, @"ove", @"uv");
            message = Regex.Replace(message, @"\!+", (match) => string.Format("{0}", " " + faces[rnd.Next(faces.Length)] + " "));

            await ReplyAsync(message);
        }

        [Command("funfact")]
        [Remarks("-c-")]
        [Alias("fact")]
        [Summary("Provides a random fact.")]
        public async Task beeFact([Remainder] string useless = "")
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("https://useless-facts.sameerkumar.website");
            HttpResponseMessage response = await client.GetAsync("/api");
            HttpContent content = response.Content;
            string stuff = await content.ReadAsStringAsync();
            var json = JObject.Parse(stuff);
            if ((string)json.SelectToken("Response") == "False")
            {
                await ReplyAsync("Error retrieving fact");
                return;
            }
            string fact = (string)json.SelectToken("data");
            await ReplyAsync(fact);
        }

        [Command("joke")]
        [Remarks("-c-")]
        [Summary("Tells a joke.")]
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
            await ReplyAsync(source);
        }

        [Command("inspire")]
        [Remarks("-c-")]
        [Summary("Gives an inspirational quote.")]
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
            await ReplyAsync("\"" + quote.text + "\"\n-" + quote.author);
        }

        [Command("tiltycat")]
        [Remarks("-c- degree")]
        [Summary("Creates a cat at any angle you specify.\nSpecial thanks to Erik (Assisting#8734) for writing the program. Accessed via ```http://www.writeonlymedia.com/tilty_cat/(degree).png``` where (degree) is the desired angle.")]
        public async Task TiltyCat(int degree, [Remainder] string useless = "")
        {
            Bitmap bmp = TiltyEmoji.tiltCat(degree);

            using (MemoryStream ms = new MemoryStream())
            {
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Seek(0, SeekOrigin.Begin);
                bmp.Dispose();
                await Context.Channel.SendFileAsync(ms, "tiltycat.png");
            }
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