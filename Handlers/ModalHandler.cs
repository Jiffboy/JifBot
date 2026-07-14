using Discord;
using Discord.WebSocket;
using JifBot.Builders;
using JifBot.Models;
using JifBot.Utils;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

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
            if (modal.Data.CustomId.StartsWith("character_description"))
            {
                await HandleCharacterDescription(modal);
            }
            else if (modal.Data.CustomId.Equals("qotd-submit"))
            {
                await HandleQotdSubmit(modal);
            }
            else if (modal.Data.CustomId.StartsWith("event"))
            {
                var brokenId = modal.Data.CustomId.Split("-");
                if (brokenId[1] == "edit")
                {
                    await HandleEventEdit(modal, int.Parse(brokenId[2]));
                }
                else if (brokenId[1] == "eventedit")
                {
                    await HandleEventEditEvent(modal, int.Parse(brokenId[2]));
                }
                else if (brokenId[1] == "addrole")
                {
                    await HandleEventAddRole(modal, int.Parse(brokenId[2]));
                }
                else if (brokenId[1] == "signup")
                {
                    await HandleEventSignup(modal, int.Parse(brokenId[2]));
                }
            }
        }

        private async Task HandleCharacterDescription(SocketModal modal)
        {
            // I know this is scuffed as hell leave me alone
            var key = modal.Data.CustomId.Split(":")[1];
            var db = new BotBaseContext();
            var character = db.Character.AsQueryable().Where(c => c.Key == key).FirstOrDefault();
            character.Description = modal.Data.Components.First(x => x.CustomId == "description").Value;
            db.SaveChanges();
            await modal.RespondAsync($"{key} successfully updated", ephemeral: true);
        }

        private async Task HandleQotdSubmit(SocketModal modal)
        {
            byte[] imageBytes = null;
            var imageType = "";
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

            db.Add(new Qotd
            {
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
                var embed = new QotdEmbedBuilder();
                embed.Populate(server.Id);
                await post.ModifyAsync(msg => msg.Embed = embed.Build());
            }
        }

        private async Task HandleEventEdit(SocketModal modal, int id)
        {
            var title = modal.Data.Components.First(x => x.CustomId == "title").Value;
            var description = modal.Data.Components.First(x => x.CustomId == "description").Value;
            var limit = modal.Data.Components.First(x => x.CustomId == "limit").Value;
            var deadline = modal.Data.Components.First(x => x.CustomId == "deadline").Value;
            var image = new CommonImage(modal.Data.Attachments?.FirstOrDefault());
            var deadlineTs = 0L;
            var limitNum = 0;

            if (deadline != "")
            {
                deadlineTs = GlobalUtils.GetTimestamp(deadline);
                if (deadlineTs == 0)
                {
                    await modal.RespondAsync("Invalid date-time format. Please format as: mm/dd/yyyy hh:mm", ephemeral: true);
                    return;
                }
            }

            if (limit != "")
            {
                bool success = int.TryParse(limit, out limitNum);
                if (!success || limitNum < 0)
                {
                    await modal.RespondAsync("Limit must be a postive number", ephemeral: true);
                    return;
                }
            }

            if (!image.isValid)
            {
                await modal.RespondAsync("Invalid image type", ephemeral: true);
                return;
            }

            var db = new BotBaseContext();
            var eventBuilder = new EventUIBuilder();

            if (id > 0)
            {
                var ev = db.Event.Where(e => e.Id == id).First();
                ev.Title = title;
                ev.Description = description;
                ev.Deadline = deadlineTs;
                ev.Limit = limitNum;
                if (!image.isNull)
                {
                    ev.Image = image.imgBytes;
                    ev.ImageType = image.imgType;
                    ev.ImageUrl = image.thumbnailUrl;
                }

                db.SaveChanges();
                await modal.UpdateAsync(m => m.Components = eventBuilder.BuildComponent(ev));
            }
            else
            {
                var user = db.GetUser(modal.User);
                var ev = new Event
                {
                    Title = title,
                    Description = description,
                    Deadline = deadlineTs,
                    Limit = limitNum,
                    EventType = "event",
                    EntrantType = "user",
                    EventTime = deadlineTs,
                    EventDuration = 3,
                    Status = "Setup-1",
                    UserId = user.UserId,
                    Image = image.imgBytes,
                    ImageType = image.imgType,
                    ImageUrl = image.thumbnailUrl
                };

                db.Add(ev);
                db.SaveChanges();
                await modal.RespondAsync(components: eventBuilder.BuildComponent(ev), ephemeral: true);
            }
        }

        private async Task HandleEventEditEvent(SocketModal modal, int id)
        {
            var start = modal.Data.Components.First(x => x.CustomId == "start").Value;
            var duration = modal.Data.Components.First(x => x.CustomId == "duration").Value;
            var location = modal.Data.Components.FirstOrDefault(x => x.CustomId == "location").Value;

            var startTs = GlobalUtils.GetTimestamp(start);
            if (startTs == 0)
            {
                await modal.RespondAsync("Invalid date-time format. Please format as: mm/dd/yyyy hh:mm", ephemeral: true);
                return;
            }

            var durNum = 0;
            bool success = int.TryParse(duration, out durNum);
            if (!success || durNum < 0)
            {
                await modal.RespondAsync("Duration must be a postive integer", ephemeral: true);
                return;
            }

            var db = new BotBaseContext();
            var ev = db.Event.Where(e => e.Id == id).First();
            ev.EventDuration = durNum;
            ev.EventTime = startTs;
            if (location != null) 
            {
                ev.EventLocation = location;
            }

            db.SaveChanges();

            var eventBuilder = new EventUIBuilder();
            await modal.UpdateAsync(m => m.Components = eventBuilder.BuildComponent(ev));
        }

        private async Task HandleEventAddRole(SocketModal modal, int id)
        {
            var name = modal.Data.Components.First(x => x.CustomId == "name").Value;
            var limit = modal.Data.Components.First(x => x.CustomId == "limit").Value;

            var limNum = 0;
            if (limit != "")
            {
                bool success = int.TryParse(limit, out limNum);
                if (!success || limNum < 0)
                {
                    await modal.RespondAsync("Limit must be a postive integer", ephemeral: true);
                    return;
                }
            }

            var db = new BotBaseContext();
            var ev = db.Event.Where(e => e.Id == id).First();
            var roles = db.EventRole.Where(r => r.EventId == id && r.Name == name).ToList();
            if (roles.Any())
            {
                await modal.RespondAsync("Role already exists!", ephemeral: true);
                return;
            }

            var role = new EventRole{
                EventId = id,
                Name = name,
                Limit = limNum
            };
            db.EventRole.Add(role);
            db.SaveChanges();

            var eventBuilder = new EventUIBuilder();
            await modal.UpdateAsync(m => m.Components = eventBuilder.BuildComponent(ev));
        }

        private async Task HandleEventSignup(SocketModal modal, int id)
        {
            var character = modal.Data.Components.FirstOrDefault(x => x.CustomId == "character");
            var role = modal.Data.Components.FirstOrDefault(x => x.CustomId == "role");

            var partChar = "";
            var partRole = "";

            if (character != null)
            {
                partChar = character.Values.First();
            }

            if ( role != null)
            {
                partRole = role.Values.First();
            }

            var db = new BotBaseContext();
            var ev = db.Event.Where(e => e.Id == id).FirstOrDefault();

            db.Add(new EventParticipant
            {
                EventId = ev.Id,
                UserId = modal.User.Id,
                CharacterKey = partChar,
                RoleName = partRole,
            });
            db.SaveChanges();
            var participants = db.EventParticipant.Where(p => p.EventId == ev.Id).ToList();
            if (participants.Count >= ev.Limit && ev.Limit != 0)
            {
                var eventResolver = new EventResolver(client);
                await eventResolver.ResolveEvent(ev);
                await modal.DeferAsync();
            }
            else
            {
                var eventBuilder = new EventUIBuilder();
                var user = client.GetUser(ev.UserId);
                await modal.UpdateAsync(m => m.Embed = eventBuilder.BuildEmbed(ev, user));
            }
        }
    }
}
