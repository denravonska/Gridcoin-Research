﻿// Copyright (c) 2017 The Gridcoin Developers
// Distributed under the MIT/X11 software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

using System;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using GridcoinDPOR.Util;
using Serilog;
using Serilog.Sinks.RollingFile;
using Serilog.Core;
using Serilog.Events;
using GridcoinDPOR.Data;

namespace GridcoinDPOR
{
    class Program
    {
        static void Main(string[] args)
        {
            // -gridcoindatadir=C:\\Example -syncdpor2=<XML>DATA</XML>
            // -gridcoindatadir=C:\\Example -syncdpor2 -debug
            // -gridcoindatadir=C:\\Example -neuralhash
            // -gridcoindatadir=C:\\Example -neuralcontract

            try
            {
                string gridcoinDataDir = "";
                string commandName = "";
                string commandOption = "";
                bool noTeam = false;
                bool debug = false;

                foreach(var arg in args)
                {
                    if (arg.StartsWith("-ping"))
                    {
                        Console.WriteLine("PONG");
                        Environment.Exit(0);
                    }

                    if (arg.StartsWith("-gridcoindatadir"))
                    {
                        gridcoinDataDir = arg.Replace("-gridcoindatadir=", "");
                    }
                    if (arg.StartsWith("-syncdpor2"))
                    {
                        commandName = "syncdpor2";
                        commandOption = arg.Replace("-syncdpor2", "").Replace("=", "");
                    }
                    if (arg.StartsWith("-neuralcontract"))
                    {
                        commandName = "neuralcontract";
                    }
                    if (arg.StartsWith("-neuralhash"))
                    {
                        commandName = "neuralhash";
                    }
                    if (arg.StartsWith("-debug"))
                    {
                        debug = true;
                    }
                    if(arg.StartsWith("-noteam"))
                    {
                        noTeam = true;
                    }
                }

                // override commandOption when debug is set
                if (debug && commandName == "syncdpor2")
                {
                    commandOption = File.ReadAllText("syncdpor.dat");    
                }

                if (string.IsNullOrEmpty(gridcoinDataDir))
                {
                    Console.WriteLine("ERROR: You must specify the path to the Gridcoin Data Directory with the -gridcoindatadir option. e.g -gridcoindatadir=[PATH]");
                    Environment.Exit(-1);
                }
                
                var gridcoinConf = Path.Combine(gridcoinDataDir, "gridcoinresearch.conf");
                if (!File.Exists(gridcoinConf))
                {
                    Console.WriteLine("ERROR: Could not find the gridcoinresearch.conf file in the directory specified.");
                    Environment.Exit(-1);
                }

                if (string.IsNullOrEmpty(commandName))
                {
                    Console.WriteLine("ERROR: You must specify a command to run. Available commands are -syncdpor2, -neuralcontract or -neuralhash");
                    Environment.Exit(-1);
                }

                var logPath = Path.Combine(gridcoinDataDir, "DPOR", "logs", "debug-{Date}.log");
                var levelSwitch = new LoggingLevelSwitch();
                var logger = new LoggerConfiguration().MinimumLevel.ControlledBy(levelSwitch)
                                                      .WriteTo.RollingFile(logPath)
                                                      .CreateLogger();

                var contractGenerator = new Contract(logger);
                                                
                // assign logger to classes we want logging in
                // TODO: probably a better way of injecting the logging.
                                                   
                logger.Information("Logging started at [Information] level");
                
                // should we switch logging to Verbose?
                var conf = File.ReadAllText(gridcoinConf);
                if (conf.Contains("debug2=true"))
                {
                    levelSwitch.MinimumLevel = LogEventLevel.Verbose;
                    logger.Information("debug2=true detected in gridcoinresearch.conf logging changed to [Verbose]");
                }

                Task.Run(async () =>
                {
                    // Do any async anything you need here without worry
                    switch(commandName)
                    {
                        case "syncdpor2":
                            Console.WriteLine("1");
                            logger.Information("SyncDPOR2 started");
                            using(var dbContext = GridcoinContext.Create(gridcoinDataDir))
                            {
                                var fileDownloader = new FileDownloader(logger);
                                var dataSynchronizer = new DataSynchronizer(logger, dbContext, fileDownloader);
                                await dataSynchronizer.SyncAsync(gridcoinDataDir, commandOption);
                            }
                            logger.Information("SyncDPOR2 finished");
                            break;

                        case "neuralcontract":
                            logger.Information("Getting neural contract");
                            string contract = await contractGenerator.GetContract(gridcoinDataDir, noTeam);
                            Console.WriteLine(contract);
                            break;

                        case "neuralhash":
                            logger.Information("Getting neural hash");
                            string hash = await contractGenerator.GetNeuralHash(gridcoinDataDir, noTeam);
                            Console.WriteLine(hash);
                            break;

                        default:
                            Console.WriteLine("ERROR: Invalid command specified. Available commands are -syncdpor2, -neuralcontract or -neuralhash");
                            Environment.Exit(-1);
                            break;
                    }

                }).GetAwaiter().GetResult();
            }
            catch(Exception ex)
            {
                Console.WriteLine("ERROR: {0}", ex.Message);
                Environment.Exit(-1);
            }
        }
    }
}