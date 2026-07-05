using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IDAS_Save_System
{
    public sealed class UpdateInfo
    {
        public Version Version { get; init; }
        public string TagName { get; init; }
        public string AssetName { get; init; }
        public string DownloadUrl { get; init; }
    }

    public static class UpdateChecker
    {
        private const string RepoApiUrl = "https://api.github.com/repos/mhytee/idas_save_manager/releases/latest";
        public const string ReleasesPageUrl = "https://github.com/mhytee/idas_save_manager/releases/latest";

        // Version comes from <Version> in the csproj (stamped by CI from the git tag).
        public static Version CurrentVersion =>
            Normalize(Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0, 0));

        private static string TempWorkDir => Path.Combine(Path.GetTempPath(), "IDAS_Update");

        private static string LogFile => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "IDAS_Save_Manager", "update_log.txt");

        /// <summary>
        /// Checks GitHub for a newer release. Never throws and never shows UI —
        /// safe to fire in the background at startup. Returns null when up to date
        /// or on any failure (offline, rate-limited, unparseable tag, no zip asset).
        /// </summary>
        public static async Task<UpdateInfo> CheckForUpdateAsync()
        {
            try
            {
                using HttpClient client = NewClient(TimeSpan.FromSeconds(10));
                string response = await client.GetStringAsync(RepoApiUrl);
                using JsonDocument doc = JsonDocument.Parse(response);

                string tag = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
                if (!TryParseVersion(tag, out Version remote))
                {
                    Log($"Update check skipped: could not parse release tag '{tag}'.");
                    return null;
                }

                if (remote <= CurrentVersion)
                    return null;

                // Pick the .zip asset deliberately; a release may also carry
                // checksums, readmes, or nothing at all.
                foreach (JsonElement asset in doc.RootElement.GetProperty("assets").EnumerateArray())
                {
                    string name = asset.GetProperty("name").GetString() ?? "";
                    if (!name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        continue;

                    return new UpdateInfo
                    {
                        Version = remote,
                        TagName = tag,
                        AssetName = name,
                        DownloadUrl = asset.GetProperty("browser_download_url").GetString()
                    };
                }

                Log($"Update check: release {tag} has no .zip asset.");
                return null;
            }
            catch (Exception ex)
            {
                Log("Update check failed", ex);
                return null;
            }
        }

        /// <summary>
        /// Downloads and unpacks the update, returning the path to the new exe
        /// (still in the temp folder — nothing is installed yet). Shows its own
        /// error dialog and returns null on failure. Call from the UI thread.
        /// </summary>
        public static async Task<string> DownloadUpdateAsync(UpdateInfo update, IProgress<string> progress, IWin32Window owner)
        {
            try
            {
                if (Directory.Exists(TempWorkDir))
                    Directory.Delete(TempWorkDir, true);
                Directory.CreateDirectory(TempWorkDir);

                string zipPath = Path.Combine(TempWorkDir, update.AssetName);

                // Overall time is unlimited (the zip is ~70 MB and connection speeds
                // vary wildly), but 60 seconds with no data at all means the
                // connection is dead — the stall guard is re-armed after every read.
                using (HttpClient client = NewClient(Timeout.InfiniteTimeSpan))
                using (var stallGuard = new CancellationTokenSource(TimeSpan.FromSeconds(60)))
                using (HttpResponseMessage response = await client.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, stallGuard.Token))
                {
                    response.EnsureSuccessStatusCode();
                    long? totalBytes = response.Content.Headers.ContentLength;

                    await using Stream source = await response.Content.ReadAsStreamAsync(stallGuard.Token);
                    await using var target = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    byte[] buffer = new byte[81920];
                    long done = 0;
                    int read;
                    while ((read = await source.ReadAsync(buffer, stallGuard.Token)) > 0)
                    {
                        stallGuard.CancelAfter(TimeSpan.FromSeconds(60));
                        await target.WriteAsync(buffer.AsMemory(0, read), stallGuard.Token);
                        done += read;
                        // Kept short: the reporting button sits in a narrow top-right corner.
                        progress?.Report(totalBytes > 0
                            ? $"{done * 100 / totalBytes.Value}%"
                            : $"{done / (1024 * 1024)} MB");
                    }
                }

                // A captive portal or interception proxy can return HTML with status 200.
                if (!IsZipFile(zipPath))
                    throw new InvalidOperationException(
                        "The downloaded file is not a valid update package. A network sign-in page may have interfered; please try again.");

                progress?.Report("Unpacking…");
                string extractDir = Path.Combine(TempWorkDir, "extracted");
                await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, extractDir));

                // Release zips have shipped both flat and inside a wrapper folder —
                // search the whole tree. Largest exe wins in case a helper tool tags along.
                string newExe = Directory.GetFiles(extractDir, "*.exe", SearchOption.AllDirectories)
                    .OrderByDescending(f => new FileInfo(f).Length)
                    .FirstOrDefault();
                if (newExe == null)
                    throw new InvalidOperationException("No .exe was found inside the downloaded zip.");

                return newExe;
            }
            catch (OperationCanceledException)
            {
                Log($"Download of {update.TagName} stalled.");
                MessageBox.Show(owner,
                    "The download stalled (no data arrived for 60 seconds).\nCheck your connection and try again.",
                    "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            catch (Exception ex)
            {
                Log($"Download of {update.TagName} failed", ex);
                MessageBox.Show(owner,
                    $"The update could not be downloaded:\n{ex.Message}\n\n" +
                    $"You can download it manually from:\n{ReleasesPageUrl}",
                    "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        /// <summary>
        /// Swaps the running exe for the downloaded one and starts the new version.
        /// Shows its own error dialogs. Returns true when the swap is committed —
        /// even if the relaunch itself fails, the update is installed at that point
        /// and the caller should close the application.
        /// </summary>
        public static bool InstallAndRestart(string newExe, IWin32Window owner)
        {
            string currentExe = Environment.ProcessPath;
            if (currentExe == null)
            {
                MessageBox.Show(owner, "Could not determine the running exe's path.",
                    "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            string stagedExe = currentExe + ".new";
            string parkedExe = currentExe + ".old";

            try
            {
                // Copy into the app folder first: proves the folder is writable
                // before the running exe is touched at all.
                File.Copy(newExe, stagedExe, true);

                // Windows allows renaming a running exe, so swap via same-folder
                // renames: park the current one, promote the staged one.
                if (File.Exists(parkedExe))
                    File.Delete(parkedExe);
                File.Move(currentExe, parkedExe);
                try
                {
                    File.Move(stagedExe, currentExe, true);
                }
                catch (Exception promoteError)
                {
                    try
                    {
                        // Copy, not move: overwrite-tolerant, and .old stays behind
                        // as a second safety net until the next startup cleanup.
                        File.Copy(parkedExe, currentExe, true);
                    }
                    catch (Exception rollbackError)
                    {
                        // Worst case: nothing left at the canonical path. Tell the
                        // user exactly how to recover, because the app can't.
                        Log("Rollback after failed promote also failed", rollbackError);
                        Log("Original promote error", promoteError);
                        MessageBox.Show(owner,
                            "The update failed and the app could not restore itself.\n\n" +
                            "To fix it manually: in the app's folder, rename\n" +
                            $"\"{Path.GetFileName(parkedExe)}\"  back to  \"{Path.GetFileName(currentExe)}\"\n\n" +
                            $"Original error: {promoteError.Message}",
                            "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                    throw; // rethrow the promote error — the rollback restored the app
                }
            }
            catch (Exception ex)
            {
                Log($"Update install failed", ex);
                MessageBox.Show(owner,
                    $"The update could not be installed:\n{ex.Message}\n\n" +
                    $"You can download it manually from:\n{ReleasesPageUrl}",
                    "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // The swap is committed — from here on the update has succeeded even
            // if the relaunch doesn't, so never report failure past this point.
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = currentExe,
                    WorkingDirectory = Path.GetDirectoryName(currentExe),
                    UseShellExecute = true
                });
                Log($"Updated and restarted.");
            }
            catch (Exception ex)
            {
                Log("Relaunch after successful update failed", ex);
                MessageBox.Show(owner,
                    "The update was installed, but the app could not restart itself.\nPlease reopen it manually.",
                    "Update Installed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            return true;
        }

        /// <summary>
        /// Removes leftovers from a completed update (the parked .old exe, temp files)
        /// and debris from the old bat-based updater. Never throws — fire and forget
        /// from a background thread at startup.
        /// </summary>
        public static void CleanupAfterUpdate()
        {
            string exe = Environment.ProcessPath;
            if (exe != null)
            {
                // The replaced exe may still be shutting down; retry briefly.
                TryDeleteWithRetry(exe + ".old");
                TryDelete(() => File.Delete(exe + ".new"));
            }

            TryDelete(() => Directory.Delete(TempWorkDir, true));

            // Debris from the pre-1.0.2 updater: extraction folder, bat script,
            // and any downloaded release zip sitting loose in the temp root.
            TryDelete(() => Directory.Delete(Path.Combine(Path.GetTempPath(), "IDAS_Extracted_Update"), true));
            TryDelete(() => File.Delete(Path.Combine(Path.GetTempPath(), "IDAS_Update.bat")));
            TryDelete(() =>
            {
                foreach (string zip in Directory.GetFiles(Path.GetTempPath(), "IDAS_Save_Manager*.zip"))
                    TryDelete(() => File.Delete(zip));
            });
        }

        private static void TryDeleteWithRetry(string path)
        {
            for (int i = 0; i < 5 && File.Exists(path); i++)
            {
                try
                {
                    File.Delete(path);
                    return;
                }
                catch (IOException) { Thread.Sleep(300); }
                catch (UnauthorizedAccessException) { Thread.Sleep(300); }
            }
        }

        private static void TryDelete(Action delete)
        {
            try { delete(); }
            catch { }
        }

        private static bool IsZipFile(string path)
        {
            try
            {
                using FileStream fs = File.OpenRead(path);
                return fs.ReadByte() == 'P' && fs.ReadByte() == 'K';
            }
            catch
            {
                return false;
            }
        }

        private static HttpClient NewClient(TimeSpan timeout)
        {
            var client = new HttpClient { Timeout = timeout };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("IDAS_Save_Manager");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
            return client;
        }

        private static bool TryParseVersion(string tag, out Version version)
        {
            version = null;
            string s = (tag ?? "").Trim().TrimStart('v', 'V');

            // Tolerate suffixes like "1.0.2-beta" or "1.0.2 hotfix".
            int cut = s.IndexOfAny(new[] { '-', '+', ' ' });
            if (cut >= 0)
                s = s.Substring(0, cut);

            if (!s.Contains('.'))
                s += ".0"; // Version.TryParse rejects a bare major like "2"

            if (!Version.TryParse(s, out Version parsed))
                return false;

            version = Normalize(parsed);
            return true;
        }

        // Pad to four components so "1.0.2" and "1.0.2.0" compare as equal.
        private static Version Normalize(Version v) =>
            new Version(v.Major, Math.Max(v.Minor, 0), Math.Max(v.Build, 0), Math.Max(v.Revision, 0));

        private static void Log(string message, Exception ex = null)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogFile));
                if (File.Exists(LogFile) && new FileInfo(LogFile).Length > 128 * 1024)
                    File.Delete(LogFile);
                File.AppendAllText(LogFile,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{(ex == null ? "" : ": " + ex)}\r\n");
            }
            catch
            {
                // Logging must never take the app down.
            }
        }
    }
}
