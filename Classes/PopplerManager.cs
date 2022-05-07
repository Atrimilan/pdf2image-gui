using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class PopplerManager : GithubReleaseManager
{
    public string BinPath { get; set; } = string.Empty; // .\Release\Poppler (single folder)\Library\bin

    public PopplerManager(string user, string repo) : base(user, repo)
    {
    }

    /// <summary>
    /// Download Poppler according to initialized parameters.
    /// </summary>
    /// <param name="folderPath">Directory path where to download the file</param>
    /// <param name="progressPercent">Percentage of downloaded stream data</param>
    /// <returns>Full Poppler bin directory path</returns>
    public async new Task<string> DownloadFileAsync(string folderPath, Action<long>? progressPercent = null)
    {
        await base.DownloadFileAsync(folderPath, progressPercent);

        string root = Path.GetFullPath(folderPath + @"\" + Path.GetFileNameWithoutExtension(ReleaseName) + @"\");
        BinPath = Directory.GetDirectories(root).FirstOrDefault() + @"\Library\bin";

        return BinPath;
    }

    public async Task<bool> IsPopplerInstalled(string popplerDir)
    {
        if (DownloadURI.Equals(""))
            await InitLatestReleaseAsync();

        if (Directory.GetDirectories(popplerDir).Length > 0)
        {
            string root = Directory.GetDirectories(popplerDir)[^1]; // [^1] : last directory
            BinPath = Directory.GetDirectories(root).FirstOrDefault() + @"\Library\bin";

            return true;
        }
        return false;
    }

    public async Task<bool> IsLatestPopplerInstalled(string popplerDir)
    {
        if (DownloadURI.Equals(""))
            await InitLatestReleaseAsync();

        foreach (var subdir in Directory.GetDirectories(popplerDir))
        {
            if (Path.GetFileNameWithoutExtension(ReleaseName).Equals(new DirectoryInfo(subdir).Name))
            {
                string root = Path.GetFullPath(popplerDir + @"\" + new DirectoryInfo(subdir).Name);
                BinPath = Directory.GetDirectories(root).FirstOrDefault() + @"\Library\bin";

                return true;
            }
        }
        return false;
    }
}