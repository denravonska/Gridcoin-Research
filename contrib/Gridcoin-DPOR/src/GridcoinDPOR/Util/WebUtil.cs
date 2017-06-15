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
using Serilog;
using Serilog.Core;
using GridcoinDPOR.Logging;

namespace GridcoinDPOR.Util
{
    public static class WebUtil
    {
        private static ILogger _logger = new NullLogger();
        public static ILogger Logger 
        { 
            get { return _logger; } 
            set { _logger = value;}
        }

        private static HttpClient _httpClient = new HttpClient();

        public static async Task<bool> DownloadFile(string requestUri, string filePath)
        {
            try
            {
                var filename = Path.GetFileName(filePath);

                //using(var _httpClient = new HttpClient())
                using(var response = await _httpClient.GetAsync(
                    requestUri: requestUri, 
                    completionOption: HttpCompletionOption.ResponseHeadersRead
                ))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.ForContext(nameof(WebUtil)).Warning("Invalid status code detected when trying to download file: {0}", requestUri);
                        return false;
                    }

                    // only download the full file if it's newer than the local file.
                    DateTime localFileLastModified = DateTime.MinValue;
                    if (File.Exists(filePath))
                    {
                        localFileLastModified = File.GetLastAccessTimeUtc(filePath);
                        _logger.ForContext(nameof(WebUtil)).Debug("Last-Modified of local file {0} is {1}", filename, localFileLastModified);
                    }

                    DateTime remoteFileLastModified = DateTime.UtcNow;
                    if (response.Content.Headers.LastModified.HasValue)
                    {
                        remoteFileLastModified = response.Content.Headers.LastModified.Value.UtcDateTime;
                    }

                    _logger.ForContext(nameof(WebUtil)).Debug("Last-Modified of remote file {0} is {1}", requestUri, remoteFileLastModified);

                    
                    if (localFileLastModified < remoteFileLastModified)
                    {
                        _logger.ForContext(nameof(WebUtil)).Information("Local file {0} is older than remote file {1}, downloading file...", filename, requestUri);
                        using(var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await response.Content.CopyToAsync(fileStream);
                        }

                        File.SetLastAccessTimeUtc(filePath, remoteFileLastModified);
                        _logger.ForContext(nameof(WebUtil)).Debug("Assigned Last-Modified to local file {0}", remoteFileLastModified);
                        
                        return true;
                    }
                    else
                    {
                         _logger.ForContext(nameof(WebUtil)).Information("Skipping download as local file {0} is the latest", filename);
                        return true;
                    }
                }   
            }
            catch(Exception ex)
            {
                _logger.ForContext(nameof(WebUtil)).Error("{0}", ex);
                return false;
            }
        }
    }
}
