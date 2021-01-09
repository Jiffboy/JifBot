using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
using System;
using JifBot.Models;
using System.Linq;
using System.Text.RegularExpressions;

namespace JifBot
{
    class ReactionHandler
    {
        public async Task ParseReactions(SocketUserMessage msg)
        {
            await CheckKeyword(msg);
            await CheckSignature(msg);
        }

        private async Task CheckKeyword(SocketUserMessage msg)
        {
            var db = new BotBaseContext();
            var react = db.ReactionBan.AsQueryable().AsQueryable().Where(c => c.ChannelId == msg.Channel.Id).FirstOrDefault();
            if (react != null)
                return;
            string words = msg.Content.ToString();
            words = words.Replace("*", "");
            words = words.Replace("_", "");
            words = words.ToLower();

            if (hasString(words, @"delete? this"))
                await msg.Channel.SendFileAsync("Media/deletthis.jpg");

            if (words.Equals(":o") || words.Equals(":0"))
                await msg.Channel.SendMessageAsync(":O");

            if (hasString(words,"fiora"))
            {
                await msg.Channel.SendFileAsync("Media/fiora.jpg");
                await msg.Channel.SendMessageAsync("**Salty Reese activated**");
            }

            if (hasString(words, "nani"))
            {
                await msg.Channel.SendFileAsync("Media/nani.jpg");
                await msg.Channel.SendMessageAsync("**NANI?!?!**");
            }

            if (hasString(words, @"be *gone,* thot"))
                await msg.Channel.SendFileAsync("Media/thot.jpg");

            if (hasString(words, "kms"))
            {
                await msg.Channel.SendFileAsync("Media/kms.png");
            }

            if (words.Equals("stop"))
                await msg.Channel.SendFileAsync("Media/stop.png");

            if (hasString(words, "bamboozle"))
                await msg.Channel.SendFileAsync("Media/bamboozle.png");

            if (words.ToLower().Equals("hi") || words.ToLower().Equals("hello") || words.ToLower().Equals("hey") || words.ToLower().Equals("yo") || words.ToLower().Equals("henlo"))
            {
                Random rnd = new Random();
                int num = rnd.Next(db.Greeting.Count()) + 1;
                var greeting = db.Greeting.AsQueryable().AsQueryable().Where(greet => greet.Id == Convert.ToUInt64(num)).First();
                await msg.Channel.SendMessageAsync(greeting.Greeting1);
            }

            if (hasString(words, @"ah{5,}"))
                await msg.Channel.SendMessageAsync("https://www.youtube.com/watch?v=yBLdQ1a4-JI");

            if (hasString(words, "@here") || hasString(words, "@everyone"))
                await msg.Channel.SendMessageAsync("<:ping:377208255132467233>");

            if (hasString(words, "i mean") && msg.Author.Id == 150084781864910848)
            {
                var count = db.Variable.AsQueryable().AsQueryable().Where(v => v.Name == "meanCount").First();
                int num = Convert.ToInt32(count.Value) + 1;
                count.Value = Convert.ToString(num);
                db.SaveChanges();
                await msg.Channel.SendMessageAsync("<@150084781864910848> you've said \"I mean\" " + count.Value + " times.");

            }

            if (hasString(words, "fuck y?o?u jif ?bot"))
            {
                await msg.DeleteAsync();
                await msg.Channel.SendMessageAsync("Know your place, trash.");
            }
            foreach (SocketUser mention in msg.MentionedUsers)
            {
                if (mention.Id == 315569278101225483)
                {
                    if (words.ToLower().Contains("play despacito"))
                        await msg.Channel.SendMessageAsync("https://www.youtube.com/watch?v=kJQP7kiw5Fk");
                    else if (msg.Author.Id == 186584509226024960)
                        await msg.Channel.SendMessageAsync("you're pretty ❤");
                    else
                        await msg.Channel.SendMessageAsync("<:ping:377208255132467233>");
                }
            }

            var channel = (SocketGuildChannel)msg.Channel;
            SocketGuildUser jifBot = channel.Guild.GetUser(315569278101225483);
            foreach(SocketRole role in msg.MentionedRoles)
            {
                if(jifBot.Roles.Contains(role))
                {
                    await msg.Channel.SendMessageAsync("<:ping:377208255132467233>");
                    break;
                }
            }

            if (hasString(words, "honk"))
            {
                await msg.Channel.SendFileAsync("Media/honk.jpg");
                await msg.Channel.SendMessageAsync("**HONK**");

                var user = db.User.AsQueryable().AsQueryable().Where(user => user.UserId == msg.Author.Id).FirstOrDefault();
                var honk = db.Honk.AsQueryable().AsQueryable().Where(honk => honk.UserId == msg.Author.Id).FirstOrDefault();

                if (user == null)
                    db.Add(new User { UserId = msg.Author.Id, Name = msg.Author.Username, Number = long.Parse(msg.Author.Discriminator) });
                else
                {
                    user.Name = msg.Author.Username;
                    user.Number = long.Parse(msg.Author.Discriminator);
                }
                if (honk == null)
                    db.Add(new Honk { UserId = msg.Author.Id, Count = 1 });
                else
                    honk.Count += 1;
                db.SaveChanges();
            }
        }


        private async Task CheckSignature(SocketUserMessage msg)
        {
            var db = new BotBaseContext();
            var signature = db.Signature.AsQueryable().AsQueryable().Where(sig => sig.UserId == msg.Author.Id).FirstOrDefault();
            if (signature != null)
            {
                Emoji react = new Emoji(signature.Signature1);
                await msg.AddReactionAsync(react);
            }
        }

        private bool hasString(string message, string phrase)
        {
            return Regex.IsMatch(message, @"(?:^| +)" + phrase + @" *(?:$| +)");
        }
    }
}
