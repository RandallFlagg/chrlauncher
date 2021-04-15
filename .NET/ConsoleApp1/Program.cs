using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace ConsoleApp1
{
    class Program
    {

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        private static readonly HttpClient client = new HttpClient();
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var platform = ChromiumPlatform.Win64;
            var baseUrlDownload =
                "https://commondatastorage.googleapis.com/chromium-browser-snapshots/index.html?prefix=";
            string platformName;
            switch (platform)
            {
                case ChromiumPlatform.Win32:
                    platformName = "Win";
                    break;
                case ChromiumPlatform.Win64:
                    platformName = "Win_x64";
                    break;
                default:
                    throw new NotImplementedException("platform: " + platform);
            }
            var versionUrl = $"https://www.googleapis.com/storage/v1/b/chromium-browser-snapshots/o/{platformName}%2FLAST_CHANGE";
            var version = await ProcessLatestVersion(versionUrl);
            var downloadUrl =
                $"https://www.googleapis.com/download/storage/v1/b/chromium-browser-snapshots/o/{platformName}%2F{version}%2Fchrome-win.zip?alt=media";
            var savePath = Path.Combine(AssemblyDirectory, $"chromium_{version}.zip");
            await ProcessDownload(downloadUrl, savePath);
        }

        private static async Task<string> ProcessLatestVersion(string url)
        {
            client.DefaultRequestHeaders.Accept.Clear();
            //client.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            //client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");

            var streamTask = client.GetStreamAsync(url);
            //var streamTask = client.GetStringAsync(url);
            var latestVersionJson = await JsonSerializer.DeserializeAsync<ExpandoObject>(await streamTask);
            var commitNumber = (JsonElement)latestVersionJson.Where(v => v.Key == "metadata").Select(v => v.Value).FirstOrDefault();
            var version = JsonDocument.Parse(commitNumber.ToString()).RootElement.GetProperty("cr-commit-position-number").ToString();
            //a.Where(v=>v.Key == "cr-commit-position-number").Select(v => v.Value);
            //var repositories = await JsonSerializer.DeserializeAsync<List<Repository>>(await streamTask);
            
            Console.Write(version);
            return version;
        }

        private static async Task ProcessDownload(string url, string filePath)
        {
            //url = "https://upload.wikimedia.org/wikipedia/commons/c/ca/1x1.png"; //TODO: Remove this line - For testing only
            client.DefaultRequestHeaders.Accept.Clear();
            //client.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            //client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");

            //var streamTask = await client.GetStreamAsync(url);
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(url, filePath);
            }
            //var repositories = await JsonSerializer.DeserializeAsync<List<Repository>>(await streamTask);
            //throw new NotImplementedException("process response");
            //Console.Write(msg);
        }
    }

    internal enum ChromiumPlatform
    {
        Win64,
        Win32
    }
}
