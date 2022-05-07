using Newtonsoft.Json.Linq;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

class GithubAPIRequest
{
    /// <summary>
    /// Get GitHub releases of a repository, using the GitHub REST API
    /// </summary>
    /// <param name="owner">Owner of the repo</param>
    /// <param name="repository">Name of the repo</param>
    /// <returns>API response containing all the repository informations</returns>
    public static async Task<JArray> GetRepositoryReleasesAsync(string owner, string repository)
    {
        string webService_URL = $"https://api.github.com/repos/{owner}/{repository}/releases";
        HttpResponseMessage response = await SendRequestAsync(webService_URL);

        JArray jsonItems = new();

        Task task = response.Content.ReadAsStreamAsync().ContinueWith(t =>
        {
            Stream? stream = t.Result;
            StreamReader? reader = new(stream);

            string result = reader.ReadToEnd();
            jsonItems = JArray.Parse(result);
        });

        await task;
        return jsonItems;
    }

    /// <summary>
    /// Send request to the GitHub API
    /// </summary>
    /// <param name="URI">The api.github.com URI</param>
    /// <returns>Response</returns>
    private static async Task<HttpResponseMessage> SendRequestAsync(string URI)
    {
        HttpClient client = new();

        client.DefaultRequestHeaders.Add("ContentType", "application/json");
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.DefaultRequestHeaders.Add("user-agent", "node.js");

        return await client.GetAsync(URI);
    }
}