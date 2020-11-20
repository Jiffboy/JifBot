using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
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

        private DiscordSocketClient client;
        public static string configName = "Live";
        public CommandService commands;
        private DiscordSocketClient bot;
        private IServiceProvider map;
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

            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                //WebSocketProvider = Discord.Net.Providers.WS4Net.WS4NetProvider.Instance
                MessageCacheSize = 500,
                LogLevel = LogSeverity.Verbose
            });

            client.Log += JifBot.EventHandler.WriteLog;
            client.Ready += OnReady;

            var config = db.Configuration.AsQueryable().Where(cfg => cfg.Name == configName).First();

            await client.LoginAsync(TokenType.Bot, config.Token);
            await client.StartAsync();

            map = ConfigureServices();
            bot = map.GetService<DiscordSocketClient>();
            commands = map.GetService<CommandService>();
            eventHandler = new JifBot.EventHandler(map);

            bot.UserJoined += eventHandler.AnnounceUserJoined;
            bot.UserLeft += eventHandler.AnnounceLeftUser;
            bot.MessageDeleted += eventHandler.SendMessageReport;
            bot.MessageReceived += eventHandler.HandleMessage;

            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), map);

            //Block this program untill it is closed
            await Task.Delay(-1);
        }

        private Task OnReady()
        {
            var db = new BotBaseContext();
            var config = db.Configuration.AsQueryable().Where(cfg => cfg.Name == configName).First();
            client.SetGameAsync(config.Prefix + "commands");
            db.Command.RemoveRange(db.Command);
            db.CommandAlias.RemoveRange(db.CommandAlias);
            foreach (Discord.Commands.CommandInfo c in this.commands.Commands)
            {
                if (c.Module.Name != "Hidden")
                {
                    if (c.Aliases.Count > 1)
                    {
                        foreach (string alias in c.Aliases)
                        {
                            if (alias != c.Name)
                            {
                                db.Add(new CommandAlias { Alias = alias, Command = c.Name });
                            }
                        }
                    }
                    db.Add(new Command { Name = c.Name, Category = c.Module.Name, Usage = c.Remarks.Replace("-c-", $"{config.Prefix}{c.Name}"), Description = c.Summary.Replace("-p-", config.Prefix) });
                }
            }
            var update = db.Variable.AsQueryable().Where(V => V.Name == "lastCmdUpdateTime").FirstOrDefault();
            update.Value = DateTime.Now.ToLocalTime().ToString();
            db.SaveChanges();

            return Task.CompletedTask;
        }

        public IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection()
                //.AddSingleton(new AudioService())
                .AddSingleton(client)
                .AddSingleton(new CommandService(new CommandServiceConfig { CaseSensitiveCommands = false }));
            var provider = new DefaultServiceProviderFactory().CreateServiceProvider(services);
            return provider;
        }
    }
}