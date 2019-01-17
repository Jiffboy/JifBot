using System;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Net;
using Discord;
using Discord.WebSocket;
using Discord.Commands;

namespace JifBot.Modules.Public
{
    public class PublicModule : ModuleBase
    {

        [Command("reese")]
        [Remarks("Prompts ladies to hit him up.\nUsage: ~reese")]
        public async Task Reese()
        {
            await Context.Channel.SendMessageAsync("Ladies hmu");
        }

        [Command("commands")]
        [Remarks("Displays available commands.\nUsage: ~commands")]
        public async Task Commands()
        {
            string file = "references/CommandTemplate.txt";
            string temp = File.ReadAllText(file);
            var embed = new EmbedBuilder();
            embed.WithColor(new Color(0x42ebf4));
            embed.Title = "All commands will begin with a tilde (~), for more information on individual commands, use: ~help commandName";
            embed.Description = "Contact Jif#3952 with any suggestions for more commands";
            embed.WithFooter("Made with love");
            while (temp != "")
            {
                temp = temp.Remove(0, temp.IndexOf("--") + 2);
                string cat = temp.Remove(temp.IndexOf("\r\n"));
                string com = "";
                temp = temp.Remove(0, temp.IndexOf("\r\n") + 2);
                while (temp.IndexOf("--") != temp.IndexOf("\r\n") + 2 && temp != "")
                {
                    com = com + temp.Remove(temp.IndexOf("\r\n")) + ", ";
                    temp = temp.Remove(0, temp.IndexOf("\r\n") + 2);
                }
                if (temp != "")
                    com = com + temp.Remove(temp.IndexOf("\r\n"));
                else
                    com = com.Remove(com.Length - 2);
                embed.AddField(cat, com);

            }
            await ReplyAsync("", false, embed);
        }

        [Command("neeko")]
        [Remarks("A command to celebrate Neeko.\nUsage: ~neeko")]
        public async Task Neko()
        {
            await Context.Channel.SendFileAsync("reactions/neeko.jpg");
        }

        [Command("wtf")]
        [Remarks("Shows your disbelief as to what your fellow server goers have just done.\nUsage: ~wtf")]
        public async Task WTF()
        {
            await ReplyAsync("https://www.youtube.com/watch?v=wKbU8B-QVZk");
        }

        [Command("smoochie")]
        [Remarks("Reese gives a smoochie.\nUsage: ~smoochie")]
        public async Task Smoochie()
        {
            await ReplyAsync("https://gyazo.com/8c51b11102ceb47e8be54653c905b97f");
        }

        [Command("flat")]
        [Remarks("Heralds the unseen truth.\nUsage: ~flat")]
        public async Task Flat()
        {
            await ReplyAsync("https://prnt.sc/ht1d1v");
        }

        [Command("shrug")]
        [Remarks("Shrugs.\nUsage: ~shrug")]
        public async Task Shrug()
        {
            await Context.Channel.SendFileAsync("reactions/shrug.png");
        }

        [Command("attention")]
        [Remarks("Gives Dee the attention she craves.\nUsage: ~attention")]
        public async Task Attention()
        {
            await ReplyAsync("https://giphy.com/gifs/glass-milk-n0xHORz5gp904");
        }

        [Command("neener")]
        [Remarks("Helps to prove your point that you were right.\nUsage: ~neener")]
        public async Task ToldYou()
        {
            await ReplyAsync("https://giphy.com/gifs/kawaii-aegyo-4QxQgWZHbeYwM");
        }

        [Command("lunch")]
        [Remarks("lunch.\nUsage: ~lunch")]
        public async Task Lunch()
        {
            await ReplyAsync("https://media.tenor.com/images/71b9ce5ea465c7fa0808553d1f9e8e3c/tenor.gif");
        }

        [Command("banterwtf")]
        [Remarks("A video to be played when Banter does something stupid.\nUsage: ~banterwtf")]
        public async Task BanterWTF()
        {
            await ReplyAsync("https://www.youtube.com/watch?v=-qRsiHfWh1w");
        }

        [Command("bully")]
        [Remarks("Reminds young rapscallions that this is a bully free zone.\nUsage ~bully")]
        public async Task Bully()
        {
            await Context.Channel.SendFileAsync("reactions/bully.gif");
        }

        [Command("stfu")]
        [Remarks("Tells someone to shut up.\nUsage ~stfu")]
        public async Task STFU()
        {
            await Context.Channel.SendFileAsync("reactions/stfu.jpg");
        }

        [Command("rammus")]
        [Remarks("PRAISE RAMMUS.\nUsage ~rammus")]
        public async Task Rammus()
        {
            await Context.Channel.SendFileAsync("reactions/rammus.png");
            await ReplyAsync("**P  R  A  I  S  E          R  A  M  M  U  S**");
        }

        [Command("edgy")]
        [Remarks("Informs someone that their prior sent comment was perhaps a tad too mischievous.\nUsage ~edgy")]
        public async Task Edgy()
        {
            await Context.Channel.SendFileAsync("reactions/edgy.jpg");
        }

        [Command("invitelink")]
        [Remarks("Provides a link which can be used should you want to spread Jif Bot to another server.\nUsage ~invitelink")]
        public async Task InviteLink()
        {
            await ReplyAsync("The following is a link to add me to another server. NOTE: You must have permissions on the server in order to add. Once on the server I must be given permission to send and delete messages, otherwise I will not work.\nhttps://discordapp.com/oauth2/authorize?client_id=315569278101225483&scope=bot");
        }

        [Command("streamers")]
        [Remarks("Displays everybody on the server who is currently streaming\nUsage ~streamers")]
        public async Task Stream()
        {
            bool found = false;
            var embed = new EmbedBuilder();
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

        [Command("sorry")]
        [Remarks("A command to help you to articulate your regret for your actions.\nUsage: ~sorry")]
        public async Task Sorry()
        {
            await ReplyAsync("I'm writing this message cause I feel really bad, thinking about the way I hurt you makes me really sad. I'm sorry for all the hurt I've caused you and I regret the things I've done. I've lost the 1 girl I've ever loved and it was cause of the things I've done. Baby I feel so bad right now, cause I tore your world apart, and now all I can think about is how I broke your heart. These tears that run down my cheek are filled with sadness and hurt, because I loved you so much and now I know that it will never work :( I messed up and now I see that you mean the absolute world to me. I know sorry's not enough because I'm such a screw up.. But for whatever its worth I wanted to say, that you cross my mind every single day...The thought of you makes me smile, and I know our love was real, so I'm writing you this letter so that you know how I truly feel. What I really want to say is that I'm sorry, I know that you didn't deserve to be hurt like that, and I know that you will find someone who will love you and treat you right, they will make you happy and that person won't hurt you like I did.");
        }

        [Command("doghouse")]
        [Remarks("A command to be used when someone has been enslaved by their female counterpart.\nUsage: ~doghouse name")]
        public async Task Doghouse([Remainder]string name)
        {
            await ReplyAsync("<:doghouse:305246514467438602> Oh no! <:doghouse:305246514467438602>\n<:doghouse:305246514467438602> Freedom is down the drain! <:doghouse:305246514467438602>\n<:doghouse:305246514467438602> That's right! <:doghouse:305246514467438602>\n<:doghouse:305246514467438602> " + name + " is in the doghouse again! <:doghouse:305246514467438602>");
        }

        [Command("cheer")]
        [Remarks("Displays one of several gifs of cute characters cheering you on.\nUsage: ~cheer")]
        public async Task Cheer()
        {
            Random rnd = new Random();
            int num = rnd.Next(10);
            string gif = "cheer/cheer" + num + ".gif";
            await Context.Channel.SendFileAsync(gif);
        }

        [Command("lewd")]
        [Remarks("Displays a random image to react to someones lewd comment.\nUsage: ~lewd")]
        public async Task Lewd()
        {
            Random rnd = new Random();
            int num = rnd.Next(8);
            string png = "lewd/" + num + ".png";
            await Context.Channel.SendFileAsync(png);
        }

        [Command("setmessage")]
        [Remarks("Allows you to set a message that can be displayed at any time using the ~message command.\nUsage: ~setmessage write your message here")]
        public async Task SetMessage([Remainder]string mess)
        {
            string file = "references/messages.txt";
            string name = Context.User.Username + "#" + Context.User.Discriminator;
            string id = Convert.ToString(Context.User.Id);
            string temp = File.ReadAllText(file);
            if (temp.IndexOf(id) != -1)
            {
                await ReplyAsync("This user already has a message!");
                return;
            }
            temp = temp + id + " " + mess + "\r\n\r\n";
            File.WriteAllText(file, temp);
            await ReplyAsync("added message: \"" + mess + "\" for user: " + name);
        }

        [Command("setsignature")]
        [Remarks("Sets for a specific emote to be reacted to every message you send. NOTE: Jif Bot does NOT have nitro, this will only work with emotes that are available on this server. \nUsage: ~setmessage :poop:")]
        public async Task SetSignature(string mess, [Remainder]string nogo = "")
        {
            string messOrig = mess;
            if (nogo != "")
                await ReplyAsync("This currently only works for one emote, " + nogo + " will not be added.");
            mess = mess.Replace("<", string.Empty);
            mess = mess.Replace(">", string.Empty);
            string file = "references/signatures.txt";
            string name = Context.User.Username + "#" + Context.User.Discriminator;
            string id = Convert.ToString(Context.User.Id);
            string temp = File.ReadAllText(file);
            if (temp.IndexOf(id) != -1)
            {
                await ReplyAsync("This user already has a signature!");
                return;
            }
            temp = temp + id + " " + mess + "\r\n\r\n";
            File.WriteAllText(file, temp);
            await ReplyAsync("added signature: \"" + messOrig + "\" for user: " + name);
        }

        [Command("resetsignature")]
        [Remarks("Removes your signature. If you do not have a signature, use the ~setsignature command\nUsage: ~resetmessage")]
        public async Task ResetSignature()
        {
            string name = Context.User.Username + "#" + Context.User.Discriminator;
            string id = Convert.ToString(Context.User.Id);
            string file = "references/signatures.txt";
            string source = File.ReadAllText(file);
            string temp = source;
            Int32 start = source.IndexOf(id);
            if (start == -1)
            {
                await ReplyAsync("User does not have a signature");
                return;
            }
            temp = temp.Remove(0, start);
            string end = "\r\n\r\n";
            Int32 finish = temp.IndexOf(end) + end.Length;
            temp = temp.Remove(temp.IndexOf(end));
            temp = temp.Remove(0, name.Length);
            source = source.Remove(start, finish);
            File.WriteAllText(file, source);
            await ReplyAsync("removed signature: \"" + temp + "\" from user: " + name);
        }

        [Command("message")]
        [Remarks("Displays your previously set message. To set a message, use the ~setmessage command.\nUsage: ~message")]
        public async Task Message()
        {
            string file = "references/messages.txt";
            string name = Context.User.Username + "#" + Context.User.Discriminator;
            string id = Convert.ToString(Context.User.Id);
            string temp = File.ReadAllText(file);
            Int32 start = temp.IndexOf(id);
            if (start == -1)
            {
                await ReplyAsync("User has not set a message yet! use ~setmessage [message] to set your message.");
                return;
            }
            start = start + id.Length;
            temp = temp.Remove(0, start);
            string end = "\r\n\r\n";
            start = temp.IndexOf(end);
            temp = temp.Remove(start);
            await ReplyAsync(temp);
        }

        [Command("resetmessage")]
        [Remarks("Deletes your currently set message. If you do not have a message, use the ~setmessage command\nUsage: ~resetmessage")]
        public async Task ResetMessage()
        {
            string name = Context.User.Username + "#" + Context.User.Discriminator;
            string id = Convert.ToString(Context.User.Id);
            string file = "references/messages.txt";
            string source = File.ReadAllText(file);
            string temp = source;
            Int32 start = source.IndexOf(id);
            if (start == -1)
            {
                await ReplyAsync("User does not have a message");
                return;
            }
            temp = temp.Remove(0, start);
            string end = "\r\n\r\n";
            Int32 finish = temp.IndexOf(end) + end.Length;
            temp = temp.Remove(temp.IndexOf(end));
            temp = temp.Remove(0, id.Length);
            source = source.Remove(start, finish);
            File.WriteAllText(file, source);
            await ReplyAsync("removed message: \"" + temp + "\" from user: " + name);
        }

        [Command("ffish")]
        [Remarks("")]
        public async Task ffish([Remainder] string fish)
        {
            var embed = new EmbedBuilder();
            embed.WithColor(new Color(0x42ebf4));
            fish = fish.Replace(" ", "_");
            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
            string html = "";
            try
            {
                html = await client.GetStringAsync("https://ffxiv.gamerescape.com/wiki/" + fish);
            }
            catch
            {
                await ReplyAsync("Invalid Fish");
                return;
            }
            embed.ThumbnailUrl = "https://ffxiv.gamerescape.com/w/images/e/e5/" + fish + "_Icon.png";

            embed.WithFooter("Made with love");
            embed.WithCurrentTimestamp();
            await ReplyAsync("", false, embed);
        }

        [Command("mock")]
        [Remarks("Deletes your command call to keep anonymity and then mocks the message that you give it. If you do not specify any message, it will mock the most recent message sent in the text channel.\nUsage: ~mock message")]
        public async Task Mock([Remainder] string words = "")
        {
            await Context.Message.DeleteAsync();
            if (words == "")
            {
                var msg = Context.Channel.GetMessagesAsync(2).Flatten().Result;
                words = msg.ElementAt(0).Content;
                if (words.ToLower() == "~mock")
                    words = msg.ElementAt(1).Content;
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

        [Command("whisper")]
        [Remarks("Sends a private message to someone on the server. The message containing your command call will be deleted for anonymity. NOTE: the \"name\" is the person's Discord username without the numbers.\nUsage: ~whisper \"name\" message")]
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
                IGuildUser test = null;
                var list = Context.Guild.GetUsersAsync();
                for (int i = 0; i < list.Result.Count; i++)
                {
                    if (list.Result.ElementAt(i).Username.ToLower() == contents.ToLower())
                        test = list.Result.ElementAt(i);
                }
                if (test == null)
                {
                    await ReplyAsync("That is not a name for anybody on this server. Your message was not sent");
                    Console.WriteLine(user + " attempted to send \"" + copy + "\" to " + contents);
                }
                else
                {
                    await test.SendMessageAsync(copy);
                    Console.WriteLine(user + " successfully sent \"" + copy + "\" to " + contents);
                }
            }
        }
        [Command("define")]
        [Remarks("Defines any SINGLE word in the English language. Will not define proper nouns.\nUsage: ~define word")]
        public async Task Define(string word, [Remainder]string word2 = "")
        {
            if (word2 != "")
                await ReplyAsync("__**NOTE**__: This command only works with single words. Defining for: " + word);
            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
            string source = "";
            source = await client.GetStringAsync("http://dictionary.com/browse/" + word);
            word = word.First().ToString().ToUpper() + word.Substring(1);
            string search = word + " definition,";
            source = source.Remove(0, source.IndexOf(search) + search.Length);
            if(source.Contains("See more.\">"))
                source = source.Remove(source.IndexOf("See more.\">"));
            else
            {
                await ReplyAsync("https://i.imgur.com/dwBlFY5.jpg");
                return;
            }
            await ReplyAsync(source);
        }

        [Command("udefine")]
        [Remarks("Gives the top definition for the term from urbandictionary.com\nUsage: ~udefine phrase")]
        public async Task DefineUrban([Remainder]string phrase)
        {
            phrase = phrase.Replace(" ", "+");
            string source = "";
            string start = "property=\"fb:app_id\"><meta content=\"";
            string end = "\" name=\"Description\" property=\"og:description\"";
            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
            if (await RemoteFileExists("https://www.urbandictionary.com/define.php?term=" + phrase))
                source = await client.GetStringAsync("https://www.urbandictionary.com/define.php?term=" + phrase);
            else
            {
                await ReplyAsync("\"" + phrase.Replace("+", " ") + "\" is not an existing word/phrase");
                return;
            }
            if (source.Contains("<p>There aren't any definitions for <i>"))
            {
                await ReplyAsync("there are no entries for \"" + phrase.Replace("+", " ") + "\"");
                return;
            }
            string def = "";
            string ex = "";
            string sendstr = "";
            if (source.Contains(start))
            {
                def = source.Remove(0, source.IndexOf(start) + start.Length);
                def = def.Remove(def.IndexOf(end));
                start = "</div><div class=\"example\">";
                end = "</div><div class=\"tags\">";
                ex = source.Remove(0, source.IndexOf(start) + start.Length);
                if (ex.Contains(end))
                    ex = ex.Remove(ex.IndexOf(end));
            }
            else
            {
                def = source.Remove(0, source.IndexOf("<div class='meaning'>") + "<div class='meaning'>".Length);
                def = def.Remove(def.IndexOf("</div>"));
                ex = source.Remove(0, source.IndexOf("<div class='example'>") + "<div class='example'>".Length);
                ex = ex.Remove(ex.IndexOf("</div>"));
            }

            sendstr = "Definition:\n" + def + "\n\nExample:\n" + ex;

            sendstr = sendstr.Replace("&quot;", "\"");
            sendstr = sendstr.Replace("<br/>", "\n");
            sendstr = sendstr.Replace("&apos;", "'");
            sendstr = sendstr.Replace("&amp;", "&");
            sendstr = sendstr.Replace("&lt;", "<");
            sendstr = sendstr.Replace("&gt;", ">");
            sendstr = sendstr.Replace("quot;", "\"");
            sendstr = sendstr.Replace("&#39;", "'");

            if (sendstr.Contains("</div><div class=\"contributor\">"))
                sendstr = sendstr.Remove(sendstr.IndexOf("</div><div class=\"contributor\">"));

            while (sendstr.Contains("<a class=") && sendstr.Contains("\">"))
                sendstr = sendstr.Remove(sendstr.IndexOf("<a class="), sendstr.IndexOf("\">") + 2 - sendstr.IndexOf("<a class="));
            while (sendstr.Contains("<a href=") && sendstr.Contains("\">"))
            {
                if (sendstr.Contains("</div><div class="))
                    sendstr = sendstr.Remove(sendstr.IndexOf("</div><div class="), sendstr.IndexOf("</a></div>") + 10 - sendstr.IndexOf("</div><div class="));
                else
                    sendstr = sendstr.Remove(sendstr.IndexOf("<a href="), sendstr.IndexOf("\">") + 2 - sendstr.IndexOf("<a href="));
            }

            sendstr = sendstr.Replace("</a>", string.Empty);
            if (sendstr.Replace(" ", string.Empty).EndsWith("Example:\n"))
                sendstr = sendstr.Remove(sendstr.IndexOf("\n\nExample:\n"));

            while (sendstr.Length >= 2000)
            {
                await ReplyAsync(sendstr.Remove(2000));
                sendstr = sendstr.Remove(0, 2000);
            }

            await ReplyAsync(sendstr);
        }
        [Command("stats")]
        [Remarks("Gives the stats for a league player on any region. The region name is the abbreviated verson of the region name. Example: na = North America\nUsage ~stats region username")]
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
                var embed = new EmbedBuilder();
                embed.WithColor(new Color(0x42ebf4));
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

        [Command("joke")]
        [Remarks("Tells a joke\nUsage: ~joke")]
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
        [Remarks("Gives an inspirational quote\nUsage: ~inspire")]
        public async Task Inspire()
        {
            string file1 = "references/quotes.txt";
            string file2 = "references/authors.txt";
            string allQuotes = File.ReadAllText(file1);
            string allAuthors = File.ReadAllText(file2);
            int count = Regex.Matches(allQuotes, "\",\"").Count + 1;
            Random rnd = new Random();
            int num = rnd.Next(count);
            for (int i = 0; i < num; i++)
            {
                if (i == count - 1)
                    break;
                allQuotes = allQuotes.Remove(0, allQuotes.IndexOf("\",\"") + 3);
                allAuthors = allAuthors.Remove(0, allAuthors.IndexOf("\",\"") + 3);
            }
            if (num != count)
            {
                allQuotes = allQuotes.Remove(allQuotes.IndexOf("\",\""));
                allAuthors = allAuthors.Remove(allAuthors.IndexOf("\",\""));
            }
            else
            {
                allQuotes = allQuotes.Replace("\"", string.Empty);
                allAuthors = allAuthors.Replace("\"", string.Empty);
            }
            await ReplyAsync("\"" + allQuotes + "\"\n-" + allAuthors);

        }

        [Command("bigtext")]
        [Remarks("Takes the user input for messages and turns it into large letters using emotes. Your command call is deleted upon use.\nUsage: ~bigtext phrase")]
        public async Task bigtext([Remainder]string orig)
        {
            await Context.Message.DeleteAsync();
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
        [Remarks("Takes the user input for messages and turns it into small letters. Your command call is deleted upon use.\nUsage: ~tinytext phrase")]
        public async Task tinytext([Remainder]string orig)
        {
            await Context.Message.DeleteAsync();
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
        [Remarks("Takes the user input for messages and turns it into a ＷＩＤＥ  ＢＯＩ. Your command call is deleted upon use.\nUsage: ~widetext phrase")]
        public async Task WideText([Remainder] string message)
        {
            await Context.Message.DeleteAsync();
            message = message.Replace(" ", "   ");
            string alpha = "QWERTYUIOPASDFGHJKLÇZXCVBNMqwertyuiopasdfghjklçzxcvbnm,.-~+´«'0987654321!\"#$%&/()=?»*`^_:;";
            string fullwidth = "ＱＷＥＲＴＹＵＩＯＰＡＳＤＦＧＨＪＫＬÇＺＸＣＶＢＮＭｑｗｅｒｔｙｕｉｏｐａｓｄｆｇｈｊｋｌçｚｘｃｖｂｎｍ,.－~ ´«＇０９８７６５４３２１！＂＃＄％＆／（）＝？»＊`＾＿：；";

            for (int i = 0; i < alpha.Length; i++)
            {
                message = message.Replace(alpha[i], fullwidth[i]);
            }
            await ReplyAsync(message);
        }

        [Command("timer")]
        [Remarks("Sets a reminder to ping you after a certain number of minutes has passed. A message can be specified along with the time to be printed back to you at the end of the timer.\nUsage: ~timer minutes message")]
        public async Task Timer(int time, [Remainder]string message = "")
        {
            if (time <= 0)
            {
                await ReplyAsync("Please use a time of 1 minute or longer.");
                return;
            }
            if (time == 1)
                await ReplyAsync("Setting timer for " + time + " minute from now.");
            else
                await ReplyAsync("Setting timer for " + time + " minutes from now.");
            Task.Run(() => SendReminder(time, message));
        }

        [Command("choose")]
        [Remarks("Randomly makes a choice for you. You can use as many choices as you want, but seperate all choices using \"\".\nUsage: ~choose \"option 1\" \"option 2\"")]
        public async Task Choose([Remainder]string message)
        {
            List<string> choices = new List<string>();
            int count = 0;
            if (message.IndexOf("\"") == -1)
            {
                await ReplyAsync("Seperate your choices with \"\" or spaces, example: ~choose \"option 1\" \"option 2\"");
                return;
            }
            while (true)
            {
                if (count != 0)
                    message = message.Remove(0, message.IndexOf("\"") + 1);
                if (message.IndexOf("\"") == -1)
                    break;
                message = message.Remove(0, message.IndexOf("\"") + 1);
                if (message.IndexOf("\"") == -1)
                {
                    await ReplyAsync("Make sure that every quotation has a beginning and end.");
                    return;
                }
                choices.Add(message.Remove(message.IndexOf("\"")));
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
        [Remarks("Takes whatever you give it and searches for it on YouTube, it will return the first search result that appears.\nUsage: ~youtube video title")]
        public async Task Youtube([Remainder]string vid)
        {
            vid = "https://www.youtube.com/results?search_query=" + vid.Replace(" ", "+");
            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
            string html = await client.GetStringAsync(vid);
            html = html.Remove(0, html.IndexOf("?v=") + 3);
            html = html.Remove(html.IndexOf("\""));
            await ReplyAsync("https://www.youtube.com/watch?v=" + html);
        }

        [Command("gnomed")]
        [Remarks("I'm gnot a gnelf...\nUsage: ~gnomed")]
        public async Task Gnomed()
        {
            await ReplyAsync("https://www.youtube.com/watch?v=6n3pFFPSlW4");
        }

        [Command("mastery")]
        [Remarks("Gives the number of mastery points for the top 10 most played champions for a user on any server.\nUsage ~mastery region username")]
        public async Task Mastery(string region, [Remainder]string name)
        {
            var embed = new EmbedBuilder();
            embed.WithColor(new Color(0x42ebf4));
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
        [Remarks("Gets varying pieces of Discord information for one or more users. Mention a user to get their information, do not mention anyone to get your own. You can mention as many people as you like.\nUsage: ~info @person1 @person2")]
        public async Task MyInfo([Remainder] string useless = "")
        {
            var mention = Context.Message.MentionedUserIds;
            if (mention.Count != 0)
            {
                foreach (ulong temp in mention)
                {
                    var embed = ConstructEmbedInfo(await Context.Guild.GetUserAsync(temp));
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
        [Remarks("Gets the avatar for one or more users. Mention a user to get their avatar, do not mention anyone to get your own. You can mention as many people as you like.\nUsage: ~avatar @person1 @person2")]
        public async Task Avatar([Remainder] string useless = "")
        {
            var mention = Context.Message.MentionedUserIds;
            if (mention.Count != 0)
            {
                foreach (ulong temp in mention)
                {
                    var embed = new EmbedBuilder();
                    IGuildUser user = await Context.Guild.GetUserAsync(temp);
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

        [Command("8ball")]
        [Remarks("asks the magic 8 ball a question.\nUsage: ~8ball")]
        public async Task eightBall([Remainder] string useless = "")
        {
            string[] responses = new string[] { "it is certain", "It is decidedly so", "Without a doubt", "Yes definitely", "You may rely on it", "As I see it, yes", "Most likely", "Outlook good", "Yes", "Signs point to yes", "Reply hazy try again", "Ask again later", "Better not tell you now", "Cannot predict now", "Concentrate and ask again", "Don't count on it", "My reply is no", "My sources say no", "Outlook not so good", "Very doubtful" };
            Random rnd = new Random();
            int num = rnd.Next(20);
            await ReplyAsync(responses[num]);
        }

        [Command("s8ball")]
        [Remarks("asks the sassy 8 ball a question.\nUsage: ~s8ball")]
        public async Task SeightBall([Remainder] string useless = "")
        {
            string[] responses = new string[] { "Fuck yeah.", "Sure, why not?", "Well, duh.", "Do bears shit in the woods?", "Is water wet?", "I mean, I guess.", "If it gets you to fuck off, then sure.", "011110010110010101110011", "Whatever floats your boat.", "Fine, sure, whatever.", "Fuck you.", "Why do you feel the need to ask a BOT for validation?", "Figure it out yourself.", "Does it really matter?", "Leave me alone.", "Fuck no.", "Why would you even consider that a possibility?", "It's cute you think that could happen.", "Not a chance shitlord.", "Not in a million years." };
            Random rnd = new Random();
            int num = rnd.Next(20);
            await ReplyAsync(responses[num]);
        }

        [Command("tiltycat")]
        [Remarks("Creates a cat at any angle you specify.\nUsage: ~tiltycat degree\n\nSpecial thanks to Erik (Assisting#8734) for writing the program. Accessed via ```http://www.writeonlymedia.com/tilty_cat/(degree).png``` where (degree) is the desired angle")]
        public async Task TiltyCat(int degree, [Remainder] string useless = "")
        {
            string temp = "http://www.writeonlymedia.com/tilty_cat/" + degree + ".png";
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(new Uri(temp),"tiltycat.png");
            }
            await Context.Channel.SendFileAsync("tiltycat.png");
        }

        [Command("rolligentle")]
        [Remarks("Makes the Gentlecat do a rollie\nUsage: ~rolligentle")]
        public async Task RolliCat([Remainder] string useless = "")
        {
            await ReplyAsync("<:gentlecat:302907277571260418> <:rightcat:455100361066283035> <:bottomcat:455100361120940032> <:leftcat:455100361187786752> <:gentlecat:302907277571260418>");
        }

        [Command("meancount")]
        [Remarks("Reports the number of times Jif has said \"I mean\"\nUsage: ~rolligentle")]
        public async Task meanCountt([Remainder] string useless = "")
        {
            string file = "references/mean.txt";
            Int32 num = Convert.ToInt32(File.ReadAllText(file));
            await ReplyAsync("I mean, I've said it " + num + " times since 12/13/18.");
        }

        [Command("imean")]
        [Remarks("Adds a tally to the number of times Jif has said \"I mean\"\nUsage: ~rolligentle")]
        public async Task iMean([Remainder] string useless = "")
        {
            string file = "references/mean.txt";
            Int32 num = Convert.ToInt32(File.ReadAllText(file));
            num++;
            await ReplyAsync("<@150084781864910848> you've said \"I mean\" " + num + " times.");
            File.WriteAllText(file, Convert.ToString(num));
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

        public async Task SendReminder(int time, string message)
        {
            System.Threading.Thread.Sleep(time * 60 * 1000);
            if (message != "")
                await ReplyAsync(Context.User.Mention + " your timer for \"" + message + "\" has ended");
            else
                await ReplyAsync(Context.User.Mention + " Times up!");
        }

        public string FormatTime(DateTimeOffset orig)
        {
            string str = "";
            str = str + orig.DayOfWeek + ",";
            str = str + orig.LocalDateTime;
            str = str.Insert(str.IndexOf(" "), " at");
            str = str.Insert(str.IndexOf(",") + 1, " ");
            return str;
        }

        public EmbedBuilder ConstructEmbedInfo(IGuildUser user)
        {
            var embed = new EmbedBuilder();
            embed.WithColor(new Color(0x42ebf4));
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
    }
}