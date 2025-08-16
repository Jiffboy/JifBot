using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using JifBot.Models;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace JIfBot
{
    public class Program
    {
        public static void Main(string[] args) =>
            new Program().Start(args).GetAwaiter().GetResult();

        public static string configName = "Live";
        public static DateTime startTime = DateTime.Now;
        public static string currLeagueVersion = "";
        public static Dictionary<string, string> championLookup = new Dictionary<string, string>();
        private DiscordSocketClient client;
        private InteractionService interactions;
        public CommandService commands;
        public JifBot.CronService cronService;
        public JifBot.CommandHandler commandHandler;
        private JifBot.EventHandler eventHandler;
        private JifBot.ModalHandler modalHandler;
        private JifBot.ButtonHandler buttonHandler;

        public async Task Start(string[] args)
        {
            var db = new BotBaseContext();
            foreach (string arg in args)
            {
                if (arg == "--test")
                {
                    configName = "Test";
                }
            }
            var config = db.Configuration.AsQueryable().Where(cfg => cfg.Name == configName).First();
            IServiceProvider services = ConfigureServices();

            client = services.GetService<DiscordSocketClient>();
            interactions = services.GetService<InteractionService>();
            commands = services.GetService<CommandService>();
            cronService = new JifBot.CronService(services);
            commandHandler = services.GetService<JifBot.CommandHandler>();
            eventHandler = new JifBot.EventHandler(services);
            modalHandler = new JifBot.ModalHandler();
            buttonHandler = new JifBot.ButtonHandler();

            client.Log += JifBot.EventHandler.WriteLog;
            interactions.Log += JifBot.EventHandler.WriteLog;
            client.Ready += OnReady;

            client.UserJoined += eventHandler.AnnounceUserJoined;
            client.UserLeft += eventHandler.AnnounceLeftUser;
            client.MessageDeleted += eventHandler.SendMessageReport;
            client.MessageReceived += eventHandler.HandleMessage;
            client.ReactionAdded += eventHandler.HandleReactionAdded;
            client.ReactionRemoved += eventHandler.HandleReactionRemoved;

            client.ModalSubmitted += modalHandler.HandleModalSubmitted;

            client.ButtonExecuted += buttonHandler.HandleButton;

            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);

            await client.LoginAsync(TokenType.Bot, config.Token);
            await client.StartAsync();
            await commandHandler.InitializeAsync();

            await cronService.Run();

            //Block this program untill it is closed
            await Task.Delay(-1);
        }

        private async Task OnReady()
        {
            if (interactions == null)
            {
                throw new ArgumentNullException("InteractionService cannot be null.");
            }
            await interactions.RegisterCommandsGloballyAsync();
            await client.SetGameAsync("Big Snooze Simulator");
            WriteCommandsToDb(interactions);

        }

        private void WriteCommandsToDb(InteractionService interaction)
        {
            var db = new BotBaseContext();
            db.Command.RemoveRange(db.Command);
            db.CommandParameter.RemoveRange(db.CommandParameter);
            db.CommandParameterChoice.RemoveRange(db.CommandParameterChoice);

            foreach (var command in interactions.SlashCommands)
            {
                // So we can have two word categories
                var category = Regex.Replace(command.Module.Name, @"([A-Z][a-z]*)([A-Z][a-z]*)*", @"$1 $2").TrimEnd(' ');
                db.Add(new Command { Name = command.Name, Description = command.Description, Category =  category});
                foreach (var variable in command.Parameters)
                {
                    db.Add(new CommandParameter { Command = command.Name, Name = variable.Name, Description = variable.Description, Required = variable.IsRequired });
                    if (variable.Choices.Count > 0)
                    {
                        foreach(var choice in variable.Choices)
                        {
                            db.Add(new CommandParameterChoice { Command = command.Name, Parameter = variable.Name, Name = choice.Name });
                        }
                    }
                }
            }
            var update = db.Variable.AsQueryable().Where(V => V.Name == "lastCmdUpdateTime").FirstOrDefault();
            update.Value = DateTime.Now.ToLocalTime().ToString();
            db.SaveChanges();
        }

        public IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection()
                //.AddSingleton(new AudioService())
                .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
                {
                    //WebSocketProvider = Discord.Net.Providers.WS4Net.WS4NetProvider.Instance
                    MessageCacheSize = 500,
                    LogLevel = LogSeverity.Verbose,
                    AlwaysDownloadUsers = true,
                    GatewayIntents = GatewayIntents.All
                }))
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton(new CommandService(new CommandServiceConfig { CaseSensitiveCommands = false }))
                .AddSingleton<JifBot.CommandHandler>();
            var provider = new DefaultServiceProviderFactory().CreateServiceProvider(services);
            return provider;
        }
    }
}