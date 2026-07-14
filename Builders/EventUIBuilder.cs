using Discord;
using Discord.WebSocket;
using JifBot.Models;
using JifBot.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace JifBot.Builders
{
    public class EventUIBuilder
    {
        public int stepCount { get; } = 3;
        private const string timePlaceholder = "mm/dd/yyyy hh:mm in military eastern time";
        private const string modalTitle = "Title";
        private const string modalDesc = "Description";
        private const string modalLimit = "Attendant Limit (Blank if none)";
        private const string modalDeadline = "Signup Deadline (Blank if none)";
        public MessageComponent BuildComponent(Event ev)
        {
            var step = int.Parse(ev.Status.Split('-')[1]);
            var component = new ComponentBuilderV2();
            var container = new ContainerBuilder()
                .WithAccentColor(GlobalUtils.GetColor())
                .WithTextDisplay($"# Event Setup ({step}/3)")
                .WithSeparator();
                
            switch(step)
            {
                case 1:
                    AddOverviewComponent(ev, container);
                    break;
                case 2:
                    AddTypeComponent(ev, container);
                    break;
                case 3:
                    AddRoleComponent(ev, container);
                    break;
                default:
                    break;
            }

            var navBar = new ActionRowBuilder();
            foreach (var button in GetNavbarButtons(ev))
            {
                navBar.WithButton(button);
            }

            component
                .WithContainer(container)
                .WithActionRow(navBar);
                    
            return component.Build();
        }

        public Modal BuildModal()
        {
            var mb = new ModalBuilder()
                .WithTitle("Make Your Event!")
                .WithCustomId($"event-edit-0")
                .AddTextInput(modalTitle, "title", TextInputStyle.Short, required: true)
                .AddTextInput(modalDesc, "description", TextInputStyle.Paragraph, required: true)
                .AddTextInput(modalLimit, "limit", TextInputStyle.Short, required: false)
                .AddTextInput(modalDeadline, "deadline", TextInputStyle.Short, required: false, placeholder: timePlaceholder)
                .AddFileUpload("Image", "image", isRequired: false);
            return mb.Build();
        }

        public Modal BuildModal(Event ev)
        {
            var deadline = GetDateTimeString(ev.Deadline);

            var mb = new ModalBuilder()
                .WithTitle("Make Your Event!")
                .WithCustomId($"event-edit-{ev.Id}")
                .AddTextInput(modalTitle, "title", TextInputStyle.Short, required: true, value: ev.Title)
                .AddTextInput(modalDesc, "description", TextInputStyle.Paragraph, required: true, value: ev.Description)
                .AddTextInput(modalLimit, "limit", TextInputStyle.Short, required: false, value: $"{ev.Limit}")
                .AddTextInput(modalDeadline, "deadline", TextInputStyle.Short, required: false, value: deadline, placeholder: timePlaceholder)
                .AddFileUpload("Image", "image", isRequired: false);
            return mb.Build();
        }

        public Modal BuildEventModal(Event ev)
        {
            var start = GetDateTimeString(ev.EventTime);
            var mb = new ModalBuilder()
                .WithTitle("Event Details")
                .WithCustomId($"event-eventedit-{ev.Id}")
                .AddTextInput("Event Start Time", "start", TextInputStyle.Short, required: true, value: start, placeholder: timePlaceholder)
                .AddTextInput("Event Duration (Hours)", "duration", TextInputStyle.Short, required: true, value: $"{ev.EventDuration}")
                .AddTextInput("Event Location", "location", TextInputStyle.Short, required: false, value: $"{ev.EventLocation}", placeholder: "The moon");
            return mb.Build();
        }

        public Modal BuildRoleModal(Event ev)
        {
            var mb = new ModalBuilder()
                .WithTitle("Add New Role")
                .WithCustomId($"event-addrole-{ev.Id}")
                .AddTextInput("Role Name", "name", TextInputStyle.Short, required: true)
                .AddTextInput("Max Participants", "limit", TextInputStyle.Short, required: false);
            return mb.Build();
        }

        public Modal BuildSignupModal(Event ev, List<EventRole> roles, List<Character> characters)
        {
            var mb = new ModalBuilder()
                .WithTitle("Sign Up!")
                .WithCustomId($"event-signup-{ev.Id}");

            if(characters.Count > 0)
            {
                mb.AddSelectMenu("Character", "character", characters.ConvertAll(c => new SelectMenuOptionBuilder(c.Name, c.Key)));
                mb.AddTextDisplay("Can't find your character? Please create them in the [Blorbopedia](https://jifbot.com/blorbopedia/edit) and try again!");
            }

            if (roles.Count > 0)
            {
                mb.AddSelectMenu("Role", "role", roles.ConvertAll(r => new SelectMenuOptionBuilder(r.Name, r.Name)));
            }

            return mb.Build();
        }

        public Embed BuildEmbed(Event ev, SocketUser user)
        {
            var embed = new JifBotEmbedBuilder();
            var db = new BotBaseContext();
            var img = new CommonImage(ev.Image, ev.ImageType);

            string eventTypeStr;
            string entrantTypeStr;

            switch (ev.EventType)
            {
                case "thread":
                    eventTypeStr = "Longform";
                    break;
                case "event":
                    eventTypeStr = ev.EventLocation;
                    break;
                case "none":
                default:
                    eventTypeStr = "";
                    break;
            }

            switch (ev.EntrantType)
            {
                case "multi":
                case "single":
                    entrantTypeStr = "IC";
                    break;
                case "user":
                default:
                    entrantTypeStr = "OOC";
                    break;
            }

            embed.Title = $"{ev.Title} [{entrantTypeStr}]";
            embed.Description = ev.Description;
            embed.Author = new EmbedAuthorBuilder().WithName(user.GlobalName).WithIconUrl(user.GetDisplayAvatarUrl());

            var participants = db.EventParticipant.Where(e => e.EventId == ev.Id).ToList();
            var participantStr = GetParticipantString(ev, participants);


            var roleStr = "";
            var roles = db.EventRole.Where(r => r.EventId == ev.Id).ToList();
            if (roles.Count > 0)
            {
                foreach (var role in roles)
                {
                    var taken = participants.Where(p => p.RoleName == role.Name).ToList();
                    roleStr += $"{role.Name} ({taken.Count}";
                    if (role.Limit > 0)
                    {
                        roleStr += $"/{role.Limit}";
                    }
                    roleStr += ")\n";
                }
            }

            var eventStr = "";
            if (ev.EventType == "event")
            {
                eventStr += $"\n<t:{ev.EventTime}:f>\n";
                eventStr += $"Duration: {ev.EventDuration} hours\n";
            }
            eventStr += eventTypeStr;

            var signupStr = "";
            if (ev.Deadline > 0)
            {
                signupStr += $"<t:{ev.Deadline}:f>\n";
            }
            if (ev.EntrantType == "multi")
            {
                signupStr += "Multiple signups allowed";
            }

            if (eventStr != "")
            {
                embed.AddField("Event", eventStr, inline: true);
            }

            if (signupStr != "")
            {
                embed.AddField("Signups", signupStr, inline: true);
            }

            if (roleStr != "")
            {
                embed.AddField("Roles", roleStr);
            }

            var participantCnt = ev.Limit > 0 ? $"({participants.Count}/{ev.Limit})" : $"({participants.Count})";
            embed.AddField($"Participants {participantCnt}", participantStr);

            if (!img.isNull)
            {
                embed.ThumbnailUrl = img.thumbnailUrl;
            }


            return embed.Build();
        }

        public MessageComponent BuildEmbedComponent(Event ev)
        {
            var builder = new ComponentBuilder()
                .WithButton("Sign Up", $"event-signup-{ev.Id}", style: ButtonStyle.Success)
                .WithButton("Leave", $"event-leave-{ev.Id}", style: ButtonStyle.Danger);
            return builder.Build();
        }

        private void AddOverviewComponent(Event ev, ContainerBuilder container)
        {
            var deadline = ev.Deadline > 0 ? $"<t:{ev.Deadline}:f>" : "[None]";
            var limit = ev.Limit > 0 ? $"{ev.Limit}" : "[None]";
            var title = $"## {ev.Title}";

            if (ev.ImageUrl != "")
            {
                container.WithSection(new SectionBuilder()
                    .WithTextDisplay(title)
                    .WithTextDisplay($"{ev.Description}")
                    .WithAccessory(new ThumbnailBuilder().WithMedia(ev.ImageUrl)));
            }
            else
            {
                container.WithTextDisplay(title).WithTextDisplay($"{ev.Description}");
            }

            container
                .WithTextDisplay($"### Signup Deadline:\n{deadline}\n### Signup Limit:\n{limit}")
                .WithSeparator()
                .WithActionRow(new ActionRowBuilder()
                    .WithButton("Edit", $"event-edit-{ev.Id}", style: ButtonStyle.Primary));
        }

        private void AddTypeComponent(Event ev, ContainerBuilder container)
        {
            var start = ev.EventTime > 0 ? $"<t:{ev.EventTime}:f>" : "[None]";
            container
                .WithAccentColor(GlobalUtils.GetColor())
                .WithTextDisplay("### Entrant:")
                .WithActionRow(new ActionRowBuilder().WithSelectMenu(new SelectMenuBuilder()
                    .WithCustomId($"event-entrant-{ev.Id}")
                    .AddOption("Discord User", "user", emote: new Emoji("👤"), isDefault: ev.EntrantType == "user")
                    .AddOption("Single Character", "single", emote: new Emoji("🧍‍♂️"), isDefault: ev.EntrantType == "single")
                    .AddOption("Multiple Characters", "multi", emote: new Emoji("👬"), isDefault: ev.EntrantType == "multi")))
                .WithTextDisplay("### Event Type")
                .WithActionRow(new ActionRowBuilder().WithSelectMenu(new SelectMenuBuilder()
                    .WithCustomId($"event-type-{ev.Id}")
                    .AddOption("Discord Event", "event", emote: new Emoji("🗓️"), isDefault: ev.EventType == "event")
                    .AddOption("Longform Thread", "thread", emote: new Emoji("🧵"), isDefault: ev.EventType == "thread")
                    .AddOption("Ping Only", "none", emote: new Emoji("🔔"), isDefault: ev.EventType == "none")));

            if (ev.EventType == "event")
            {
                var location = ev.EventLocation != "" ? ev.EventLocation : "[Unspecified]";
                container
                    .WithTextDisplay($"### Event Start:\n{start}\n### Event Duration:\n{ev.EventDuration} hours\n### Event Location:\n{location}")
                    .WithSeparator()
                    .WithActionRow(new ActionRowBuilder()
                        .WithButton("Edit", $"event-eventedit-{ev.Id}", style: ButtonStyle.Primary));
            }
            else if (ev.EventType == "thread")
            {
                container
                    .WithTextDisplay("### Forum Channel")
                    .WithActionRow(new ActionRowBuilder()
                        .WithSelectMenu(new SelectMenuBuilder()
                            .WithCustomId($"event-channel-{ev.Id}")
                            .WithChannelTypes(ChannelType.Forum)
                            .WithType(ComponentType.ChannelSelect)));
            }
        }

        private void AddRoleComponent(Event ev, ContainerBuilder container)
        {
            var db = new BotBaseContext();
            var roles = db.EventRole.Where(r => r.EventId == ev.Id).ToList();
            var roleTxt = "";
            foreach (var role in roles)
            {
                var limit = role.Limit > 0 ? $"({role.Limit})" : "";
                roleTxt += $"**{role.Name}** {limit}\n";
            }
            roleTxt = roleTxt == "" ? "[None]" : roleTxt;

            container
                .WithAccentColor(GlobalUtils.GetColor())
                .WithTextDisplay("## Roles")
                .WithTextDisplay(roleTxt)
                .WithSeparator()
                .WithActionRow(new ActionRowBuilder()
                    .WithButton("Add", $"event-addrole-{ev.Id}", style: ButtonStyle.Primary)
                    .WithButton("Remove", $"event-removerole-{ev.Id}", style: ButtonStyle.Primary));
        }

        public List<ButtonBuilder> GetNavbarButtons(Event ev)
        {
            var list = new List<ButtonBuilder>();
            list.Add(new ButtonBuilder("Post", $"event-post-{ev.Id}", style: ButtonStyle.Success).WithDisabled(ev.Status != $"Setup-{stepCount}"));
            list.Add(new ButtonBuilder("<<", $"event-prev-{ev.Id}", style: ButtonStyle.Primary).WithDisabled(ev.Status == "Setup-1"));
            list.Add(new ButtonBuilder(">>", $"event-next-{ev.Id}", style: ButtonStyle.Primary).WithDisabled(ev.Status == $"Setup-{stepCount}"));
            list.Add(new ButtonBuilder("Cancel", $"event-cancel-{ev.Id}", style: ButtonStyle.Danger));
            return list;
        }

        public string GetParticipantString(Event ev, List<EventParticipant> participants)
        {
            var db = new BotBaseContext();
            var participantStr = "[None]";
            if (participants.Count > 0)
            {
                participantStr = "";
                foreach (var participant in participants)
                {
                    if (participant.CharacterKey != "")
                    {
                        var partChar = db.Character.Where(c => c.Key == participant.CharacterKey).FirstOrDefault();
                        participantStr += $"[{partChar.Name}]({partChar.ToUrl()})";
                    }
                    else
                    {
                        participantStr += $"<@!{participant.UserId}>";
                    }
                    if (participant.RoleName != "")
                    {
                        participantStr += $" ({participant.RoleName})";
                    }
                    participantStr += "\n";
                }
            }
            return participantStr;
        }

        private string GetDateTimeString(long timestamp)
        {
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return timestamp > 0 ? dateTime.AddSeconds(timestamp).ToLocalTime().ToString("MM/dd/yyyy HH:mm") : "";
        }
    }
}
