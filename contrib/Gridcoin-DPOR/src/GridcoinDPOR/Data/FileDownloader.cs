// Copyright (c) 2017 The Gridcoin Developers
// Distributed under the MIT/X11 software license, see the accompanying
// file COPYING or http://www.opensource.org/licenses/mit-license.php.

using GridcoinDPOR.Logging;
using Serilog;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace GridcoinDPOR.Data
{
    public class FileDownloader
    {
        private static ILogger _logger = new NullLogger();
        public static ILogger Logger 
        { 
            get { return _logger; } 
            set { _logger = value; }
        }

        private readonly HttpClient _httpClient;

        public FileDownloader()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public FileDownloader(ILogger logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<bool> DownloadFileAsync(string requestUri, string filePath)
        {
            for (int i = 0; i < 1; i++)
            {
                try
                {
                     _logger.Information("Attempt: {0} of downloading file: {1}", i+1, requestUri);
                    var result = await InternalDownloadFileAsync(requestUri, filePath);
                    if (result == HttpStatusCode.OK)
                    {
                        _logger.Information("Downloaded successfully to {0}", filePath);
                        return true;
                    }
                    else
                    {
                        _logger.Warning("Downloaded failed with Status Code: {0}", result);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning("Error while downloading file. Message: {0}, Retrying...", ex.Message);
                }
            }

            return false;
        }

        private async Task<HttpStatusCode> InternalDownloadFileAsync(string requestUri, string filePath)
        {
            var filename = Path.GetFileName(filePath);

            using(var response = await _httpClient.GetAsync(
                requestUri: requestUri, 
                completionOption: HttpCompletionOption.ResponseHeadersRead
            ))
            {
                if (!response.IsSuccessStatusCode)
                {
                    return response.StatusCode;
                }

                // only download the full file if it's newer than the local file.
                DateTime localFileLastModified = DateTime.MinValue;
                if (File.Exists(filePath))
                {
                    localFileLastModified = File.GetLastAccessTimeUtc(filePath);
                    _logger.Debug("Last-Modified of local file {0} is {1}", filename, localFileLastModified);
                }

                DateTime remoteFileLastModified = DateTime.UtcNow;
                if (response.Content.Headers.LastModified.HasValue)
                {
                    remoteFileLastModified = response.Content.Headers.LastModified.Value.UtcDateTime;
                }

                _logger.Debug("Last-Modified of remote file {0} is {1}", requestUri, remoteFileLastModified);

                    
                if (localFileLastModified < remoteFileLastModified)
                {
                    _logger.Debug("Local file {0} is older than remote file {1}, downloading file...", filename, requestUri);
                    using(var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }

                    File.SetLastAccessTimeUtc(filePath, remoteFileLastModified);
                    _logger.Debug("Assigned Last-Modified to local file {0}", remoteFileLastModified);
                }
                else
                {
                    _logger.Debug("Skipping download as local file {0} is the latest", filename);
                }

                return response.StatusCode;
            }
        }
    }
}
