using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using JifBot.Config;
using JifBot.CommandHandler;
using System.IO;

namespace JIfBot
{
    public class Program
    {
        public static void Main(string[] args) =>
            new Program().Start(args).GetAwaiter().GetResult();

        private DiscordSocketClient client;
        private CommandHandler handler;
        private ulong timerLaunchId = 0;
        private string timerLaunchMessage = "";

        public async Task Start(string[] args)
        {
            if (args.Length == 2)
            {
                timerLaunchId = Convert.ToUInt64(args[0]);
                timerLaunchMessage = args[1];
            }

            CreateJSON(); // create a JSON file to run from

            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                //WebSocketProvider = Discord.Net.Providers.WS4Net.WS4NetProvider.Instance
                MessageCacheSize = 500,
                LogLevel = LogSeverity.Verbose
            });

            client.Log += Logger;
            client.Ready += OnReady;

            await client.LoginAsync(TokenType.Bot, BotConfig.Load().Token);
            await client.StartAsync();
            await client.SetGameAsync(BotConfig.Load().Prefix + "commands");

            var serviceProvider = ConfigureServices();
            handler = new CommandHandler(serviceProvider);
            await handler.ConfigureAsync();


            //Block this program untill it is closed
            await Task.Delay(-1);
        }

        private Task OnReady()
        {
            if (timerLaunchId > 0)
            {
                var channel = client.GetChannel(timerLaunchId) as IMessageChannel;
                if (channel != null)
                    channel.SendMessageAsync(timerLaunchMessage);
                System.Environment.Exit(0);
            }
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

        public static void CreateJSON()
        {
            if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "configuration")))
                Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "configuration"));

            string loc = Path.Combine(AppContext.BaseDirectory, "configuration/config.json");

            if (File.Exists(loc))                              // Check if the configuration file exists.
            {
                File.Delete(loc);
            }
            var config = new BotConfig();               // Create a new configuration object.
            System.IO.StreamReader file = new System.IO.StreamReader("references/LoadInformation.txt");

            config.Prefix = file.ReadLine();

            config.Token = file.ReadLine();

            config.DictKey = file.ReadLine();

            config.DictId = file.ReadLine();

            config.Save();                                  // Save the new configuration object to file.
            Console.WriteLine("Configuration has been loaded");
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