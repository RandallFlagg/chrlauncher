using System.Text.Json;

namespace ClassLibrary1
{
    public class Class1
    {
        public static async Task DownloadFile(Uri updateUrl, byte bit)
        {
            const string fileName = "chrome-win.zip";
            using HttpClient client = new();
            try
            {
                var updateResponse = await client.GetStringAsync(updateUrl);
                // Parse JSON to extract the file URL
                using var doc = JsonDocument.Parse(updateResponse);
                var revision = doc.RootElement.GetProperty("chromium").GetProperty("windows").GetProperty("revision").GetInt32();
                LOG("revision: " + revision);
                var fileUrl = $"https://storage.googleapis.com/chromium-browser-snapshots/Win{(bit == 32 ? string.Empty : "_x" + bit)}/{revision}/{fileName}";
                // Second request: download file
                if (false)
                {
                    using HttpResponseMessage response = await client.GetAsync(fileUrl);
                    response.EnsureSuccessStatusCode();
                    await using Stream contentStream = await response.Content.ReadAsStreamAsync();
                    await using FileStream fileStream = new(fileName, FileMode.Create);
                    await contentStream.CopyToAsync(fileStream);
                }
                else
                {
                    LOG("File not downloaded. This is a mock!");
                }
                LOG("File downloaded successfully!");
            }
            catch (HttpRequestException ex)
            {
                LOG($"Request error: {ex.Message}");
                LOG($"Something went wrong: {ex.Message}");
            }
            int x = 5;
        }

        public static void LOG(object message)
        {
            Console.WriteLine(message);
        }
    }
}
