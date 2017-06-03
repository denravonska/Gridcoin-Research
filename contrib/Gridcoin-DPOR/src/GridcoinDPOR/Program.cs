// Copyright (c) 2017 The Gridcoin Developers
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

namespace GridcoinDPOR
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string directory = "";
                string command = "";
                string message = "";

                foreach(var arg in args)
                {
                    var split = arg.Split(new char[] {'='}, StringSplitOptions.RemoveEmptyEntries);

                    switch(split[0])
                    {
                        case "-d":
                            directory = split[1];
                            break;
                        case "-c":
                            command = split[1];
                            break;
                        case "-m":
                            message = split[1];
                            break;
                    }
                }

                Task.Run(async () =>
                {
                    // Do any async anything you need here without worry
                    switch(command)
                    {
                        case "syncdpor2":
                            Console.Write("1");
                            await Service.SyncDPOR2(directory);
                            break;
                    }

                }).GetAwaiter().GetResult();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}