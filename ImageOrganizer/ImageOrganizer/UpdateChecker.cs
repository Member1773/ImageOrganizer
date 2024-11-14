using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ImageOrganizer
{
    public static class UpdateChecker
    {
        private static readonly string owner = "Member1773"; 
        private static readonly string repo = "ImageOrganizer";       

        public static async Task<string> CheckForUpdatesAsync()
        {
            string apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

            using (var client = new HttpClient())
            {
                // GitHub API requires a User-Agent header
                client.DefaultRequestHeaders.Add("User-Agent", "ImageOrganizerApp");

                try
                {
                    var response = await client.GetAsync(apiUrl);
                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(content);

                    // Get the tag name (assuming it's the version number)
                    string latestVersion = json["tag_name"]?.ToString();

                    return latestVersion;
                }
                catch (HttpRequestException)
                {
                    // Handle web exceptions (e.g., network issues, API errors)
                    return null;
                }
            }
        }
    }
}