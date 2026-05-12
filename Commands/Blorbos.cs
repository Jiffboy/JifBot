using Discord;
using Discord.Interactions;
using JifBot.Builders;
using JifBot.Models;
using JifBot.Utils;
using System.IO;
using System.Linq;
using System.Net;
using System;
using System.Threading.Tasks;

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
                    var embed = new JifBotEmbedBuilder();
                    embed.Title = $"{author.Username}'s characters";

                    embed.ThumbnailUrl = author.GetDisplayAvatarUrl();
                    foreach (var character in characters)
                    {
                        var desc = $"Id: `{character.Key}`";
                        desc += $"\n[Blorbopedia](https://jifbot.com/b/{character.Key.Replace(" ", "%20")})";
                        if (character.Resources != "" && Uri.IsWellFormedUriString(character.Resources, UriKind.Absolute))
                        {
                            desc += $"\n[Resources]({character.Resources})";
                        }
                        desc += $"\n";
                        embed.AddField(character.Name, desc, inline: true);

                    }
                    await RespondAsync(embed: embed.Build());
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

        [SlashCommand("makeevent", "Makes an event.")]
        public async Task MakeEvent()
        {
            var eventUIBuilder = new EventUIBuilder();
            await RespondWithModalAsync(eventUIBuilder.BuildModal());  
        }

        private async Task SendCharacterBio(Character character)
        {
            var db = new BotBaseContext();
            var user = db.User.AsQueryable().Where(u => u.UserId == character.UserId).FirstOrDefault();
            var tags = db.CharacterTag.AsQueryable().Where(c => c.Key == character.Key).ToList();

            JifBotEmbedBuilder embed = new JifBotEmbedBuilder();
            embed.Title = character.Name != "" ? character.Name : character.Key;
            embed.Url = $"https://jifbot.com/b/{character.Key.Replace(" ", "%20")}";
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
            if (character.Resources != "" && Uri.IsWellFormedUriString(character.Resources, UriKind.Absolute))
                embed.AddField("Additional Resources", $"<{character.Resources}>");
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

            var img = new CommonImage(character.Image, character.ImageType);
            if (!img.isNull)
            {
                if (character.CompactImage)
                    embed.ThumbnailUrl = img.thumbnailUrl;
                else
                    embed.ImageUrl = img.thumbnailUrl;
                await RespondWithFileAsync(img.GetMS(), img.imgName, embed: embed.Build());
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
