using System.Threading.Tasks;
using System.Linq;
using System;
using JifBot.Models;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace JifBot
{
    public class CronService
    {
        // This is stupid but I don't care
        private DayOfWeek lastQotdDay = DateTime.Today.AddDays(2).DayOfWeek;
        private DiscordSocketClient client;

        public CronService(IServiceProvider service)
        {
            client = service.GetService<DiscordSocketClient>();
        }

        public async Task Run()
        {
            while(true)
            {
                // Wait 30s before running it back
                await Task.Delay(30000);
                var now = DateTime.Now;

                try
                {
                    // Qotd time!!
                    if (now.Hour == 9 && now.DayOfWeek != lastQotdDay)
                    {
                        lastQotdDay = now.DayOfWeek;
                        var db = new BotBaseContext();
                        var servers = db.ServerConfig.AsQueryable().Where(s => s.QotdThreadId != 0).ToList();

                        foreach (var server in servers)
                        {
                            var questions = db.Qotd.AsQueryable().Where(q => q.ServerId == server.ServerId && q.AskTimestamp == 0).ToList();
                            if (questions.Count() > 0)
                            {
                                var guild = client.GetGuild(server.ServerId);
                                Console.WriteLine($"{DateTime.Now} [{"Info",8}] QOTD: Posting in {guild.Name}");

                                Random random = new Random();
                                var index = random.Next(questions.Count());
                                var question = questions[index];

                                var channel = guild.GetChannel(server.QotdForumId) as SocketForumChannel;
                                var thread = guild.GetThreadChannel(server.QotdThreadId);
                                var post = await thread.GetMessageAsync(thread.Id) as IUserMessage;
                                var user = client.GetUser(question.UserId);

                                if (question.Image != null)
                                {
                                    var ms = new MemoryStream(question.Image);

                                    var image = new Discord.FileAttachment(ms, $"image.{question.ImageType}");
                                    await channel.CreatePostWithFileAsync($"{now.Month}/{now.Day}/{now.Year}", image, text: $"# {question.Question}\nSubmitted by: {user.Mention}");
                                }
                                else
                                {
                                    await channel.CreatePostAsync($"{now.Month}/{now.Day}/{now.Year}", text: $"# {question.Question}\nSubmitted by: {user.Mention}");
                                }

                                question.AskTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                                db.SaveChanges();
                                var embed = new JifBotEmbedBuilder();
                                embed.PopulateAsQotd(server.ServerId);
                                await post.ModifyAsync(msg => msg.Embed = embed.Build());
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    var cc = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{DateTime.Now} [{"Error",8}] {ex.Message}");
                    Console.ForegroundColor = cc;
                }
            }
        }
    }
}
