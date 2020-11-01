using System;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Http;
using System.Globalization;
using System.Threading;
using System.Diagnostics;
using Discord;
using Discord.Commands;
using Newtonsoft.Json.Linq;
using System.Web;
using Newtonsoft.Json;
using JifBot.Models;
using JIfBot;
using SQLitePCL;

namespace JifBot.Commands
{
    public class Commands : ModuleBase
    {
        private string configName = Program.configName;
        List<Quote> quoteList = new List<Quote>();

        [Command("invitelink")]
        [Remarks("Utility")]
        [Summary("Provides a link which can be used should you want to spread Jif Bot to another server.\nUsage: ~invitelink")]
        public async Task InviteLink()
        {
            await ReplyAsync("The following is a link to add me to another server. NOTE: You must have permissions on the server in order to add. Once on the server I must be given permission to send and delete messages, otherwise I will not work.\nhttps://discordapp.com/oauth2/authorize?client_id=315569278101225483&scope=bot");
        }

        [Command("bigtext")]
        [Remarks("Utility")]
        [Summary("Takes the user input for messages and turns it into large letters using emotes. If you end your command call with -d, it will delete your message calling the bot.\nUsage: ~bigtext phrase, ~bigtext phrase -d")]
        public async Task bigtext([Remainder]string orig)
        {
            if (orig.EndsWith("-d"))
            {
                orig = orig.Remove(orig.Length - 2);
                await Context.Message.DeleteAsync();
            }
            string final = "";
            orig = orig.ToLower();
            for (int i = 0; i < orig.Length; i++)
            {
                final += getBig(orig[i]);
                final += " ";
            }
            if (final.Length > 2000)
                await ReplyAsync("This command does not support messages of that length, please shorten your message.");
            else
                await ReplyAsync(final);
        }

        [Command("tinytext")]
        [Alias("smalltext")]
        [Remarks("Utility")]
        [Summary("Takes the user input for messages and turns it into small letters. If you end your command call with -d, it will delete your message calling the bot.\nUsage: ~tinytext phrase, ~tinytext phrase -d")]
        public async Task tinytext([Remainder]string orig)
        {
            if (orig.EndsWith("-d"))
            {
                orig = orig.Remove(orig.Length - 2);
                await Context.Message.DeleteAsync();
            }
            string final = "";
            orig = orig.ToLower();
            for (int i = 0; i < orig.Length; i++)
            {
                final += getSmol(orig[i]);
            }
            if (final.Length > 2000)
                await ReplyAsync("This command does not support messages of that length, please shorten your message.");
            else
                await ReplyAsync(final);
        }

        [Command("widetext")]
        [Remarks("Utility")]
        [Summary("Takes the user input for messages and turns it into a ＷＩＤＥ  ＢＯＩ. If you end your command call with -d, it will delete your message calling the bot.\nUsage: ~widetext phrase, ~widetext phrase -d")]
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
        [Remarks("Utility")]
        [Summary("Takes the user input, and translates it into degenerate owo speak. If you end your command call with -d, it will delete your message calling the bot..\nUsage: ~owo phrase, ~owo phrase -d")]
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

        [Command("timer")]
        [Remarks("Utility")]
        [Summary("Sets a reminder to ping you after a certain amount of time has passed. A message can be specified along with the time to be printed back to you at the end of the timer. Times can be specified using any combination of -m[minutes], -h[hours], -d[days], and -w[weeks] anywhere in the message. Additionally, to set a quick timer for a number of minutes, just do ~timer [minutes] message\nUsage: ~timer -h2 -m30 message, ~timer 150 message")]
        public async Task Timer([Remainder]string message = "")
        {
            int waitTime = 0;

            if (Regex.IsMatch(message, @"-m *[0-9]+"))
                waitTime += Convert.ToInt32(Regex.Match(message, @"-m *[0-9]+").Value.Replace("-m", ""));

            if (Regex.IsMatch(message, @"-h *[0-9]+"))
            {
                waitTime += (Convert.ToInt32(Regex.Match(message, @"-h *[0-9]+").Value.Replace("-h", "")) * 60);
            }

            if (Regex.IsMatch(message, @"-d *[0-9]+"))
            {
                waitTime += (Convert.ToInt32(Regex.Match(message, @"-d *[0-9]+").Value.Replace("-d", "")) * 1440);
            }

            if (Regex.IsMatch(message, @"-w *[0-9]+"))
            {
                waitTime += (Convert.ToInt32(Regex.Match(message, @"-w *[0-9]+").Value.Replace("-w", "")) * 10080);
            }

            if (waitTime == 0)
            {
                if (Regex.IsMatch(message, @" *[0-9]+"))
                    waitTime = Convert.ToInt32(message.Split(" ")[0]);
                else
                {
                    await ReplyAsync("Please provide an amount of time to wait for. For assistance, use ~help.");
                    return;
                }
            }

            message = Regex.Replace(message, @"-[m,h,d] *[0-9]+", "");
            if (message.Replace(" ", "") == "")
                message = "Times up!";
            Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = "/bin/bash";
            proc.StartInfo.Arguments = "../../../scripts/sendmessage.sh " + Context.Channel.Id + " \"" + Context.User.Mention + " " + message + "\" " + waitTime;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.Start();

            await ReplyAsync("Setting timer for " + formatMinutesToString(waitTime) + " from now.");
        }

        [Command("choose")]
        [Remarks("Utility")]
        [Summary("Randomly makes a choice for you. You can use as many choices as you want, but seperate all choices using a space. If you wish for a choice to contain spaces, surround the choice with \"\"\nUsage: ~choose choice \"choice but with spaces\"")]
        public async Task Choose([Remainder]string message)
        {
            int quotes = message.Split('\"').Length - 1;
            if (quotes % 2 != 0)
            {
                await ReplyAsync("please ensure all quotations are closed");
                return;
            }

            List<string> choices = new List<string>();
            int count = 0;
            message = message.TrimEnd();
            while (true)
            {
                message = message.TrimStart();
                string choice;
                if (message == "")
                {
                    break;
                }
                if (message[0] == '\"')
                {
                    message = message.Remove(0, 1);
                    choice = message.Remove(message.IndexOf("\""));
                    message = message.Remove(0, message.IndexOf("\"") + 1);
                }
                else
                {
                    if (message.Contains(" "))
                    {
                        choice = message.Remove(message.IndexOf(" "));
                        message = message.Remove(0, message.IndexOf(" "));
                    }
                    else
                    {
                        choice = message;
                        message = "";
                    }
                }
                choices.Add(choice);
                count++;
            }

            if (count < 2)
            {
                await ReplyAsync("Please provide at least two choices.");
                return;
            }

            Random rnd = new Random();
            int num = rnd.Next(count);
            await ReplyAsync("The robot overlords have chosen: **" + choices[num] + "**");
        }

        [Command("youtube")]
        [Remarks("Utility")]
        [Summary("Takes whatever you give it and searches for it on YouTube, it will return the first search result that appears.\nUsage: ~youtube video title")]
        public async Task Youtube([Remainder]string vid)
        {
            vid = "https://www.youtube.com/results?search_query=" + vid.Replace(" ", "+");
            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
            string html = await client.GetStringAsync(vid);
            html = html.Remove(0, html.IndexOf("?v=") + 3);
            html = html.Remove(html.IndexOf("\""));
            await ReplyAsync("https://www.youtube.com/watch?v=" + html);
        }

        [Command("mock")]
        [Remarks("Utility")]
        [Summary("Mocks the text you provide it. If you end your command call with -d, it will delete your message calling the bot. If you do not specify any message, it will mock the most recent message sent in the text channel, and delete your command call.\nUsage: ~mock, ~mock message, ~mock message -d")]
        public async Task Mock([Remainder] string words = "")
        {
            if (words == "")
            {
                await Context.Message.DeleteAsync();
                var msg = Context.Channel.GetMessagesAsync(2).Flatten().Result;
                words = msg.ElementAt(0).Content;
                if (words.ToLower() == "!mock")
                    words = msg.ElementAt(1).Content;
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

        [Command("8ball")]
        [Remarks("Utility")]
        [Summary("asks the magic 8 ball a question.\nUsage: ~8ball")]
        public async Task eightBall([Remainder] string useless = "")
        {
            string[] responses = new string[] { "it is certain", "It is decidedly so", "Without a doubt", "Yes definitely", "You may rely on it", "As I see it, yes", "Most likely", "Outlook good", "Yes", "Signs point to yes", "Reply hazy try again", "Ask again later", "Better not tell you now", "Cannot predict now", "Concentrate and ask again", "Don't count on it", "My reply is no", "My sources say no", "Outlook not so good", "Very doubtful" };
            Random rnd = new Random();
            int num = rnd.Next(20);
            await ReplyAsync(responses[num]);
        }

        [Command("s8ball")]
        [Remarks("Utility")]
        [Summary("asks the sassy 8 ball a question.\nUsage: ~s8ball")]
        public async Task SeightBall([Remainder] string useless = "")
        {
            string[] responses = new string[] { "Fuck yeah.", "Sure, why not?", "Well, duh.", "Do bears shit in the woods?", "Is water wet?", "I mean, I guess.", "If it gets you to fuck off, then sure.", "011110010110010101110011", "Whatever floats your boat.", "Fine, sure, whatever.", "Fuck you.", "Why do you feel the need to ask a BOT for validation?", "Figure it out yourself.", "Does it really matter?", "Leave me alone.", "Fuck no.", "Why would you even consider that a possibility?", "It's cute you think that could happen.", "Not a chance shitlord.", "Not in a million years." };
            Random rnd = new Random();
            int num = rnd.Next(20);
            await ReplyAsync(responses[num]);
        }

        [Command("tiltycat")]
        [Remarks("Utility")]
        [Summary("Creates a cat at any angle you specify.\nSpecial thanks to Erik (Assisting#8734) for writing the program. Accessed via ```http://www.writeonlymedia.com/tilty_cat/(degree).png``` where (degree) is the desired angle\nUsage: ~tiltycat degree")]
        public async Task TiltyCat(int degree, [Remainder] string useless = "")
        {
            string temp = "http://www.writeonlymedia.com/tilty_cat/" + degree + ".png";
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(new Uri(temp), "tiltycat.png");
            }
            await Context.Channel.SendFileAsync("tiltycat.png");
        }

        [Command("tiltydog")]
        [Remarks("Utility")]
        [Summary("Creates a dog at any angle you specify.\nSpecial thanks to Erik (Assisting#8734) for writing the program. Accessed via ```http://www.writeonlymedia.com/tilty_dog/(degree).png``` where (degree) is the desired angle\nUsage: ~tiltydog degree")]
        public async Task TiltyDat(int degree, [Remainder] string useless = "")
        {
            string temp = "http://www.writeonlymedia.com/tilty_dog/" + degree + ".png";
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(new Uri(temp), "tiltydog.png");
            }
            await Context.Channel.SendFileAsync("tiltydog.png");
        }

        [Command("joke")]
        [Remarks("Utility")]
        [Summary("Tells a joke\nUsage: ~joke")]
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
            source = source.Replace("</ul>", "\n");
            source = source.Replace("</li>", "\n");
            await ReplyAsync(source);
        }


        [Command("inspire")]
        [Remarks("Utility")]
        [Summary("Gives an inspirational quote\nUsage: ~inspire")]
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
            await ReplyAsync("\"" + quote.text + "\"\n-" + quote.author);


        }

        [Command("wtf")]
        [Remarks("Reaction")]
        [Summary("Shows your disbelief as to what your fellow server goers have just done.\nUsage: ~wtf")]
        public async Task WTF()
        {
            await ReplyAsync("https://www.youtube.com/watch?v=wKbU8B-QVZk");
        }

        [Command("neener")]
        [Remarks("Reaction")]
        [Summary("Helps to prove your point that you were right.\nUsage: ~neener")]
        public async Task ToldYou()
        {
            await Context.Channel.SendFileAsync("media/neener.gif");
        }

        [Command("bully")]
        [Remarks("Reaction")]
        [Summary("Reminds young rapscallions that this is a bully free zone.\nUsage: ~bully")]
        public async Task Bully()
        {
            await Context.Channel.SendFileAsync("media/bully.gif");
        }

        [Command("stfu")]
        [Remarks("Reaction")]
        [Summary("Tells someone to shut up.\nUsage: ~stfu")]
        public async Task STFU()
        {
            await Context.Channel.SendFileAsync("media/stfu.jpg");
        }

        [Command("edgy")]
        [Remarks("Reaction")]
        [Summary("Informs someone that their prior sent comment was perhaps a tad too mischievous.\nUsage: ~edgy")]
        public async Task Edgy()
        {
            await Context.Channel.SendFileAsync("media/edgy.jpg");
        }

        [Command("sorry")]
        [Remarks("Reaction")]
        [Summary("A command to help you to articulate your regret for your actions.\nUsage: ~sorry")]
        public async Task Sorry()
        {
            await ReplyAsync("I'm writing this message cause I feel really bad, thinking about the way I hurt you makes me really sad. I'm sorry for all the hurt I've caused you and I regret the things I've done. I've lost the 1 girl I've ever loved and it was cause of the things I've done. Baby I feel so bad right now, cause I tore your world apart, and now all I can think about is how I broke your heart. These tears that run down my cheek are filled with sadness and hurt, because I loved you so much and now I know that it will never work :( I messed up and now I see that you mean the absolute world to me. I know sorry's not enough because I'm such a screw up.. But for whatever its worth I wanted to say, that you cross my mind every single day...The thought of you makes me smile, and I know our love was real, so I'm writing you this letter so that you know how I truly feel. What I really want to say is that I'm sorry, I know that you didn't deserve to be hurt like that, and I know that you will find someone who will love you and treat you right, they will make you happy and that person won't hurt you like I did.");
        }

        [Command("cheer")]
        [Remarks("Reaction")]
        [Summary("Displays one of several gifs of cute characters cheering you on.\nUsage: ~cheer")]
        public async Task Cheer()
        {
            Random rnd = new Random();
            int num = rnd.Next(10);
            string gif = "media/cheer/cheer" + num + ".gif";
            await Context.Channel.SendFileAsync(gif);
        }

        [Command("lewd")]
        [Remarks("Reaction")]
        [Summary("Displays a random image to react to someones lewd comment.\nUsage: ~lewd")]
        public async Task Lewd()
        {
            Random rnd = new Random();
            int num = rnd.Next(8);
            string png = "media/lewd/" + num + ".png";
            await Context.Channel.SendFileAsync(png);
        }

        [Command("doghouse")]
        [Remarks("Reaction")]
        [Summary("A command to be used when someone has been imprisoned by their significant other.\nUsage: ~doghouse name")]
        public async Task Doghouse([Remainder]string name)
        {
            await ReplyAsync("<:doghouse:305246514467438602> Oh no! <:doghouse:305246514467438602>\n<:doghouse:305246514467438602> Freedom is down the drain! <:doghouse:305246514467438602>\n<:doghouse:305246514467438602> That's right! <:doghouse:305246514467438602>\n<:doghouse:305246514467438602> " + name + " is in the doghouse again! <:doghouse:305246514467438602>");
        }

        [Command("gay")]
        [Remarks("Reaction")]
        [Summary("For when the gaydar starts beeping.\nUsage: ~gay")]
        public async Task Gay()
        {
            await Context.Channel.SendFileAsync("media/gay.jpg");
        }

        [Command("biggay")]
        [Remarks("Reaction")]
        [Summary("Inform somebody that they are the big gay.\nUsage: ~biggay")]
        public async Task BigGay()
        {
            await Context.Channel.SendFileAsync("media/biggay.jpg");
        }

        [Command("wheeze")]
        [Remarks("Reaction")]
        [Summary("For use to accompany a joke that really wasn't that good\nUsage: ~wheeze")]
        public async Task Wheeze()
        {
            await Context.Channel.SendFileAsync("media/wheeze.png");
        }

        [Command("streamers")]
        [Remarks("Information")]
        [Summary("Displays everybody on the server who is currently streaming\nUsage: ~streamers")]
        public async Task Stream()
        {
            var db = new BotBaseContext();
            bool found = false;
            var embed = new EmbedBuilder();
            var color = db.Variable.Where(V => V.Name == "embedColor").FirstOrDefault();
            embed.WithColor(new Color(Convert.ToUInt32(color.Value, 16)));
            IGuild server = Context.Guild;
            var people = server.GetUsersAsync();
            foreach (IGuildUser person in people.Result)
            {
                if (person.Game != null && person.Game.Value.StreamUrl != null)
                {
                    embed.AddField(person.Username, "[" + person.Game + "](" + person.Game.Value.StreamUrl + ")");
                    found = true;
                }
            }
            if (!found)
                await ReplyAsync("Nobody is streaming at this time.");
            else
                await ReplyAsync("", false, embed);
        }

        [Command("define")]
        [Remarks("Information")]
        [Summary("Defines any word in the Oxford English dictionary. For multiple definitions, use -m at the end of the command\nUsage: ~define word OR ~define word -m")]
        public async Task Define([Remainder]string word)
        {
            var db = new BotBaseContext();
            var config = db.Configuration.Where(cfg => cfg.Name == configName).First();
            bool multiple = false;

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("https://od-api.oxforddictionaries.com/api/v2/entries/en/");
            client.DefaultRequestHeaders.Add("app_id", config.DictId);
            client.DefaultRequestHeaders.Add("app_key", config.DictKey);
            if (word.EndsWith(" -m"))
            {
                word = word.Replace(" -m", "");
                multiple = true;
            }
            HttpResponseMessage response = await client.GetAsync(word);
            if (response.StatusCode.ToString() == "NotFound")
            {
                await Context.Channel.SendFileAsync("media/damage.png");
                return;
            }

            if (response.StatusCode.ToString() == "Forbidden")
            {
                await ReplyAsync("Unable to retrieve definition");
                return;
            }
            HttpContent content = response.Content;
            string stuff = await content.ReadAsStringAsync();
            var json = JObject.Parse(stuff);
            if (multiple)
            {
                var embed = new EmbedBuilder();
                var color = db.Variable.Where(V => V.Name == "embedColor").FirstOrDefault();
                embed.WithColor(new Color(Convert.ToUInt32(color.Value, 16)));
                string def = "1.) " + (string)json.SelectToken("results[0].lexicalEntries[0].entries[0].senses[0].definitions[0]");
                string example = (string)json.SelectToken("results[0].lexicalEntries[0].entries[0].senses[0].examples[0].text");
                if (example == null)
                {
                    example = "(no example available)";
                }
                embed.AddField(def, example);
                for (int i = 0; i < 4; i++)
                {
                    def = (string)json.SelectToken("results[0].lexicalEntries[0].entries[0].senses[0].subsenses[" + i.ToString() + "].definitions[0]");
                    example = (string)json.SelectToken("results[0].lexicalEntries[0].entries[0].senses[0].subsenses[" + i.ToString() + "].examples[0].text");
                    if (def != null)
                    {
                        def = (i + 2).ToString() + ".) " + def;
                        if (example == null)
                        {
                            example = "(no example available)";
                        }
                        embed.AddField(def, example);
                    }
                }
                embed.WithFooter("Made with love");
                embed.WithCurrentTimestamp();
                await ReplyAsync("", false, embed);
            }
            else
            {
                CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
                TextInfo textInfo = cultureInfo.TextInfo;
                string name = textInfo.ToTitleCase(word);
                var type = json.SelectToken("results[0].lexicalEntries[0].lexicalCategory.text");
                string spelling = (string)json.SelectToken("results[0].lexicalEntries[0].pronunciations[0].phoneticSpelling");
                string definition = (string)json.SelectToken("results[0].lexicalEntries[0].entries[0].senses[0].definitions[0]");
                string example = (string)json.SelectToken("results[0].lexicalEntries[0].entries[0].senses[0].examples[0].text");
                string message = name + " (" + type + ")";
                if (spelling != null)
                {
                    message += "     /" + spelling + "/";
                }
                message += "\n**Definition: **" + definition;
                if (example != null)
                {
                    message += "\n**Example: **" + example;
                }
                await ReplyAsync(message);
            }
        }

        [Command("udefine")]
        [Remarks("Information")]
        [Alias("slang")]
        [Summary("Gives the top definition for the term from urbandictionary.com\nUsage: ~udefine phrase, ~slang phrase")]
        public async Task DefineUrbanDictionary([Remainder]string phrase)
        {
            string URBAN_DICTIONARY_ENDPOINT = "http://api.urbandictionary.com/v0/define?term=";

            string encodedSearchTerm = HttpUtility.UrlEncode(phrase);
            List<UrbanDictionaryDefinition> definitionList = new List<UrbanDictionaryDefinition>();

            using (HttpClient client = new HttpClient())
            {
                using (var response = await client.GetAsync(URBAN_DICTIONARY_ENDPOINT + encodedSearchTerm))
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    try
                    {
                        UrbanDictionaryResult udefineResult = JsonConvert.DeserializeObject<UrbanDictionaryResult>(jsonResponse);
                        definitionList = udefineResult.List;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }

            if (definitionList.Count > 0)
            {
                // Urban Dictionary uses square brackets for links in its markup; they'll never appear as part of the definition text.
                var cleanDefinition = definitionList[0].Definition.Replace("[", "").Replace("]", "");
                var cleanExample = definitionList[0].Example.Replace("[", "").Replace("]", "");
                var year = definitionList[0].Written_On.Substring(0, definitionList[0].Written_On.IndexOf("-"));
                var dayMonth = definitionList[0].Written_On.Substring(definitionList[0].Written_On.IndexOf("-") + 1, 5);
                var cleanDate = dayMonth.Replace("-", "/") + "/" + year;
                var word = definitionList[0].Word;

                await ReplyAsync($"{word} - {cleanDate}\n**Definition:** {cleanDefinition}\n**Example:** {cleanExample}");
            }
            else
            {
                await ReplyAsync($"{phrase} is not an existing word/phrase");
            }

        }

        [Command("movie")]
        [Remarks("Information")]
        [Summary("Provides information for a movie as specified by name\nUsage: ~movie airplane!")]
        public async Task Movie([Remainder] string word)
        {
            var db = new BotBaseContext();
            var config = db.Configuration.Where(cfg => cfg.Name == configName).First();

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("http://www.omdbapi.com");
            HttpResponseMessage response = await client.GetAsync($"?t={word}&plot=full&apikey={config.OmdbKey}");
            HttpContent content = response.Content;
            string stuff = await content.ReadAsStringAsync();
            var json = JObject.Parse(stuff);
            if ((string)json.SelectToken("Response") == "False")
            {
                await ReplyAsync("Movie not found");
                return;
            }
            var embed = new EmbedBuilder();
            var color = db.Variable.Where(V => V.Name == "embedColor").FirstOrDefault();
            embed.WithColor(new Color(Convert.ToUInt32(color.Value, 16)));
            string rt = (string)json.SelectToken("Ratings[1].Value");
            string imdb = (string)json.SelectToken("Ratings[0].Value");

            embed.Title = (string)json.SelectToken("Title");
            embed.Description = (string)json.SelectToken("Genre");
            if((string)json.SelectToken("Poster") != "N/A")
                embed.ThumbnailUrl = (string)json.SelectToken("Poster");
            if(rt != null)
                embed.AddField($"Rotten Tomatoes: {rt}, IMDb: {imdb}", (string)json.SelectToken("Plot"));
            else
                embed.AddField($"IMDb Rating: {imdb}", (string)json.SelectToken("Plot"));
            embed.AddInlineField("Released", (string)json.SelectToken("Released"));
            embed.AddInlineField("Run Time", (string)json.SelectToken("Runtime"));
            embed.AddInlineField("Rating", (string)json.SelectToken("Rated"));
            embed.AddField("Starring", (string)json.SelectToken("Actors"));
            embed.AddField("Directed By", (string)json.SelectToken("Director"));

            embed.WithFooter("Made with love");
            embed.WithCurrentTimestamp();
            await ReplyAsync("", false, embed);
        }

        [Command("stats")]
        [Remarks("Information")]
        [Summary("Gives the stats for a league player on any region. The region name is the abbreviated verson of the region name. Example: na = North America\nUsage: ~stats region username")]
        public async Task Stats(string region, [Remainder]string name)
        {
            name = name.Replace(" ", string.Empty);

            string SearchText = "<meta name=\"description\" content=\"";
            string SearchText2 = "\"/>";

            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
            string source = "";
            if (region == "kr")
            {
                if (await RemoteFileExists("http://www.op.gg/summoner/userName=" + name))
                    source = await client.GetStringAsync("http://www.op.gg/summoner/userName=" + name);
                else
                {
                    await ReplyAsync("That is not a valid summoner name / region");
                    return;
                }
            }
            else
            {
                if (await RemoteFileExists("http://" + region + ".op.gg/summoner/userName=" + name))
                    source = await client.GetStringAsync("http://" + region + ".op.gg/summoner/userName=" + name);
                else
                {
                    await ReplyAsync("That is not a valid summoner name / region");
                    return;
                }
            }
            if (source.IndexOf("This summoner is not registered at OP.GG. Please check spelling.") != -1)
            {
                await ReplyAsync("That Summoner does not exist");
                return;
            }
            else
            {
                var db = new BotBaseContext();
                var embed = new EmbedBuilder();
                var color = db.Variable.Where(V => V.Name == "embedColor").FirstOrDefault();
                embed.WithColor(new Color(Convert.ToUInt32(color.Value, 16)));
                {
                    string kdsource = source.Remove(0, source.IndexOf("summoner-id=\"") + 13);
                    kdsource = kdsource.Remove(kdsource.IndexOf("\""));
                    if (region == "kr")
                        kdsource = "http://www." + "op.gg/summoner/champions/ajax/champions.most/summonerId=" + kdsource + "&season=11";
                    else
                        kdsource = "http://" + region + ".op.gg/summoner/champions/ajax/champions.most/summonerId=" + kdsource + "&season=11";
                    System.Net.Http.HttpClient client2 = new System.Net.Http.HttpClient();
                    kdsource = await client2.GetStringAsync(kdsource);
                    string url = source.Remove(0, source.IndexOf("ProfileIcon"));
                    url = url.Remove(0, url.IndexOf("<img src=\"//") + 12);
                    url = url.Remove(url.IndexOf("\""));
                    url = "http://" + url;
                    embed.ThumbnailUrl = url;
                    Int32 start = source.IndexOf(SearchText) + SearchText.Length;
                    source = source.Remove(0, start);
                    Int32 end = source.IndexOf(SearchText2);
                    source = source.Remove(end);

                    source = source.Replace("&#039;", "'");
                    if (source.IndexOf("Lv. ") == -1 && source.IndexOf("Unranked") == -1)
                    {
                        string def = "Information for: " + source.Remove(source.IndexOf("/")) + "\n";
                        source = source.Remove(0, source.IndexOf("/") + 1);
                        embed.Title = def;
                        def = "Current Ranking: " + source.Remove(source.IndexOf("/")) + "\n";
                        source = source.Remove(0, source.IndexOf("/") + 1);
                        def = def + "Win Record: " + source.Remove(source.IndexOf("Win")) + "  (";
                        source = source.Remove(0, source.IndexOf("o") + 1);
                        def = def + source.Remove(source.IndexOf("/")) + ")\n\nTop 5 Champions:\n";
                        source = source.Remove(0, source.IndexOf("/") + 1);
                        embed.Description = def;
                        for (int i = 0; i < 4; i++)
                        {
                            if (source.IndexOf(",") != -1)
                            {
                                def = source.Remove(source.IndexOf("Win")) + "(";
                                source = source.Remove(0, source.IndexOf("Win") + 9);
                                def = def + source.Remove(source.IndexOf(",")) + " )";
                                def = def.Remove(def.IndexOf("-")) + def.Remove(0, def.IndexOf("-")).PadRight(30, ' ');
                                source = source.Remove(0, source.IndexOf(",") + 1);
                                kdsource = kdsource.Remove(0, kdsource.IndexOf("span class=\"KDA") + 17);
                                def = def + "KDA: **" + kdsource.Remove(kdsource.IndexOf(":")) + "**     ( ";
                                kdsource = kdsource.Remove(0, kdsource.IndexOf("KDAEach"));
                                kdsource = kdsource.Remove(0, kdsource.IndexOf("Kill") + 6);
                                def = def + kdsource.Remove(kdsource.IndexOf("<"));
                                kdsource = kdsource.Remove(0, kdsource.IndexOf("Death") + 7);
                                def = def + " / " + kdsource.Remove(kdsource.IndexOf("<"));
                                kdsource = kdsource.Remove(0, kdsource.IndexOf("Assist") + 8);
                                def = def + " / " + kdsource.Remove(kdsource.IndexOf("<")) + " )";
                                embed.AddField(def.Remove(def.IndexOf("-")), def.Remove(0, def.IndexOf("-") + 1));

                            }
                        }
                        def = source.Remove(source.IndexOf("Win")) + "  (";
                        source = source.Remove(0, source.IndexOf("Win") + 9);
                        def = def + source + " )";
                        def = def.Remove(def.IndexOf("-")) + def.Remove(0, def.IndexOf("-")).PadRight(30, ' ');
                        kdsource = kdsource.Remove(0, kdsource.IndexOf("span class=\"KDA") + 17);
                        def = def + "KDA: **" + kdsource.Remove(kdsource.IndexOf(":")) + "**     ( ";
                        kdsource = kdsource.Remove(0, kdsource.IndexOf("KDAEach"));
                        kdsource = kdsource.Remove(0, kdsource.IndexOf("Kill") + 6);
                        def = def + kdsource.Remove(kdsource.IndexOf("<"));
                        kdsource = kdsource.Remove(0, kdsource.IndexOf("Death") + 7);
                        def = def + " / " + kdsource.Remove(kdsource.IndexOf("<"));
                        kdsource = kdsource.Remove(0, kdsource.IndexOf("Assist") + 8);
                        def = def + " / " + kdsource.Remove(kdsource.IndexOf("<")) + " )";
                        embed.AddField(def.Remove(def.IndexOf("-")), def.Remove(0, def.IndexOf("-") + 1));
                    }
                    else
                    {
                        await ReplyAsync("That Summoner has not been placed yet this season");
                        return;
                    }
                }
                embed.WithFooter("Made with love");
                embed.WithCurrentTimestamp();
                await ReplyAsync("", false, embed);
            }
        }

        [Command("mastery")]
        [Remarks("Information")]
        [Summary("Gives the number of mastery points for the top 10 most played champions for a user on any server.\nUsage: ~mastery region username")]
        public async Task Mastery(string region, [Remainder]string name)
        {
            var db = new BotBaseContext();
            var embed = new EmbedBuilder();
            var color = db.Variable.Where(V => V.Name == "embedColor").FirstOrDefault();
            embed.WithColor(new Color(Convert.ToUInt32(color.Value, 16)));
            {
                name = name.Replace(" ", string.Empty);
                System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
                string html = "";
                try
                {
                    html = await client.GetStringAsync("https://championmasterylookup.derpthemeus.com/summoner?summoner=" + name + "&region=" + region.ToUpper());
                }
                catch
                {
                    await ReplyAsync("That summoner does not exist");
                    return;
                }
                html = html.Remove(0, html.IndexOf("/img/profile"));
                embed.ThumbnailUrl = "https://championmasterylookup.derpthemeus.com" + html.Remove(html.IndexOf("\""));
                html = html.Remove(0, html.IndexOf("userName=") + 9);
                embed.Title = "Top ten mastery scores for " + (html.Remove(html.IndexOf("\"")).Replace("%20", " "));
                string champ = "";
                string nums = "";
                int count = 0;
                for (int i = 1; i <= 10; i++)
                {
                    if (html.IndexOf("/champion?") == html.IndexOf("/champion?champion=-1"))
                        break;
                    html = html.Remove(0, html.IndexOf("/champion?"));
                    html = html.Remove(0, html.IndexOf(">") + 1);
                    champ = html.Remove(html.IndexOf("<"));
                    champ = champ.Replace("&#x27;", "'");
                    html = html.Remove(0, html.IndexOf("\"") + 1);
                    nums = html.Remove(html.IndexOf("\""));
                    count = count + Convert.ToInt32(nums);
                    for (int j = nums.Length - 3; j > 0; j = j - 3)
                        nums = nums.Remove(j) + "," + nums.Remove(0, j);

                    embed.AddInlineField(i + ". " + champ, nums + " points");
                }

                nums = Convert.ToString(count);
                for (int j = nums.Length - 3; j > 0; j = j - 3)
                    nums = nums.Remove(j) + "," + nums.Remove(0, j);
                embed.Description = "Total score across top ten: " + nums;

                embed.WithFooter("Made with love");
                embed.WithCurrentTimestamp();
                await ReplyAsync("", false, embed);
            }
        }

        [Command("info")]
        [Remarks("Information")]
        [Summary("Gets varying pieces of Discord information for one or more users. Mention a user or provide their id to get their information, or do neither to get your own. To do more than 1 person, separate mentions/ids with spaces.\nUsage: ~info, ~info @person1 @person2, ~info person1id person2id")]
        public async Task MyInfo([Remainder] string ids = "")
        {
            var mention = Context.Message.MentionedUserIds;
            if (mention.Count != 0)
            {
                foreach (ulong id in mention)
                {
                    var embed = ConstructEmbedInfo(await Context.Guild.GetUserAsync(id));
                    await ReplyAsync("", false, embed);
                }
            }
            else if (ids != "")
            {
                string[] idList = ids.Split(' ');
                foreach (string id in idList)
                {
                    var embed = ConstructEmbedInfo(await Context.Guild.GetUserAsync(Convert.ToUInt64(id)));
                    await ReplyAsync("", false, embed);
                }
            }
            else
            {
                var embed = ConstructEmbedInfo(await Context.Guild.GetUserAsync(Context.User.Id));
                await ReplyAsync("", false, embed);
            }
        }

        [Command("avatar")]
        [Remarks("Information")]
        [Summary("Gets the avatar for one or more users. Mention a user or provide their id to get their avatar, or do neither to get your own. To do more than 1 person, separate mentions/ids with spaces.\nUsage: ~avatar, ~avatar @person1 @person2, ~avatar person1id person2id")]
        public async Task Avatar([Remainder] string ids = "")
        {
            var mention = Context.Message.MentionedUserIds;
            if (mention.Count != 0)
            {
                foreach (ulong id in mention)
                {
                    var embed = new EmbedBuilder();
                    IGuildUser user = await Context.Guild.GetUserAsync(id);
                    string url = user.GetAvatarUrl();
                    url = url.Remove(url.IndexOf("?size=128"));
                    url = url + "?size=256";
                    embed.ImageUrl = url;
                    await ReplyAsync("", false, embed);
                }
            }
            else if (ids != "")
            {
                string[] idList = ids.Split(' ');
                foreach (string id in idList)
                {
                    var embed = new EmbedBuilder();
                    IGuildUser user = await Context.Guild.GetUserAsync(Convert.ToUInt64(id));
                    string url = user.GetAvatarUrl();
                    url = url.Remove(url.IndexOf("?size=128"));
                    url = url + "?size=256";
                    embed.ImageUrl = url;
                    await ReplyAsync("", false, embed);
                }
            }
            else
            {
                var embed = new EmbedBuilder();
                string url = Context.User.GetAvatarUrl();
                url = url.Remove(url.IndexOf("?size=128"));
                url = url + "?size=256";
                embed.ImageUrl = url;
                await ReplyAsync("", false, embed);
            }
        }

        [Command("beefact")]
        [Remarks("Information")]
        [Alias("beefacts", "bee", "bees")]
        [Summary("Provides a fact about bees\nUsage: ~beefact, ~beefacts, bee, bees")]
        public async Task beeFact([Remainder] string useless = "")
        {
            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
            string fact = await client.GetStringAsync("http://thebuzz.writeonlymedia.com/api");
            await ReplyAsync(fact.TrimStart('\"').TrimEnd('\"'));
        }

        [Command("message")]
        [Remarks("Customization")]
        [Summary("Displays your previously set message. To set a message, use the ~setmessage command.\nUsage: ~message")]
        public async Task Message()
        {
            var db = new BotBaseContext();
            var message = db.Message.Where(msg => msg.UserId == Context.User.Id).FirstOrDefault();
            var config = db.Configuration.Where(cfg => cfg.Name == configName).First();
            if (message == null)
                await ReplyAsync($"User does not have a message yet! use {config.Token}setmessage to set a message.");
            else
                await ReplyAsync(message.Message1);
        }

        [Command("setmessage")]
        [Remarks("Customization")]
        [Summary("Allows you to set a message that can be displayed at any time using the ~message command.\nUsage: ~setmessage write your message here")]
        public async Task SetMessage([Remainder]string mess)
        {
            var db = new BotBaseContext();
            var message = db.Message.Where(msg => msg.UserId == Context.User.Id).FirstOrDefault();
            var user = db.User.Where(usr => usr.UserId == Context.User.Id).FirstOrDefault();
            if (user == null)
                db.Add(new User { UserId = Context.User.Id, Name = Context.User.Username, Number = long.Parse(Context.User.Discriminator) });
            else
            {
                user.Name = Context.User.Username;
                user.Number = long.Parse(Context.User.Discriminator);
            }
            if (message == null)
                db.Add(new Message { UserId = Context.User.Id, Message1 = mess});
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
        [Remarks("Customization")]
        [Summary("Sets for a specific emote to be reacted to every message you send. To remove a signature, call the command without specifying an emote, or using the emote you already have set. NOTE: Jif Bot does NOT have nitro, this will only work with emotes that are available on this server. \nUsage: ~togglesignature :poop:")]
        public async Task ToggleSignature([Remainder] string sig = "")
        {
            var db = new BotBaseContext();
            var signature = db.Signature.Where(s => s.UserId == Context.User.Id).FirstOrDefault();
            var user = db.User.Where(usr => usr.UserId == Context.User.Id).FirstOrDefault();
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
        [Remarks("Customization")]
        [Summary("Toggles between enabling and disabling reactions for the channel the command was issued in. Reactions are set keywords that Jif Bot will respond to. This does not include commands. To disable/enable for all channels, follow the command with \"all\". If there is at least one channel in which reactions are disabled when using \"all\", all channels will be enabled, otherwise, all will be disabled. Only the server owner can execute this command.\nUsage: ~togglereactions, ~togglereactions all")]
        public async Task ToggleReactions([Remainder] string args = "")
        {
            if( Context.Guild.OwnerId != Context.User.Id )
            {
                await ReplyAsync("Command can only be used by server owner");
                return;
            }

            var db = new BotBaseContext();
            if (args.ToLower().Contains("all"))
            {
                var channels = db.ReactionBan.Where(c => c.ServerId == Context.Guild.Id).ToList();
                if(channels.Count == 0)
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
                var channel = db.ReactionBan.Where(s => s.ChannelId == Context.Channel.Id).FirstOrDefault();
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
        [Remarks("Customization")]
        [Summary("Sets a channel to send messages to when new users join the server. To remove, issue the command in the channel the welcome is currently set to. Only the server owner can execute this command.\nUsage: ~setwelcome")]
        public async Task SetWelcome([Remainder] string args = "")
        {
            if (Context.Guild.OwnerId != Context.User.Id)
            {
                await ReplyAsync("Command can only be used by server owner");
                return;
            }

            var db = new BotBaseContext();
            var config = db.ServerConfig.Where(s => s.ServerId == Context.Guild.Id).FirstOrDefault();
            if(config != null)
            {
                if(config.JoinId == Context.Channel.Id)
                {
                    config.JoinId = 0;
                    await ReplyAsync($"Welcome messages will no longer be sent in {Context.Channel.Name}");
                }
                else
                {
                    var old = await Context.Guild.GetTextChannelAsync(config.JoinId);
                    config.JoinId = Context.Channel.Id;
                    if(old != null)
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
        [Remarks("Customization")]
        [Summary("Sets a channel to send messages to when users leave the server. To remove, issue the command in the channel the goodbye is currently set to. Only the server owner can execute this command.\nUsage: ~setgoodbye")]
        public async Task SetGoodbye([Remainder] string args = "")
        {
            if (Context.Guild.OwnerId != Context.User.Id)
            {
                await ReplyAsync("Command can only be used by server owner");
                return;
            }
            
            var db = new BotBaseContext();
            var config = db.ServerConfig.Where(s => s.ServerId == Context.Guild.Id).FirstOrDefault();
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
        [Remarks("Customization")]
        [Summary("Sets a channel to send messages to whenever a message gets deleted in the server. To remove, issue the command in the channel the goodbye is currently set to. Only the server owner can execute this command.\nUsage: ~setsnoop")]
        public async Task SetMessageReport([Remainder] string args = "")
        {
            if (Context.Guild.OwnerId != Context.User.Id)
            {
                await ReplyAsync("Command can only be used by server owner");
                return;
            }

            var db = new BotBaseContext();
            var config = db.ServerConfig.Where(s => s.ServerId == Context.Guild.Id).FirstOrDefault();
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

        [Command("reese")]
        [Remarks("Miscellaneous")]
        [Summary("Prompts ladies to hit him up.\nUsage: ~reese")]
        public async Task Reese()
        {
            await Context.Channel.SendMessageAsync("Ladies hmu");
        }

        [Command("lobster")]
        [Remarks("Miscellaneous")]
        [Summary("Displays the best image on the internet.\nUsage: ~lobster")]
        public async Task Lobster()
        {
            await Context.Channel.SendFileAsync("media/lobster.jpg");
        }

        [Command("neeko")]
        [Remarks("Miscellaneous")]
        [Summary("A command to celebrate Neeko.\nUsage: ~neeko")]
        public async Task Neko()
        {
            await Context.Channel.SendFileAsync("media/neeko.jpg");
        }

        [Command("smoochie")]
        [Remarks("Miscellaneous")]
        [Summary("Reese gives a smoochie.\nUsage: ~smoochie")]
        public async Task Smoochie()
        {
            await Context.Channel.SendFileAsync("media/smoochie.mp4");
        }

        [Command("flat")]
        [Remarks("Miscellaneous")]
        [Summary("Heralds the unseen truth.\nUsage: ~flat")]
        public async Task Flat()
        {
            await Context.Channel.SendFileAsync("media/flat.png");
        }

        [Command("attention")]
        [Remarks("Miscellaneous")]
        [Summary("Gives Dee the attention she craves.\nUsage: ~attention")]
        public async Task Attention()
        {
            await Context.Channel.SendFileAsync("media/attention.gif");
        }

        [Command("shrug")]
        [Remarks("Miscellaneous")]
        [Summary("Shrugs.\nUsage: ~shrug")]
        public async Task Shrug()
        {
            await Context.Channel.SendFileAsync("media/shrug.png");
        }

        [Command("lunch")]
        [Remarks("Miscellaneous")]
        [Summary("lunch.\nUsage: ~lunch")]
        public async Task Lunch()
        {
            await Context.Channel.SendFileAsync("media/lunch.gif");
        }

        [Command("banterwtf")]
        [Remarks("Miscellaneous")]
        [Summary("A video to be played when Banter does something stupid.\nUsage: ~banterwtf")]
        public async Task BanterWTF()
        {
            await ReplyAsync("https://www.youtube.com/watch?v=-qRsiHfWh1w");
        }

        [Command("rammus")]
        [Remarks("Miscellaneous")]
        [Summary("PRAISE RAMMUS.\nUsage: ~rammus")]
        public async Task Rammus()
        {
            await Context.Channel.SendFileAsync("media/rammus.png");
            await ReplyAsync("**P  R  A  I  S  E          R  A  M  M  U  S**");
        }

        [Command("whisper")]
        [Remarks("Miscellaneous")]
        [Summary("Sends a private message to someone on the server. The message containing your command call will be deleted for anonymity. NOTE: the \"name\" is the person's Discord username without the numbers.\nUsage: ~whisper \"name\" message")]
        public async Task Whisper([Remainder]string contents)
        {
            int spot = contents.IndexOf("\"");
            if (spot == -1)
                await ReplyAsync("improper usage, please use: ~whisper \"username\" message. Do not include numbers with the user name.");
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
        [Remarks("Miscellaneous")]
        [Summary("Reports the number of times Jif has said \"I mean\"\nUsage: ~meancount")]
        public async Task meanCount([Remainder] string useless = "")
        {
            var db = new BotBaseContext();
            var count = db.Variable.Where(v => v.Name == "meanCount").First();
            await ReplyAsync("I mean, I've said it " + count.Value + " times since 12/13/18.");
        }

        [Command("honkcount")]
        [Remarks("Miscellaneous")]
        [Summary("Reports the number of times you have said honk\"\nUsage: ~honkcount")]
        public async Task honkCount([Remainder] string useless = "")
        {
            var db = new BotBaseContext();
            var honk = db.Honk.Where(user => user.UserId == Context.Message.Author.Id).FirstOrDefault();
            if (honk != null)
                await ReplyAsync($"You have honked {honk.Count} times!");
            else
                await ReplyAsync("You have never honked! For shame!");
        }

        [Command("totalhonks")]
        [Remarks("Miscellaneous")]
        [Summary("Reports the total number of honks accross all users\"\nUsage: ~honkcount")]
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
        [Remarks("Miscellaneous")]
        [Summary("Reports the top 5 users who have honked the most\"\nUsage: ~honkcount")]
        public async Task honkBoard([Remainder] string useless = "")
        {
            var db = new BotBaseContext();
            var honks = db.Honk.OrderByDescending(honk => honk.Count);
            int count = 1;
            string message = "";
            foreach (Honk honk in honks)
            {
                var user = db.User.Where(user => user.UserId == honk.UserId).FirstOrDefault();
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

        [Command("imean")]
        [Remarks("Hidden")]
        [Summary("Adds a tally to the number of times Jif has said \"I mean\"\nUsage: ~imean")]
        public async Task iMean([Remainder] string useless = "")
        {
            string file = "references/mean.txt";
            Int32 num = Convert.ToInt32(File.ReadAllText(file));
            num++;
            await ReplyAsync("<@150084781864910848> you've said \"I mean\" " + num + " times.");
            File.WriteAllText(file, Convert.ToString(num));
        }

        [Command("gnomed")]
        [Remarks("Hidden")]
        [Summary("I'm gnot a gnelf...\nUsage: ~gnomed")]
        public async Task Gnomed()
        {
            await ReplyAsync("https://www.youtube.com/watch?v=6n3pFFPSlW4");
        }

        [Command("rolligentle")]
        [Remarks("Hidden")]
        [Summary("Makes the Gentlecat do a rollie\nUsage: ~rolligentle")]
        public async Task RolliCat([Remainder] string useless = "")
        {
            await ReplyAsync("<:gentlecat:302907277571260418> <:rightcat:455100361066283035> <:bottomcat:455100361120940032> <:leftcat:455100361187786752> <:gentlecat:302907277571260418>");
        }


        [Command("metroman")]
        [Remarks("Hidden")]
        [Summary("Spreads the good word\nUsage: ~metroman")]
        public async Task MetroMan([Remainder] string useless = "")
        {
            await ReplyAsync("https://www.youtube.com/watch?v=W7urgEgXgyg");
        }

        async Task<bool> RemoteFileExists(string url)
        {
            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
            try
            {
                string response = await client.GetStringAsync(url);
                if (response.Length == 0) return false;
                else
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public string getSmol(char orig)
        {
            switch (orig)
            {
                case 'e':
                    return "ᵉ";
                case 't':
                    return "ᵗ";
                case 'a':
                    return "ᵃ";
                case 'o':
                    return "ᵒ";
                case 'i':
                    return "ᶦ";
                case 'n':
                    return "ᶰ";
                case 's':
                    return "ˢ";
                case 'r':
                    return "ʳ";
                case 'h':
                    return "ʰ";
                case 'd':
                    return "ᵈ";
                case 'l':
                    return "ᶫ";
                case 'u':
                    return "ᵘ";
                case 'c':
                    return "ᶜ";
                case 'm':
                    return "ᵐ";
                case 'f':
                    return "ᶠ";
                case 'y':
                    return "ʸ";
                case 'w':
                    return "ʷ";
                case 'g':
                    return "ᵍ";
                case 'p':
                    return "ᵖ";
                case 'b':
                    return "ᵇ";
                case 'v':
                    return "ᵛ";
                case 'k':
                    return "ᵏ";
                case 'x':
                    return "ˣ";
                case 'q':
                    return "ᑫ";
                case 'j':
                    return "ʲ";
                case 'z':
                    return "ᶻ";
            }
            return Convert.ToString(orig);
        }

        public string getBig(char orig)
        {
            switch (orig)
            {
                case 'e':
                    return "🇪";
                case 't':
                    return "🇹";
                case 'a':
                    return "🇦";
                case 'o':
                    return "🇴";
                case 'i':
                    return "🇮";
                case 'n':
                    return "🇳";
                case 's':
                    return "🇸";
                case 'r':
                    return "🇷";
                case 'h':
                    return "🇭";
                case 'd':
                    return "🇩";
                case 'l':
                    return "🇱";
                case 'u':
                    return "🇺";
                case 'c':
                    return "🇨";
                case 'm':
                    return "🇲";
                case 'f':
                    return "🇫";
                case 'y':
                    return "🇾";
                case 'w':
                    return "🇼";
                case 'g':
                    return "🇬";
                case 'p':
                    return "🇵";
                case 'b':
                    return "🅱";
                case 'v':
                    return "🇻";
                case 'k':
                    return "🇰";
                case 'x':
                    return "🇽";
                case 'q':
                    return "🇶";
                case 'j':
                    return "🇯";
                case 'z':
                    return "🇿";
                case ' ':
                    return "  ";
            }
            return Convert.ToString(orig);
        }

        public string FormatTime(DateTimeOffset orig)
        {
            string str = "";
            str = str + orig.LocalDateTime.DayOfWeek + ", ";
            str = str + orig.LocalDateTime.Month + "/" + orig.LocalDateTime.Day + "/" + orig.LocalDateTime.Year;
            str = str + " at " + orig.LocalDateTime.Hour + ":" + orig.LocalDateTime.Minute + " CST";
            return str;
        }

        public EmbedBuilder ConstructEmbedInfo(IGuildUser user)
        {
            var db = new BotBaseContext();
            var embed = new EmbedBuilder();
            var color = db.Variable.Where(V => V.Name == "embedColor").FirstOrDefault();
            embed.WithColor(new Color(Convert.ToUInt32(color.Value, 16)));
            embed.WithAuthor(user.Username + "#" + user.Discriminator, user.GetAvatarUrl());
            embed.ThumbnailUrl = user.GetAvatarUrl();
            embed.AddField("User ID", user.Id);
            if (user.Nickname == null)
                embed.AddField("Nickname", user.Username);
            else
                embed.AddField("Nickname", user.Nickname);
            if (user.Game == null)
                embed.AddField("Currently Playing", "[nothing]");
            else
                embed.AddField("Currently Playing", user.Game);
            embed.AddField("Account Creation Date", FormatTime(user.CreatedAt));
            embed.AddField("Server Join Date", FormatTime(user.JoinedAt.Value));
            string roles = "";
            foreach (ulong id in user.RoleIds)
            {
                if (roles != "")
                    roles = roles + ", ";
                if (Context.Guild.GetRole(id).Name != "@everyone")
                    roles = roles + Context.Guild.GetRole(id).Name;
            }
            embed.AddField("Roles", roles);
            embed.WithFooter("Made with love");

            embed.WithCurrentTimestamp();
            return embed;
        }
        string formatMinutesToString(int minutes)
        {
            string format = "";

            if (minutes / 10080 > 0)
            {
                format += Convert.ToString(minutes / 10080) + " week";
                if (minutes / 10080 > 1)
                    format += "s";
                format += ", ";
                minutes = minutes % 10080;
            }

            if (minutes / 1440 > 0)
            {
                format += Convert.ToString(minutes / 1440) + " day";
                if (minutes / 1440 > 1)
                    format += "s";
                format += ", ";
                minutes = minutes % 1440;
            }

            if (minutes / 60 > 0)
            {
                format += Convert.ToString(minutes / 60) + " hour";
                if (minutes / 60 > 1)
                    format += "s";
                format += ", ";
                minutes = minutes % 60;
            }

            if (minutes > 0)
            {
                format += Convert.ToString(minutes) + " minute";
                if (minutes > 1)
                    format += "s";
            }
            else
                format = format.Remove(format.Length - 2, 2);

            return format;
        }
    }

    class UrbanDictionaryDefinition
    {
        public string Definition { get; set; }
        public string Example { get; set; }
        public string Word { get; set; }
        public string Written_On { get; set; }
    }

    class UrbanDictionaryResult
    {
        public List<UrbanDictionaryDefinition> List { get; set; }
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