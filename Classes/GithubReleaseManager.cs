using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

public abstract class GithubReleaseManager
{
    private readonly string user;
    private readonly string repo;

    public GithubReleaseManager(string user, string repo)
    {
        this.user = user;
        this.repo = repo;
    }

    public long Size { get; set; }
    public string DownloadURI { get; set; } = string.Empty;
    public string ReleaseName { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string UdpatedAt { get; set; } = string.Empty;

    public async Task InitLatestReleaseAsync()
    {
        JArray releases = await GithubAPIRequest.GetRepositoryReleasesAsync(user, repo);

        // releases[0] = latest release
        DownloadURI = releases[0]["assets"]![0]!["browser_download_url"]!.ToString();
        Size = long.Parse(releases[0]["assets"]![0]!["size"]!.ToString());
        ReleaseName = releases[0]["assets"]![0]!["name"]!.ToString();
        CreatedAt = releases[0]["assets"]![0]!["created_at"]!.ToString();
        UdpatedAt = releases[0]["assets"]![0]!["updated_at"]!.ToString();
    }

    /// <summary>
    /// Download the file according to initialized parameters
    /// </summary>
    /// <param name="folderPath">Directory path where to download the file</param>
    /// <param name="progressPercent">Percentage of downloaded stream data</param>
    /// <returns>Directory path where file has been downloaded</returns>
    public async Task<string> DownloadFileAsync(string folderPath, Action<long>? progressPercent = null)
    {
        if (DownloadURI.Equals(""))
            await InitLatestReleaseAsync();   // Init the latest release download link first

        string destination = folderPath + @"\" + ReleaseName;

        using (var client = new System.Net.Http.HttpClient())
        {
            Uri uri = new(DownloadURI);

            Stream sourceStream = await client.GetStreamAsync(uri);
            FileStream fileStream = new(destination, FileMode.CreateNew);

            var progress = new Action<long>(value =>
            {
                progressPercent(value * 100 / Size);
            });

            await CopyToWithProgressAsync(sourceStream, fileStream, 4016, progress);

            sourceStream.Close();
            fileStream.Close();
        }

        if (Path.GetExtension(ReleaseName).Equals(".zip"))
        {
            ZipFile.ExtractToDirectory(destination, folderPath + @"\" + Path.GetFileNameWithoutExtension(ReleaseName));
            File.Delete(destination);
        }

        return Path.GetFullPath(folderPath + @"\" + Path.GetFileNameWithoutExtension(ReleaseName) + @"\");
    }

    /// <summary>
    /// Copy stream to another, while getting the progress<br/>
    /// https://stackoverflow.com/a/36994135
    /// </summary>
    /// <param name="source">Source stream</param>
    /// <param name="destination">Destination stream</param>
    /// <param name="bufferSize">Buffer size</param>
    /// <param name="progress">Aynchronous stream copy progression</param>
    /// <returns></returns>
    public async Task CopyToWithProgressAsync(Stream source, Stream destination, int bufferSize = 4096, Action<long>? progress = null)
    {
        var buffer = new byte[bufferSize];
        var total = 0L;
        int amtRead;
        do
        {
            amtRead = 0;
            while (amtRead < bufferSize)
            {
                var numBytes = await source.ReadAsync(buffer, amtRead, bufferSize - amtRead);
                if (numBytes == 0)
                {
                    break;
                }
                amtRead += numBytes;
            }
            total += amtRead;
            await destination.WriteAsync(buffer, 0, amtRead);
            if (progress != null)
            {
                progress(total);
            }
        } while (amtRead == bufferSize);
    }
}
