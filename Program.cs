using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using JifBot;
using JifBot.Models;
using JifBot.CommandHandler;
using System.Linq;
using Newtonsoft.Json;
using System.IO;

namespace JIfBot
{
    public class Program
    {
        public static void Main(string[] args) =>
            new Program().Start(args).GetAwaiter().GetResult();

        private DiscordSocketClient client;
        private CommandHandler handler;
        public static string configName = "Live";
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

            client.Log += Logger;
            client.Ready += OnReady;

            var config = db.Configuration.AsQueryable().AsQueryable().Where(cfg => cfg.Name == configName).First();

            await client.LoginAsync(TokenType.Bot, config.Token);
            await client.StartAsync();

            var serviceProvider = ConfigureServices();
            handler = new CommandHandler(serviceProvider);
            await handler.ConfigureAsync();


            //Block this program untill it is closed
            await Task.Delay(-1);
        }

        private Task OnReady()
        {
            var db = new BotBaseContext();
            var config = db.Configuration.AsQueryable().AsQueryable().Where(cfg => cfg.Name == configName).First();
            client.SetGameAsync(config.Prefix + "commands");
            if(print)
                printCommandsToJSON("commands.js");
            return Task.CompletedTask;
        }

        private static Task Logger(LogMessage lmsg)
        {
            var cc = Console.ForegroundColor;
            switch (lmsg.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }
            Console.WriteLine($"{DateTime.Now} [{lmsg.Severity,8}] {lmsg.Source}: {lmsg.Message}");
            Console.ForegroundColor = cc;
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
                foreach (Discord.Commands.CommandInfo c in this.handler.commands.Commands)
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
}