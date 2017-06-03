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

namespace GridcoinDPOR.Util
{
    public class WebUtil
    {
        private static HttpClient _httpClient = new HttpClient();

        public static async Task<bool> DownloadFile(string requestUri, string filename)
        {
            try
            {
                using(var response = await _httpClient.GetAsync(
                    requestUri: requestUri, 
                    completionOption: HttpCompletionOption.ResponseHeadersRead
                ))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        // TODO: Log failure
                        return false;
                    }

                    // only download the full file if it's newer than the local file.
                    DateTime localFileLastModified = DateTime.MinValue;
                    if (File.Exists(filename))
                    {
                        localFileLastModified = File.GetLastAccessTimeUtc(filename);
                    }

                    var remoteFileLastModified = response.Content.Headers.LastModified;
                    Console.WriteLine("Last-Modified: {0}", remoteFileLastModified);
                    
                    if (localFileLastModified < remoteFileLastModified)
                    {
                        Console.WriteLine("Local {0} is out of date", filename);
                        using(var fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await response.Content.CopyToAsync(fileStream);
                        }

                        if (remoteFileLastModified.HasValue)
                        {
                            File.SetLastAccessTimeUtc(filename, remoteFileLastModified.Value.UtcDateTime);
                        }

                        return true;
                    }
                    else
                    {
                        Console.WriteLine("Local {0} is the latest", filename);
                        return true;
                    }
                }   
            }
            catch(Exception ex)
            {
                Console.WriteLine("Failed to download file: {0}", ex);
                return false;
            }
        }
    }
}
