using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using JifBot.Models;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;
using System.ComponentModel;

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
        private bool print = false;

        public async Task Start(string[] args)
        {
            var db = new BotBaseContext();
            foreach (string arg in args)
            {
                if(arg == "--generatejs")
                {
                    print = true;
                }

                if(arg == "--test")
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
            if(print)
                printCommandsToJSON("commands.js");
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

        public void printCommandsToJSON(string file)
        {
            using (StreamWriter fileStream = File.CreateText(file))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.DefaultValueHandling = DefaultValueHandling.Ignore;
                foreach (Discord.Commands.CommandInfo c in this.commands.Commands)
                {
                    string aliases = "";
                    if (c.Aliases.Count > 1)
                    {
                        foreach (string alias in c.Aliases)
                        {
                            aliases = aliases + alias + ",";
                        }
                        aliases = aliases.Remove(0, aliases.IndexOf(",") + 1);
                        aliases = aliases.Remove(aliases.LastIndexOf(","));
                    }
                    CommandJSON command = new CommandJSON(c.Name, aliases, c.Remarks, c.Summary);

                    serializer.Serialize(fileStream, command);
                }
            }
            string temp = File.ReadAllText(file);
            temp = temp.Insert(0, "var jifBotCommands = [");
            temp = temp += "];";
            temp = temp.Replace("'", "\'");
            temp = temp.Replace("}", "},");
            File.WriteAllText(file, temp);
            Console.WriteLine("Commands have been printed");
            return;
        }
    }

    class CommandJSON
    {
        public CommandJSON(string commandName, string aliasName, string categoryName, string descriptionName)
        {
            command = commandName;
            alias = aliasName;
            category = categoryName;
            description = descriptionName;
        }

        [DefaultValue("")]
        public string command { get; set; }

        [DefaultValue("")]
        public string alias { get; set; }

        [DefaultValue("")]
        public string category { get; set; }

        [DefaultValue("")]
        public string description { get; set; }
    }
}