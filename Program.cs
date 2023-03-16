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

namespace JIfBot
{
    public class Program
    {
        public static void Main(string[] args) =>
            new Program().Start(args).GetAwaiter().GetResult();

        public static string configName = "Live";
        public static DateTime startTime = DateTime.Now;
        private DiscordSocketClient client;
        private InteractionService interactions;
        public CommandService commands;
        public JifBot.CommandHandler commandHandler;
        private JifBot.EventHandler eventHandler;

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
            commandHandler = services.GetService<JifBot.CommandHandler>();
            eventHandler = new JifBot.EventHandler(services);

            client.Log += JifBot.EventHandler.WriteLog;
            interactions.Log += JifBot.EventHandler.WriteLog;
            client.Ready += OnReady;

            client.UserJoined += eventHandler.AnnounceUserJoined;
            client.UserLeft += eventHandler.AnnounceLeftUser;
            client.MessageDeleted += eventHandler.SendMessageReport;
            client.MessageReceived += eventHandler.HandleMessage;
            client.ReactionAdded += eventHandler.HandleReactionAdded;
            client.ReactionRemoved += eventHandler.HandleReactionRemoved;

            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);

            await client.LoginAsync(TokenType.Bot, config.Token);
            await client.StartAsync();
            await commandHandler.InitializeAsync();

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

            foreach (var command in interactions.SlashCommands)
            {
                db.Add(new Command { Name = command.Name, Description = command.Description, Category = command.Module.Name });
                foreach (var variable in command.Parameters)
                {
                    Console.WriteLine(variable.Name + " | " + variable.Description + " | " + variable.IsRequired);
                    db.Add(new CommandParameter { Command = command.Name, Name = variable.Name, Description = variable.Description, Required = variable.IsRequired });
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