using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using Discord.WebSocket;
using Discord;
using JifBot.Models;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace JifBot
{
    public class ModalHandler
    {
        private DiscordSocketClient client;

        public ModalHandler(IServiceProvider service)
        {
            client = service.GetService<DiscordSocketClient>();
        }


        public async Task HandleModalSubmitted(SocketModal modal)
        {
            var id = modal.Data.CustomId;
            byte[] imageBytes = null;
            var imageType = "";

            if (id.StartsWith("character_description"))
            {
                // I know this is scuffed as hell leave me alone
                var key = id.Split(":")[1];
                var db = new BotBaseContext();
                var character = db.Character.AsQueryable().Where(c => c.Key == key).FirstOrDefault();
                character.Description = modal.Data.Components.First(x => x.CustomId == "description").Value;
                db.SaveChanges();
                await modal.RespondAsync($"{key} successfully updated", ephemeral: true);
            }
            else if (id.Equals("qotd-submit"))
            {
                var question = modal.Data.Components.First(x => x.CustomId == "question").Value;
                var image = modal.Data.Components.First(x => x.CustomId == "image").Value;
                var db = new BotBaseContext();

                if (image != "")
                {
                    using (HttpClient client = new HttpClient())
                    {
                        HttpResponseMessage response = await client.GetAsync(image);
                        if (response.Content.Headers.ContentType?.MediaType.StartsWith("image/") == true)
                        {
                            imageBytes = await response.Content.ReadAsByteArrayAsync();
                            imageType = response.Content.Headers.ContentType?.MediaType.Replace("image/", "");
                        }
                        else
                        {
                            await modal.RespondAsync("Invalid file type! Please supply a link to an image, or leave the image field blank.", ephemeral: true);
                            return;
                        }
                    }
                }

                db.Add(new Qotd { 
                    Question = question,
                    ServerId = modal.GuildId.Value,
                    UserId = modal.User.Id,
                    Image = imageBytes,
                    ImageType = imageType
                });
                db.SaveChanges();
                
                await modal.RespondAsync("Question recorded. Thank you!", ephemeral: true);

                var server = client.GetGuild(modal.GuildId.Value);
                var config = db.GetServerConfig(server);

                if (config != null && config.QotdThreadId != 0)
                {
                    var thread = server.GetThreadChannel(config.QotdThreadId);
                    var post = await thread.GetMessageAsync(thread.Id) as IUserMessage;
                    var embed = new JifBotEmbedBuilder();
                    embed.PopulateAsQotd(server.Id);
                    await post.ModifyAsync(msg => msg.Embed = embed.Build());
                }
            }
        }
    }
}
