// Copyright (c) 2017 The Gridcoin Developers
// Distributed under the MIT/X11 software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

using Serilog;
using Serilog.Core;

namespace GridcoinDPOR.Logging
{
    public static class LoggingExtensions
    {
        public static ILogger ForContext(this ILogger logger, string context)
        {
            return logger.ForContext(Constants.SourceContextPropertyName, context);
        }
    }
}