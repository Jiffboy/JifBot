using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using JifBot.Config;
using JifBot.CommandHandler;
using System.IO;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Text;
using Microsoft.ML.Core.Data;

namespace JIfBot
{
    public class Program
    {
        public static void Main(string[] args) =>
            new Program().Start().GetAwaiter().GetResult();

        private DiscordSocketClient client;
        public static MLContext mLContext;
        private CommandHandler handler;

        static readonly string _trainDataPath = Path.Combine(Environment.CurrentDirectory, "Data", "wikipedia-detox-250-line-data.tsv");
        static readonly string _testDataPath = Path.Combine(Environment.CurrentDirectory, "Data", "wikipedia-detox-250-line-test.tsv");
        public static readonly string _recordedDataPath = Path.Combine(Environment.CurrentDirectory, "Data", "our_friends_are_fucking_weapons.tsv");
        public static readonly string _modelPath = Path.Combine(Environment.CurrentDirectory, "Data", "Model.zip");
        public static TextLoader _textLoader;

        public async Task Start()
        {
            mLContext = new MLContext(seed: 0);
            BuildModel();

            CreateJSON(); // create a JSON file to run from

            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                //WebSocketProvider = Discord.Net.Providers.WS4Net.WS4NetProvider.Instance
                MessageCacheSize = 500,
                LogLevel = LogSeverity.Verbose
            });
            client.Log += Logger;
            await client.LoginAsync(TokenType.Bot, BotConfig.Load().Token);
            await client.StartAsync();

            var serviceProvider = ConfigureServices();
            handler = new CommandHandler(serviceProvider);
            await handler.ConfigureAsync();

            //Block this program untill it is closed
            await Task.Delay(-1);
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

            config.Prefix = file.ReadLine();              // Read the bot prefix from console.

            config.Token = file.ReadLine();              // Read the bot token from console.

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

        public void BuildModel()
        {
            _textLoader = mLContext.Data.CreateTextReader(new TextLoader.Arguments()
                {
                    Separator = "tab",
                    HasHeader = true,
                    Column = new[]
                        {
                            new TextLoader.Column("Label", DataKind.Bool, 0),
                            new TextLoader.Column("SentimentText", DataKind.Text, 1)
                        }
                }
            ); //end CreateTextReader
            var model = TrainModel(mLContext, _trainDataPath);

            SaveModelAsFile(mLContext, model);
        }

        public static ITransformer TrainModel(MLContext mlContext, string dataPath)
        {
            IDataView dataView = _textLoader.Read(dataPath);
            var pipeline = mlContext.Transforms.Text.FeaturizeText("SentimentText", "Features")
                //These numbers are literally magic
                .Append(mlContext.BinaryClassification.Trainers.FastTree(numLeaves: 50, numTrees: 50, minDatapointsInLeaves: 20));

            return pipeline.Fit(dataView);
        }

        public static CalibratedBinaryClassificationMetrics EvaluateModel(MLContext mlContext, ITransformer model)
        {
            IDataView dataView = _textLoader.Read(_testDataPath);
            var predictions = model.Transform(dataView);
            return mlContext.BinaryClassification.Evaluate(predictions, "Label");
        }

        private static void SaveModelAsFile(MLContext mlContext, ITransformer model)
        {
            using (var fs = new FileStream(_modelPath, FileMode.Create, FileAccess.Write, FileShare.Write))
                mlContext.Model.Save(model, fs);
        }
    }
}