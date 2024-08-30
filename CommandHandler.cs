using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using JifBot.Models;
using System.Linq;
using System.Reflection;

namespace JifBot
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _interactions;
        private readonly IServiceProvider _services;

        public CommandHandler(DiscordSocketClient client, InteractionService interactions, IServiceProvider services)
        {
            _client = client;
            _interactions = interactions;
            _services = services;
        }

        public async Task InitializeAsync()
        {
            await _interactions.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            _client.InteractionCreated += HandleInteraction;
        }

        private async Task HandleInteraction(SocketInteraction arg)
        {
            try
            {
                var context = new SocketInteractionContext(_client, arg);
                if (context.Interaction.Type == InteractionType.ApplicationCommand)
                {
                    var result = await _interactions.ExecuteCommandAsync(context, _services);
                    if (!result.IsSuccess && result.Error != InteractionCommandError.UnknownCommand)
                    {
                        await context.Interaction.RespondAsync($"**ERROR:** {result.ErrorReason}");
                    }
                    else
                    {
                        var command = (SocketSlashCommand)context.Interaction;
                        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        var userId = context.User.Id;
                        var db = new BotBaseContext();
                        db.Add(new CommandCall { Command = command.CommandName, Timestamp = timestamp, ServerId = context.Guild.Id, UserId = userId });
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                if (arg.Type == InteractionType.ApplicationCommand)
                {
                    await arg.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
                }
            }
        }
    }
}
