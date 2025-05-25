using System.Threading.Tasks;
using System.Linq;
using Discord;
using Discord.Interactions;
using JifBot.Models;
using System;
using System.IO;
using System.Net;
using System.Collections.Generic;

namespace JifBot.Commands
{
    namespace JifBot.Commands
    {
        public class Blorbos : InteractionModuleBase<SocketInteractionContext>
        {
            [SlashCommand("blorbopedia", "Looks up a saved characters by name, or by author")]
            public async Task Blorbopedia(
            [Summary("character-key", "The key for the exact character you are looking for")] string key = null,
            [Summary("search", "Value to search tags, aliases, and character names for")] string search = null,
            [Summary("author", "The author you would like to retreive the characters for")] IUser author = null)
            {
                if (key != null)
                {
                    key = key.ToLower();
                }
                var db = new BotBaseContext();
                if (author != null)
                {
                    var characters = db.Character.AsQueryable().Where(c => c.UserId == author.Id).ToList();
                    if (characters.Count() == 1)
                    {
                        key = characters[0].Key;
                    }
                    else if (characters.Count() > 1)
                    {
                        var msg = $"{author.Username}'s characters:";
                        foreach (var character in characters)
                        {
                            msg += $"\n{character.Key}";
                            if (character.Name != "")
                                msg += $" - [{character.Name}]";
                        }
                        await RespondAsync(msg);
                        return;
                    }
                    else if (key == null)
                    {
                        await RespondAsync("User has no characters!", ephemeral: true);
                        return;
                    }
                }
                if (key != null)
                {
                    var character = db.Character.AsQueryable().Where(c => c.Key == key).FirstOrDefault();
                    if (character == null)
                    {
                        await RespondAsync("Invalid character key provided, please try again", ephemeral: true);
                        return;
                    }

                    await SendCharacterBio(character);
                    return;
                }
                if (search != null)
                {
                    search = search.ToLower();
                    var charactersByName = db.Character.AsQueryable().Where(c => c.Name.ToLower().Contains(search)).ToList();
                    var tagMatches = db.CharacterTag.AsQueryable().Where(c => c.Tag.ToLower() == search).Select(c => c.Key).ToList();
                    var charactersByTag = db.Character.AsQueryable().Where(c => tagMatches.Contains(c.Key)).ToList();
                    var aliasMatches = db.CharacterAlias.AsQueryable().Where(c => c.Alias.ToLower() == search).Select(c => c.Key).ToList();
                    var charactersByAlias = db.Character.AsQueryable().Where(c => aliasMatches.Contains(c.Key)).ToList();
                    var allCharacters = charactersByName.Concat(charactersByTag).Concat(charactersByAlias).Distinct().ToList();
                    if (allCharacters.Count > 0)
                    {
                        if(allCharacters.Count == 1)
                        {
                            await SendCharacterBio(allCharacters[0]);
                            return;
                        }

                        var msg = "Characters matching this criteria:";
                        foreach( var character in allCharacters)
                        {
                            msg += $"\n{character.Key}";
                            if (character.Name != "")
                                msg += $" - [{character.Name}]";
                        }
                        await RespondAsync(msg);
                    }
                    else
                        await RespondAsync("No characters match this criteria", ephemeral: true);
                    return;
                }

                await RespondAsync("No valid characters or users found. Please try again.", ephemeral: true);
            }

            [SlashCommand("managecharacter", "Manages characters saved to the blorbopedia")]
            public async Task ManageCharacter(
            [Choice("Add", "add")]
            [Choice("Modify", "modify")]
            [Choice("Delete", "delete")]
            [Summary("action", "The action to perform for the specified character")] string action,
            [Summary("character-key", "The key to look up your character. (Please use first name /nickname)")] string key,
            [Summary("name", "The character's full name to be displayed")] string name = "",
            [Summary("image", "An image to be used to display the character.")] IAttachment image = null,
            [Summary("title", "The characters title. i.e. 'The Savior of Eorzea'")] string title = "",
            [Summary("occupation", "The character's occupation")] string occupation = "",
            [Summary("age", "The characeter's age")] string age = "",
            [Summary("race", "The character's race")] string race = "",
            [Summary("pronouns", "The characeter's pronouns")] string pronouns = "",
            [Summary("sexuality", "The character's sexuality")] string sexuality = "",
            [Summary("origin", "Where the character is from")] string origin = "",
            [Summary("residence", "Where the character currently resides")] string residence = "",
            [Summary("additional-resources", "Links for resources on the character. (carrd, lore doc, etc)")] string resources = "",
            [Choice("Compact", "compact")]
            [Choice("Expanded", "expanded")]
            [Summary("compact-view", "Specifies whether the image displays big or small in /blorbopedia")] string compact = "unset")
            {
                key = key.ToLower();
                var db = new BotBaseContext();

                if (image != null && !(image.ContentType.StartsWith("image/")))
                {
                    await RespondAsync("Please supply a valid image filetype", ephemeral: true);
                    return;
                }

                if (action == "add")
                {
                    var user = db.User.AsQueryable().AsQueryable().Where(user => user.UserId == Context.User.Id).FirstOrDefault();
                    if (user == null)
                        db.Add(new User { UserId = Context.User.Id, Name = Context.User.Username, Number = long.Parse(Context.User.Discriminator) });
                    var character = db.Character.AsQueryable().AsQueryable().Where(c => c.Key == key).FirstOrDefault();
                    if (character != null)
                    {
                        await RespondAsync("Character already exists with this character-key. Please choose another", ephemeral: true);
                        return;
                    }

                    db.Add(new Character
                    {
                        Key = key,
                        UserId = Context.User.Id,
                        Name = name,
                        Description = "",
                        Title = title,
                        Occupation = occupation,
                        Age = age,
                        Race = race,
                        Pronouns = pronouns,
                        Sexuality = sexuality,
                        Origin = origin,
                        Residence = residence,
                        Resources = resources,
                        CompactImage = compact == "compact",
                        Image = image != null ? GetBytesFromAttachment(image) : null,
                        ImageType = image != null ? image.ContentType.Replace("image/", "") : ""
                    });
                    db.SaveChanges();
                    await RespondAsync($"{key} added! They can now be found in /blorbopedia!\n\nTo provide more information about the character, please use /characterdescription", ephemeral: true);
                }
                else if (action == "modify")
                {
                    var character = db.Character.AsQueryable().Where(c => c.Key == key).FirstOrDefault();
                    if (character == null)
                    {
                        await RespondAsync("Character key does not exist. Please try again", ephemeral: true);
                        return;
                    }
                    if (character.UserId != Context.User.Id)
                    {
                        await RespondAsync("That character does not belong to you! Hands off!", ephemeral: true);
                        return;
                    }
                    if (name != "")
                        character.Name = name;
                    if (title != "")
                        character.Title = title;
                    if (occupation != "")
                        character.Occupation = occupation;
                    if (age != "")
                        character.Age = age;
                    if (race != "")
                        character.Race = race;
                    if (pronouns != "")
                        character.Pronouns = pronouns;
                    if (sexuality != "")
                        character.Sexuality = sexuality;
                    if (origin != "")
                        character.Origin = origin;
                    if (residence != "")
                        character.Residence = residence;
                    if (resources != "")
                        character.Resources = resources;
                    if (compact != "unset")
                        character.CompactImage = compact == "compact";
                    if (image != null)
                    {
                        character.Image = GetBytesFromAttachment(image);
                        character.ImageType = image.ContentType.Replace("image/", "");
                    }
                    db.SaveChanges();
                    await RespondAsync($"{key} successfully updated", ephemeral: true);
                }
                else if (action == "delete")
                {
                    var character = db.Character.AsQueryable().Where(c => c.Key == key).FirstOrDefault();
                    if (character == null)
                        await RespondAsync("That character does not exist!", ephemeral: true);
                    else
                    {
                        db.Character.Remove(character);
                        db.SaveChanges();
                        await RespondAsync("Character removed successfully", ephemeral: true);
                    }
                }
            }

            [SlashCommand("characterdescription", "Opens a window to set the description for a specified character")]
            public async Task CharacterDescription(
                [Summary("character-key", "The character key of the character you wish to update")] string key)
            {
                var db = new BotBaseContext();
                var character = db.Character.AsQueryable().Where(c => c.Key == key).FirstOrDefault();
                if (character == null)
                {
                    await RespondAsync("Invalid character key!", ephemeral: true);
                    return;
                }
                var mb = new ModalBuilder()
                        .WithTitle("Tell us about them!")
                        .WithCustomId($"character_description:{key}")
                        .AddTextInput("Description / Backstory", "description", TextInputStyle.Paragraph, placeholder: "If you wish to modify the description, copy/paste it. Otherwise, hit cancel to keep as-is. Sorry :(");
                await Context.Interaction.RespondWithModalAsync(mb.Build());
            }

            [SlashCommand("tagcharacter", "Applies tags or aliases to a character to be used in searches.")]
            public async Task TagCharacter(
                [Summary("character-key", "The character key of the character you wish to update")] string key,
                [Choice("Add", "add")]
                [Choice("Delete", "delete")]
                [Choice("View", "view")]
                [Summary("action", "What you wish to do with the tags/aliases")] string action,
                [Summary("aliases", "A list of of other known names for a character. Separate with commas")] string aliases = "",
                [Summary("tags", "A list of tags to describe the character. (universe, nationality, class etc.) Separate with commas")] string tags = "")
            {
                var db = new BotBaseContext();
                var character = db.Character.AsQueryable().Where(c => c.Key == key).FirstOrDefault();
                if (character == null)
                {
                    await RespondAsync("Character does not exist, or character key is incorrect", ephemeral: true);
                    return;
                }

                if( action == "add")
                {
                    if (tags == "" && aliases == "")
                    {
                        await RespondAsync("Please provide aliases and/or tags", ephemeral: true);
                        return;
                    }

                    if (aliases != "")
                    {
                        foreach (var alias in aliases.Split(',').Select(c => c.Trim()).Distinct())
                        {
                            if (db.CharacterAlias.AsQueryable().Where(c => c.Alias == alias & c.Key == key).FirstOrDefault() == null)
                            {
                                db.Add(new CharacterAlias { Key = key, Alias = alias });
                            }
                        }
                    }
                    if (tags != "")
                    {
                        foreach (var tag in tags.Split(',').Select(c => c.Trim()).Distinct())
                        {
                            if (db.CharacterTag.AsQueryable().Where(c => c.Tag == tag & c.Key == key).FirstOrDefault() == null)
                            {
                                db.Add(new CharacterTag { Key = key, Tag = tag });
                            }
                        }
                        
                    }
                }
                else if (action == "delete")
                {
                    if (tags == "" && aliases == "")
                    {
                        await RespondAsync("Please provide aliases and/or tags", ephemeral: true);
                        return;
                    }

                    if (aliases != "")
                    {
                        foreach (var alias in aliases.Split(',').Select(c => c.Trim()).Distinct())
                        {
                            var currTag = db.CharacterAlias.AsQueryable().Where(c => c.Alias == alias & c.Key == key).FirstOrDefault();
                            if (currTag != null)
                            {
                                db.CharacterAlias.Remove(currTag);
                            }
                        }
                    }
                    if (tags != "")
                    {
                        foreach (var tag in tags.Split(',').Select(c => c.Trim()).Distinct())
                        {
                            var currTag = db.CharacterTag.AsQueryable().Where(c => c.Tag == tag & c.Key == key).FirstOrDefault();
                            if(currTag != null)
                            {
                                db.CharacterTag.Remove(currTag);
                            }
                        }

                    }
                }
                else if (action == "view")
                {
                    var tagList = db.CharacterTag.AsQueryable().Where(c => c.Key == character.Key).ToList();
                    var aliasList = db.CharacterAlias.AsQueryable().Where(c => c.Key == character.Key).ToList();

                    if (tagList.Count == 0 && aliasList.Count == 0)
                    {
                        await RespondAsync("No tags or aliases for this character!", ephemeral: true);
                        return;
                    }
                    var msg = "";

                    if (tagList.Count > 0)
                    {
                        msg += "Tags:\n";
                        foreach(var tag in tagList)
                        {
                            msg += $"`{tag.Tag}` ";
                        }
                    }

                    if (aliasList.Count > 0)
                    {
                        msg += "\n\nAliases:\n";
                        foreach(var alias in aliasList)
                        {
                            msg += $"`{alias.Alias}` ";
                        }
                    }

                    await RespondAsync(msg, ephemeral: true);
                    return;
                }

                db.SaveChanges();
                await RespondAsync("Tags and Aliases updated successfully", ephemeral: true);
            }

            [SlashCommand("rptrial", "Begins a vote to award/deduct RP Points")]
            public async Task RPTrial(
            [Summary("user", "The user on trial")] IGuildUser user,
            [Summary("points", "The number of points to add / deduct. Negative number to deduct.")] int points,
            [Summary("reason", "The reason for assigning these points.")] string reason,
            [Summary("image", "An image to be used as evidence.")] IAttachment image = null)
            {
                if (points == 0)
                {
                    await RespondAsync("Value must be non-zero.", ephemeral: true);
                    return;
                }

                if (image != null && !(image.ContentType.StartsWith("image/")))
                {
                    await RespondAsync("Please supply a valid image filetype", ephemeral: true);
                    return;
                }

                var db = new BotBaseContext();
                
                var record = db.CourtRecord.Add(new CourtRecord
                {
                    Status = "Pending",
                    Points = points,
                    Justification = reason,
                    DefendantId = user.Id,
                    ProsecutorId = Context.User.Id,
                    YayVotes = new List<ulong>(),
                    NayVotes = new List<ulong>(),
                    ServerId = Context.Guild.Id,
                    ChannelId = Context.Channel.Id,
                    Timestamp = ((DateTimeOffset)DateTime.Now).ToUnixTimeMilliseconds(),
                    ImageUrl = image != null ? image.Url : null
                });
                db.SaveChanges();

                var dbUser = db.User.AsQueryable().Where(u => u.UserId == user.Id).FirstOrDefault();
                if (dbUser == null)
                    db.Add(new User { UserId = user.Id, Name = user.Username, Number = long.Parse(user.Discriminator) });

                JifBotEmbedBuilder embed = new JifBotEmbedBuilder();
                embed.PopulateAsTrial(record.Entity, user);

                var builder = new ComponentBuilder()
                    .WithButton("Yay", $"yay-{record.Entity.Id}", style: ButtonStyle.Success)
                    .WithButton("Nay", $"nay-{record.Entity.Id}", style: ButtonStyle.Danger);

                await RespondAsync(embed: embed.Build(), components: builder.Build());
                var response = await Context.Interaction.GetOriginalResponseAsync();
                record.Entity.MessageId = response.Id;
                db.SaveChanges();
            }

            [SlashCommand("rppoints", "Gets the RP points leaderboard, or for a specific person.")]
            public async Task RPPoints(
            [Summary("count", "The number of users to display. Defaults to 10, max of 25.")] int count = 10,
            [Summary("user", "If specified, gets points for a specific user.")] IGuildUser user = null)
            {
                var db = new BotBaseContext();
                if (user != null)
                {
                    var target = db.User.AsQueryable().Where(u => u.UserId == user.Id).FirstOrDefault();
                    if (user == null)
                        await RespondAsync("User does not exist, somehow!", ephemeral: true);
                    else
                        await RespondAsync($"{target.Name} has {target.RpPoints} RP points.");
                }
                else
                {
                    var users = db.User.AsQueryable().Where(u => u.RpPoints != 0).OrderByDescending(u => u.RpPoints).Take(count).ToList();
                    int curr = 0;
                    int maxNameLen = users.Max(u => u.Name.Length);
                    int maxPointLen = users.Max(u => u.RpPoints.ToString().Length);
                    string msg = "```";
                    foreach (User target in users)
                    {
                        msg += $"{curr + 1}. {target.Name.PadRight(maxNameLen)}  {target.RpPoints.ToString().PadLeft(maxPointLen)}\n";
                        curr++;
                    }
                    msg += "```";

                    JifBotEmbedBuilder builder = new JifBotEmbedBuilder();
                    builder.Title = $"Top {count} RPers";
                    builder.Description = msg;
                    await RespondAsync(embed: builder.Build());
                }
            }

            [SlashCommand("rprecords", "Gets the RP trial records for a given user.")]
            public async Task RPRecords(
            [Summary("user", "The user to get the records of")] IGuildUser user)
            {
                var emojiMap = new Dictionary<string, string>()
                {
                    {"Approved",  "✅"},
                    {"Denied", "❌" },
                    {"Pending", "🤔" }
                };
                var db = new BotBaseContext();
                var defendantRecords = db.CourtRecord.AsQueryable().Where(r => r.DefendantId == user.Id).OrderByDescending(r => r.Timestamp);
                var prosecutorRecords = db.CourtRecord.AsQueryable().Where(r => r.ProsecutorId == user.Id).OrderByDescending(r => r.Timestamp);
                var points = db.User.AsQueryable().Where(u => u.UserId == user.Id).First().RpPoints;

                JifBotEmbedBuilder builder = new JifBotEmbedBuilder();
                builder.Title = $"RP Case Record for {user.DisplayName}";
                builder.ThumbnailUrl = user.GetAvatarUrl();
                builder.AddField("Total Points", points, inline: true);
                builder.AddField("Defendant Trials", defendantRecords.Count(), inline: true);
                builder.AddField("Prosecutor Trials", prosecutorRecords.Count(), inline: true);

                var defendantMsg = "";
                var prosecutorMsg = "";

                foreach (var record in defendantRecords.Take(5).ToList())
                {
                    defendantMsg += $"{emojiMap[record.Status]} [Trial](https://discord.com/channels/{record.ServerId}/{record.ChannelId}/{record.MessageId}) [{record.Points}] {record.Justification}\n";
                }

                foreach (var record in prosecutorRecords.Take(5).ToList())
                {
                    prosecutorMsg += $"{emojiMap[record.Status]} [Trial](https://discord.com/channels/{record.ServerId}/{record.ChannelId}/{record.MessageId}) [{record.Points}] {record.Justification}\n";
                }

                if (defendantMsg.Length > 0)
                    builder.AddField("Defendant [Last 5]", defendantMsg);
                if (prosecutorMsg.Length > 0)
                    builder.AddField("Prosecutor [Last 5]", prosecutorMsg);

                await RespondAsync(embed: builder.Build());
            }

            private byte[] GetBytesFromAttachment(IAttachment attachment)
            {
                var client = new WebClient();
                return client.DownloadData(attachment.Url);
            }

            private async Task SendCharacterBio(Character character)
            {
                var db = new BotBaseContext();
                var user = db.User.AsQueryable().Where(u => u.UserId == character.UserId).FirstOrDefault();
                var tags = db.CharacterTag.AsQueryable().Where(c => c.Key == character.Key).ToList();

                JifBotEmbedBuilder embed = new JifBotEmbedBuilder();
                embed.Title = character.Name != "" ? character.Name : character.Key;
                embed.Description = "";
                if (character.Title != "")
                    embed.Description += $"*{character.Title}*\n\n";
                if (character.Description != "")
                    embed.Description += character.Description;
                if (character.Occupation != "")
                    embed.AddField("Occupation", character.Occupation, inline: true);
                if (character.Age != "")
                    embed.AddField("Age", character.Age, inline: true);
                if (character.Race != "")
                    embed.AddField("Race", character.Race, inline: true);
                if (character.Pronouns != "")
                    embed.AddField("Pronouns", character.Pronouns, inline: true);
                if (character.Sexuality != "")
                    embed.AddField("Sexuality", character.Sexuality, inline: true);
                if (character.Origin != "")
                    embed.AddField("Origin", character.Origin, inline: true);
                if (character.Residence != "")
                    embed.AddField("Residence", character.Residence, inline: true);
                if (character.Resources != "")
                    embed.AddField("Additional Resources", character.Resources);
                if (tags.Count > 0)
                {
                    var tagline = "";
                    foreach (var tag in tags)
                    {
                        tagline += $"`{tag.Tag}` ";
                    }
                    embed.AddField("Tags", tagline);
                }
                
                embed.WithFooter($"Character by {user.Name}");
                if (character.Image != null)
                {
                    var ms = new MemoryStream(character.Image);
                    var imageName = $"character.{character.ImageType}";

                    if (character.CompactImage)
                        embed.ThumbnailUrl = $"attachment://{imageName}";
                    else
                        embed.ImageUrl = $"attachment://{imageName}";
                    await RespondWithFileAsync(ms, imageName, embed: embed.Build());
                    return;
                }
                else
                {
                    await RespondAsync(embed: embed.Build());
                    return;
                }
            }
        }
    }
}
