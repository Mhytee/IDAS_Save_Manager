using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IDAS_Save_System
{
    public static class UpdateChecker
        //THIS DOES NOT DO ANYTHING YET. IT IS STILL A WIP
    {
        public const string CurrentVersion = "1.0.1"; // Update this per release
        private const string RepoApiUrl = "https://api.github.com/repos/mhytee/idas_save_manager/releases/latest";
        private const string LocalExeName = "IDAS_Save_Manager.exe";

        public static async Task CheckAndPerformUpdate()
        {
            try
            {
                using HttpClient client = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(5) // Fast timeout
                };
                client.DefaultRequestHeaders.UserAgent.ParseAdd("IDAS_Save_Manager");

                string response = await client.GetStringAsync(RepoApiUrl);
                using JsonDocument doc = JsonDocument.Parse(response);

                string tag = doc.RootElement.GetProperty("tag_name").GetString();
                var asset = doc.RootElement.GetProperty("assets")[0];

                string downloadUrl = asset.GetProperty("browser_download_url").GetString();
                string assetName = asset.GetProperty("name").GetString();

                if (!IsNewerVersion(tag, CurrentVersion) || !downloadUrl.EndsWith(".zip"))
                    return;

                var result = MessageBox.Show(
                    $"A new version ({tag}) is available. Would you like to download and install it now?",
                    "Update Available",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result != DialogResult.Yes)
                    return;

                // 1. Download the zip
                string zipPath = Path.Combine(Path.GetTempPath(), assetName);
                using var download = await client.GetAsync(downloadUrl);
                using var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write);
                await download.Content.CopyToAsync(fs);

                // 2. Extract zip to temp
                string extractPath = Path.Combine(Path.GetTempPath(), "IDAS_Extracted_Update");
                if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);
                ZipFile.ExtractToDirectory(zipPath, extractPath);

                // 3. Find new EXE
                string newExePath = Path.Combine(extractPath, LocalExeName);
                if (!File.Exists(newExePath))
                {
                    MessageBox.Show("Update failed: executable not found in .zip.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 4. Replace current EXE via .bat script
                string currentExe = Process.GetCurrentProcess().MainModule.FileName;

                string bat = $@"
@echo off
timeout /t 2 >nul
taskkill /pid {Process.GetCurrentProcess().Id} /f
move /y ""{newExePath}"" ""{currentExe}""
start """" ""{currentExe}""
del ""%~f0""
";
                string batPath = Path.Combine(Path.GetTempPath(), "IDAS_Update.bat");
                File.WriteAllText(batPath, bat);

                Process.Start(new ProcessStartInfo
                {
                    FileName = batPath,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });

                Application.Exit();
            }
            catch (Exception ex)
            {
#if DEBUG
                File.WriteAllText(Path.Combine(Path.GetTempPath(), "update_error_log.txt"), ex.ToString());
#endif
                // Silent fail in release mode
            }
        }

        private static bool IsNewerVersion(string remote, string local)
        {
            Version vRemote = Version.Parse(remote.TrimStart('v', 'V'));
            Version vLocal = Version.Parse(local.TrimStart('v', 'V'));
            return vRemote > vLocal;
        }
    }
}
