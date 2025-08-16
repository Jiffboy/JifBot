using System.Threading.Tasks;
using System.Linq;
using Discord;
using Discord.Interactions;
using JifBot.Models;
using System.IO;
using System.Net;

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
