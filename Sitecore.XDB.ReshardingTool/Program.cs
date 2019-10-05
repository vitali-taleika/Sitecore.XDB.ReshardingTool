using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Sitecore.XDB.ReshardingTool
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, true);

            IConfigurationRoot configuration = builder.Build();
            var appSettings = configuration.GetSection("AppSettings");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                var tool = new ReshardingTool(Log.Logger,
                    configuration.GetConnectionString("collection.source"),
                    configuration.GetConnectionString("collection.target"),
                    configuration.GetConnectionString("resharding.log"),
                    appSettings["ContactIdShardMap"],
                    appSettings["DeviceProfileIdShardMap"],
                    appSettings["ContactIdentifiersIndexShardMap"],
                    configuration.GetConnectionString("resharding.log") != null,
                    int.Parse(appSettings["ConnectionTimeout"]),
                    int.Parse(appSettings["BatchSize"]),
                    int.Parse(appSettings["RetryCount"]),
                    int.Parse(appSettings["RetryDelay"])
                );

                Console.WriteLine("Resharding is initialized, commands: -start, -stop, -quit/-q");
                Log.Information("Resharding is initialized, commands: -start/-stop");

                CancellationTokenSource cancelTokenSource = null;

                var isTaskActive = false;
                var isQuit = false;
                while (!isQuit)
                {
                    var command = Console.ReadLine();
                    switch (command)
                    {
                        case "-start":
                            Console.WriteLine("Starting resharding process");
                            Log.Information("Starting resharding process");
                            cancelTokenSource = new CancellationTokenSource();

                            void Job()
                            {
                                var stopwatch = new Stopwatch();
                                stopwatch.Start();
                                isTaskActive = true;
                                try
                                {
                                    var interactionsDate = string.IsNullOrEmpty(appSettings["InteractionsFromDate"]) ? (DateTime?)null : DateTime.ParseExact(appSettings["InteractionsFromDate"], "yyyy-MM-dd", CultureInfo.InvariantCulture);
                                    var interactionsFilterByEventDefinitions = appSettings.GetSection("InteractionsFilterByEventDefinitions")?.GetChildren().Select(x => x.Value).ToList();
                                    tool.RunAsync(interactionsDate, interactionsFilterByEventDefinitions, cancelTokenSource.Token).Wait();
                                    Console.WriteLine("Resharding is finished");
                                    Log.Information("Resharding is finished");
                                }
                                catch (AggregateException ae)
                                {
                                    foreach (var exp in ae.InnerExceptions)
                                    {
                                        if (exp is TaskCanceledException)
                                        {
                                            Console.WriteLine("Resharding is stopped");
                                            Log.Information("Resharding is stopped");
                                        }
                                        else
                                        {
                                            Console.WriteLine(exp);
                                            Log.Error(exp, exp.Message);
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                    Log.Error(e, e.Message);
                                }
                                finally
                                {
                                    cancelTokenSource.Dispose();
                                    cancelTokenSource = null;
                                }

                                isTaskActive = false;
                                stopwatch.Stop();
                                Console.WriteLine($"Time elapsed: {stopwatch.Elapsed}");
                                Log.Information($"Time elapsed: {stopwatch.Elapsed}");
                            }

                            Task.Run(Job);
                            break;

                        case "-stop":
                            if (isTaskActive)
                            {
                                Console.WriteLine("Stopping resharding process");
                                Log.Information("Stopping resharding process");
                                cancelTokenSource?.Cancel();
                            }

                            break;
                        case "-q":
                        case "-quit":
                            if (isTaskActive)
                            {
                                Console.WriteLine("Stopping resharding process");
                                Log.Information("Stopping resharding process");
                                cancelTokenSource?.Cancel();

                                while (isTaskActive)
                                {
                                    await Task.Delay(1000);
                                }
                            }

                            isQuit = true;
                            break;
                        default:
                            Console.WriteLine("Unknown command " + command);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Log.Error(e, e.Message);
                Console.ReadLine();
            }

            Log.CloseAndFlush();
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }
    }
}