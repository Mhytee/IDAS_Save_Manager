using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IDAS_Save_System
{
    public partial class Form2 : Form
    {
        bool suppressSaveSelectionEvents = false;
        string lastSelectedSaveName = "";
        string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IDAS_Save_Manager");
        string backupPath => Path.Combine(appDataPath, "IDAS_Backups");
        string configFile => Path.Combine(appDataPath, "config.txt");
        string savePathRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TeknoParrot");
        string profileFile = "";
        string teknoExePath = "";
        string userProfilesPath => string.IsNullOrWhiteSpace(teknoExePath) ? "" : Path.Combine(Path.GetDirectoryName(teknoExePath), "UserProfiles");
        // Full backup filename of the save that was launched ("" for no-save
        // sessions), so the write-back targets exactly the file that was loaded
        // instead of reconstructing the name from a display string.
        string launchedSaveFile = "";
        int launchedGameIndex = -1;
        bool sessionActive = false;
        Process teknoProcess;
        Timer teknoExitTimer;
        Button btnUpdate;
        ToolTip updateToolTip = new ToolTip();
        UpdateInfo pendingUpdate;
        bool updateInProgress = false;
        string downloadedUpdateExe = null;

        private readonly (string profile, string displayName, string saveFileName)[] idGames = new[]
        {
            ("ID6.xml", "Initial D 6 Double Aces", "SBUU_card.bin"),
            ("ID7.xml", "Initial D 7 AAX", "SBYD_card.bin"),
            ("ID8.xml", "Initial D 8 Infinity", "SBZZ_card.bin")
        };

        private PictureBox selectedPicture = null;
        private int selectedGameIndex = -1;

        private string GetUniqueSavePath(string baseName, string prefix)
        {
            string cleanFileName = $"{prefix}_{baseName}.bin";
            string path = Path.Combine(backupPath, cleanFileName);

            if (!File.Exists(path))
                return path;

            // Conflict detected — add readable timestamp
            string timestamp = DateTime.Now.ToString("MMMM d, h-mm tt").Replace(":", "-");
            string nameWithTime = $"{baseName} ({timestamp})";
            string withTimestamp = Path.Combine(backupPath, $"{prefix}_{nameWithTime}.bin");

            if (!File.Exists(withTimestamp))
                return withTimestamp;

            // If even that exists, fall back to counter
            int counter = 2;
            string numberedPath;
            do
            {
                numberedPath = Path.Combine(backupPath, $"{prefix}_{nameWithTime} ({counter}).bin");
                counter++;
            } while (File.Exists(numberedPath));

            return numberedPath;
        }


        public Form2()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            // Never allow the window smaller than its designed layout — below that
            // the bottom rows just clip, and the update banner would overlap them.
            this.MinimumSize = new Size(LogicalToDeviceUnits(479), this.Height);
            this.Text = $"IDAS Save Manager v{UpdateChecker.CurrentVersion.ToString(3)}";

            btnUpdate = new Button
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.Gold,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Visible = false
            };
            btnUpdate.Click += btnUpdate_Click;
            this.Controls.Add(btnUpdate);

            this.FormClosing += Form2_FormClosing;
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Never veto an OS-initiated close: the session save is recovered by
            // the stray-card import on next startup anyway.
            if (!sessionActive ||
                e.CloseReason == CloseReason.WindowsShutDown ||
                e.CloseReason == CloseReason.TaskManagerClosing)
                return;

            var result = MessageBox.Show(this,
                "A game session is still running.\n\n" +
                "If you close the manager now, this session's save will be imported " +
                "the next time you open it (after the game closes).\n\nClose anyway?",
                "Game Session Running", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
                e.Cancel = true;
        }

        private void Form2_Load(object sender, EventArgs e)
{
    Directory.CreateDirectory(appDataPath);
    Directory.CreateDirectory(backupPath);
    LoadOrPromptTeknoParrotPath();

    // Never import while TeknoParrot is running: the card in its folder may be
    // in use by a live game, and grabbing it would hand the player a stale save.
    if (IsTeknoParrotRunning())
        MessageBox.Show(this,
            "TeknoParrot is currently running.\nAny existing save will be imported the next time you start the manager after it closes.",
            "TeknoParrot Running", MessageBoxButtons.OK, MessageBoxIcon.Information);
    else
        HandleFirstRunBackup();

    chkAutoNameOnly.CheckedChanged += (s, e) => SaveConfig();
    comboSaves.SelectedIndexChanged += comboSaves_SelectedIndexChanged;

    picID6.Click += Pic_Click;
    picID7.Click += Pic_Click;
    picID8.Click += Pic_Click;

    textBox1.GotFocus += (s, e) => this.ActiveControl = null;
    textBox2.GotFocus += (s, e) => this.ActiveControl = null;
    textBox3.GotFocus += (s, e) => this.ActiveControl = null;

    this.StartPosition = FormStartPosition.Manual;
    this.Location = new Point(
        Screen.PrimaryScreen.Bounds.X + (Screen.PrimaryScreen.Bounds.Width - this.Width) / 2,
        Screen.PrimaryScreen.Bounds.Y + (Screen.PrimaryScreen.Bounds.Height - this.Height) / 2
    );

    suppressSaveSelectionEvents = true;

    if (selectedGameIndex >= 0)
    {
        PictureBox[] pics = { picID6, picID7, picID8 };
        if (selectedGameIndex < pics.Length)
            Pic_Click(pics[selectedGameIndex], EventArgs.Empty);
    }

    LoadBackupList();

    if (!string.IsNullOrWhiteSpace(lastSelectedSaveName))
    {
        foreach (var item in comboSaves.Items)
        {
            if ((item as ComboBoxItem)?.FullFilename == lastSelectedSaveName)
            {
                comboSaves.SelectedItem = item;
                break;
            }
        }
    }

    suppressSaveSelectionEvents = false;
    UpdateLaunchButtons();

    _ = Task.Run(() =>
    {
        UpdateChecker.CleanupAfterUpdate();
        PurgeOldTrash();
    });
    CheckForUpdatesInBackground();
}

        private async void CheckForUpdatesInBackground()
        {
            UpdateInfo update = await UpdateChecker.CheckForUpdateAsync();
            if (update == null || IsDisposed)
                return;

            pendingUpdate = update;
            updateToolTip.SetToolTip(btnUpdate, "A new version is available. Click to download and install.");
            SetUpdateButtonText($"Update v{update.Version.ToString(3)}");
            btnUpdate.Visible = true;
            btnUpdate.BringToFront();
        }

        // The button lives in the top-right corner, opposite the DevName credit,
        // so it stays compact: it is sized to its text and kept right-aligned.
        private void SetUpdateButtonText(string text)
        {
            btnUpdate.Text = text;
            Size textSize = TextRenderer.MeasureText(text, btnUpdate.Font);
            btnUpdate.Size = new Size(textSize.Width + LogicalToDeviceUnits(16), LogicalToDeviceUnits(26));
            btnUpdate.Location = new Point(
                ClientSize.Width - btnUpdate.Width - LogicalToDeviceUnits(12),
                LogicalToDeviceUnits(25));
        }

        private async void btnUpdate_Click(object sender, EventArgs e)
        {
            if (pendingUpdate == null || updateInProgress)
                return;

            if (sessionActive)
            {
                MessageBox.Show(this,
                    "A game session is still running. Finish the session first, then update.",
                    "Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            updateInProgress = true;
            UpdateLaunchButtons();
            btnUpdate.Enabled = false;
            string readyText = btnUpdate.Text;

            if (downloadedUpdateExe == null || !File.Exists(downloadedUpdateExe))
            {
                var progress = new Progress<string>(SetUpdateButtonText);
                downloadedUpdateExe = await UpdateChecker.DownloadUpdateAsync(pendingUpdate, progress, this);
                if (downloadedUpdateExe == null)
                {
                    updateInProgress = false;
                    UpdateLaunchButtons();
                    btnUpdate.Enabled = true;
                    SetUpdateButtonText(readyText);
                    return;
                }
            }

            // Belt and braces: never swap the exe while a session is active —
            // the restart would kill the watchdog that backs up the session's save.
            if (sessionActive)
            {
                updateInProgress = false;
                UpdateLaunchButtons();
                btnUpdate.Enabled = true;
                updateToolTip.SetToolTip(btnUpdate, "Update downloaded. Click to install once your game session has ended.");
                SetUpdateButtonText("Install update");
                return;
            }

            if (UpdateChecker.InstallAndRestart(downloadedUpdateExe, this))
            {
                Application.Exit(); // the new version is already starting
                return;
            }

            updateInProgress = false;
            UpdateLaunchButtons();
            btnUpdate.Enabled = true;
            SetUpdateButtonText(readyText);
        }



        private void UpdateSetPathButton()
        {
            bool pathOk = !string.IsNullOrWhiteSpace(teknoExePath) && File.Exists(teknoExePath);
            btnSetPath.Text = pathOk
                ? "Click to Change TeknoParrot Path ✅"
                : "Click to Set TeknoParrot Path (Not Set)";
            btnSetPath.BackColor = pathOk ? Color.LightGreen : Color.LightCoral;
        }
        // The only config writer: always writes every setting, atomically
        // (write to .tmp, then swap) so a crash mid-write can't truncate it.
        private void SaveConfig()
        {
            try
            {
                Directory.CreateDirectory(appDataPath);
                string tmp = configFile + ".tmp";
                File.WriteAllLines(tmp, new[]
                {
                    "Path to TeknoParrotUI.exe:",
                    teknoExePath,
                    $"SelectedGameIndex:{selectedGameIndex}",
                    $"SelectedSaveName:{lastSelectedSaveName}",
                    $"AutoNameEnabled={chkAutoNameOnly.Checked.ToString().ToLower()}"
                });
                File.Move(tmp, configFile, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Settings could not be saved ({ex.Message}).",
                    "Settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LoadBackupList()
        {
            comboSaves.Items.Clear();
            if (selectedGameIndex < 0) return;

            string prefix = GetSelectedGamePrefix();
            var files = Directory.GetFiles(backupPath, prefix + "_*.bin");
            foreach (var file in files)
            {
                string fullFile = Path.GetFileName(file);
                // Strip only the leading prefix: a save named "ID6_Bob" must not
                // have the token removed from the middle of its display name.
                string displayName = fullFile.StartsWith(prefix + "_") ? fullFile.Substring(prefix.Length + 1) : fullFile;
                comboSaves.Items.Add(new ComboBoxItem(displayName, fullFile));
            }

            if (comboSaves.Items.Count > 0)
                comboSaves.SelectedIndex = 0;

            UpdateLaunchButtons();
        }

        private void comboSaves_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressSaveSelectionEvents) return;

            UpdateLaunchButtons();

            var selected = comboSaves.SelectedItem as ComboBoxItem;
            if (selected != null)
            {
                lastSelectedSaveName = selected.FullFilename;
                SaveConfig();
            }
        }



        private void Pic_Click(object sender, EventArgs e)
        {
            if (selectedPicture != null)
            {
                selectedPicture.BackColor = SystemColors.Control;
                selectedPicture.Padding = new Padding(0);
            }

            selectedPicture = (PictureBox)sender;
            selectedPicture.Padding = new Padding(2);
            selectedPicture.BackColor = Color.LimeGreen;

            if (selectedPicture == picID6) selectedGameIndex = 0;
            else if (selectedPicture == picID7) selectedGameIndex = 1;
            else if (selectedPicture == picID8) selectedGameIndex = 2;
            else selectedGameIndex = -1;

            UpdateLaunchButtons();
            LoadBackupList();
            // Only auto-select the first save if no lastSelectedSaveName is set
            if (string.IsNullOrWhiteSpace(lastSelectedSaveName) && comboSaves.Items.Count > 0)
            {
                comboSaves.SelectedItem = comboSaves.Items[0];
            }
            SaveConfig();

        }

        private void UpdateLaunchButtons()
        {
            // Launching is blocked while a session runs (the card file is in use)
            // and while an update download runs (a completed update restarts the
            // app, which would kill the watchdog that backs up the session save).
            bool blocked = sessionActive || updateInProgress;
            btnLaunch.Enabled = !blocked && comboSaves.SelectedItem != null && selectedGameIndex >= 0;
            btnNoSave.Enabled = !blocked && selectedGameIndex >= 0;
        }

        private void SetSessionUiState(bool active)
        {
            // Rename/Duplicate/Delete are blocked mid-session: the watchdog will
            // write the session's save back under the name it was launched with.
            btnRename.Enabled = !active;
            btnDelete.Enabled = !active;
            btnDuplicate.Enabled = !active;
            btnLaunch.Text = active ? "Game Running…" : "Launch Game!";
            UpdateLaunchButtons();
        }

        private bool IsTeknoParrotRunning()
        {
            Process[] procs = Process.GetProcessesByName("TeknoParrotUi");
            bool running = procs.Length > 0;
            foreach (Process p in procs)
                p.Dispose();
            return running;
        }

        private string GetSelectedGamePrefix() => selectedGameIndex >= 0 ? idGames[selectedGameIndex].profile.Replace(".xml", "") : "Unknown";
        private string GetSelectedProfileFile() => selectedGameIndex >= 0 ? idGames[selectedGameIndex].profile : "";
        private string GetSelectedSaveFileName() => selectedGameIndex >= 0 ? idGames[selectedGameIndex].saveFileName : "";

        private void HandleFirstRunBackup()
        {
            for (int i = 0; i < idGames.Length; i++)
                ImportStrayCard(i);
        }

        // Moves a card sitting in TeknoParrot's folder into the backup list
        // (named via the prompt flow). Used at startup for all games and before
        // launching, so an unknown save is never overwritten or deleted.
        // Returns false if a card exists but could not be imported.
        private bool ImportStrayCard(int gameIndex, bool selectImported = true)
        {
            var (profile, displayName, saveFileName) = idGames[gameIndex];
            string prefix = profile.Replace(".xml", "");
            string saveFilePath = Path.Combine(savePathRoot, saveFileName);

            if (!File.Exists(saveFilePath))
                return true;

            try
            {
                string extracted = SaveFileHelper.TryExtractPlayerName(saveFilePath);
                string name = extracted ?? DateTime.Now.ToString("yyyyMMdd_HHmmss");

                bool skipPrompt = chkAutoNameOnly.Checked &&
                                  !string.IsNullOrWhiteSpace(name) &&
                                  !ContainsInvalidFileNameChars(name);

                if (!skipPrompt)
                {
                    string promptText = ContainsInvalidFileNameChars(name)
                        ? $"The extracted name for {displayName} contains invalid characters.\nPlease provide a valid name:"
                        : $"A save file was found for {displayName}. Please provide a name for this save:";

                    string input = PromptForValidName(promptText, "Import Save", extracted);

                    if (!string.IsNullOrWhiteSpace(input))
                        name = input;
                }

                string previousSelection = (comboSaves.SelectedItem as ComboBoxItem)?.FullFilename;
                string finalPath = GetUniqueSavePath(name, prefix);
                File.Copy(saveFilePath, finalPath);
                File.Delete(saveFilePath);

                suppressSaveSelectionEvents = true;
                try
                {
                    LoadBackupList();
                    string toSelect = selectImported ? Path.GetFileName(finalPath) : previousSelection;
                    var restore = comboSaves.Items
                        .Cast<ComboBoxItem>()
                        .FirstOrDefault(x => x.FullFilename == toSelect);
                    if (restore != null)
                        comboSaves.SelectedItem = restore; // otherwise keep LoadBackupList's default
                }
                finally
                {
                    suppressSaveSelectionEvents = false;
                }
                string nowSelected = (comboSaves.SelectedItem as ComboBoxItem)?.FullFilename;
                if (!string.IsNullOrEmpty(nowSelected) && nowSelected != lastSelectedSaveName)
                {
                    lastSelectedSaveName = nowSelected;
                    SaveConfig();
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"Could not import the existing {displayName} save ({ex.Message}).\nThe file was left in place.",
                    "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }




        private void LoadOrPromptTeknoParrotPath()
        {
            if (File.Exists(configFile))
            {
                string[] lines = File.ReadAllLines(configFile);
                if (lines.Length >= 2)
                {
                    teknoExePath = lines[1].Trim();
                    foreach (var line in lines.Skip(2))
                    {
                        if (line.StartsWith("SelectedGameIndex:"))
                        {
                            if (int.TryParse(line.Replace("SelectedGameIndex:", ""), out int index))
                                selectedGameIndex = index;
                        }
                        else if (line.StartsWith("SelectedSaveName:"))
                        {
                            lastSelectedSaveName = line.Replace("SelectedSaveName:", "").Trim();
                        }
                        else if (line.StartsWith("AutoNameEnabled=", StringComparison.OrdinalIgnoreCase))
                        {
                            if (chkAutoNameOnly != null)
                                chkAutoNameOnly.Checked = line.Split('=')[1].Trim().ToLower() == "true";
                        }
                    }
                }
            }
            UpdateSetPathButton();
        }


        private void btnSetPath_Click(object sender, EventArgs e) => PromptForTeknoParrotPath();

        private void PromptForTeknoParrotPath()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Select TeknoParrotUi.exe",
                Filter = "Executable (*.exe)|*.exe"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (!Path.GetFileName(dialog.FileName).Equals("TeknoParrotUi.exe", StringComparison.OrdinalIgnoreCase))
                {
                    var proceed = MessageBox.Show(this,
                        "The selected file is not named TeknoParrotUi.exe.\n" +
                        "Session tracking may not work correctly with a different launcher.\n\nUse it anyway?",
                        "Unexpected File", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (proceed != DialogResult.Yes)
                    {
                        UpdateSetPathButton();
                        return;
                    }
                }
                teknoExePath = dialog.FileName;
                SaveConfig();
            }
            else if (string.IsNullOrWhiteSpace(teknoExePath))
            {
                // Cancelling must not erase an already-working path.
                MessageBox.Show("Program will not function without setting the TeknoParrotUI.exe path!", "Path Not Set", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            UpdateSetPathButton();
        }

        private bool ValidateReadyToLaunch()
        {
            if (string.IsNullOrWhiteSpace(teknoExePath) || !File.Exists(teknoExePath))
            {
                MessageBox.Show(this,
                    "TeknoParrotUi.exe was not found. Click the button at the top to set its location.",
                    "TeknoParrot Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                UpdateSetPathButton();
                return false;
            }

            if (IsTeknoParrotRunning())
            {
                MessageBox.Show(this,
                    "TeknoParrot is already running. Close it before launching a game from the manager.",
                    "TeknoParrot Running", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private void btnLaunch_Click(object sender, EventArgs e)
        {
            string selectedSave = (comboSaves.SelectedItem as ComboBoxItem)?.FullFilename;
            if (selectedGameIndex < 0 || string.IsNullOrWhiteSpace(selectedSave) || sessionActive) return;
            if (!ValidateReadyToLaunch()) return;

            // A card already in TeknoParrot's folder is someone's progress
            // (e.g. the manager was closed mid-session): import it, never overwrite.
            if (!ImportStrayCard(selectedGameIndex, selectImported: false))
                return;

            string saveFileName = GetSelectedSaveFileName();
            string sourcePath = Path.Combine(backupPath, selectedSave);
            string targetPath = Path.Combine(savePathRoot, saveFileName);

            try
            {
                Directory.CreateDirectory(savePathRoot);
                File.Copy(sourcePath, targetPath, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"The save could not be loaded ({ex.Message}).",
                    "Launch Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            launchedSaveFile = selectedSave;
            profileFile = GetSelectedProfileFile();
            SaveConfig();

            if (!LaunchGame(profileFile))
            {
                // Un-stage so the card folder stays clean; the backup copy is untouched.
                try { File.Delete(targetPath); } catch { }
                launchedSaveFile = "";
            }
        }

        private void btnNoSave_Click(object sender, EventArgs e)
        {
            if (selectedGameIndex < 0 || sessionActive) return;
            if (!ValidateReadyToLaunch()) return;

            // A card sitting here is someone's progress: import it instead of deleting it.
            if (!ImportStrayCard(selectedGameIndex, selectImported: false))
                return;

            profileFile = GetSelectedProfileFile();
            launchedSaveFile = "";
            SaveConfig();
            LaunchGame(profileFile);
        }

        private bool LaunchGame(string profile)
        {
            try
            {
                var info = new ProcessStartInfo
                {
                    FileName = teknoExePath,
                    Arguments = $"--profile=\"{profile}\" --start",
                    WorkingDirectory = Path.GetDirectoryName(teknoExePath),
                    UseShellExecute = true
                };
                teknoProcess = Process.Start(info);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"TeknoParrot could not be started:\n{ex.Message}",
                    "Launch Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                teknoProcess = null;
                return false;
            }

            launchedGameIndex = selectedGameIndex;
            sessionActive = true;
            SetSessionUiState(true);
            this.WindowState = FormWindowState.Minimized;
            StartTeknoWatchdog();
            return true;
        }

        private void StartTeknoWatchdog()
        {
            teknoExitTimer?.Stop();
            teknoExitTimer?.Dispose();
            teknoExitTimer = new Timer { Interval = 1000 };
            int endedTicks = 0;
            teknoExitTimer.Tick += (s, e) =>
            {
                // Require a few consecutive quiet ticks: launcher stubs and
                // TeknoParrot self-updates can briefly show no process during
                // a hand-off, and one quiet second must not end the session.
                endedTicks = SessionHasEnded() ? endedTicks + 1 : 0;
                if (endedTicks < 3)
                    return;

                teknoExitTimer.Stop();
                teknoExitTimer.Dispose();
                teknoExitTimer = null;
                teknoProcess?.Dispose();
                teknoProcess = null;
                sessionActive = false;
                SetSessionUiState(false);

                // Restore the window first so recovery prompts appear over it.
                this.WindowState = FormWindowState.Normal;
                this.BringToFront();
                this.Activate();

                try
                {
                    RecoverSessionSaves();
                }
                finally
                {
                    launchedSaveFile = "";
                    launchedGameIndex = -1;
                }
            };
            teknoExitTimer.Start();
        }

        private bool SessionHasEnded()
        {
            // Watch the process we actually started, so a renamed or slow-starting
            // exe can't make the first tick look like the session already ended.
            bool processGone = true;
            if (teknoProcess != null)
            {
                try { processGone = teknoProcess.HasExited; }
                catch { processGone = true; } // can't query it: fall back to the name check
            }
            if (!processGone)
                return false;

            // TeknoParrot can hand off to another instance; wait for those too.
            return !IsTeknoParrotRunning();
        }

        private void RecoverSessionSaves()
        {
            string newFilenameToSelect = null;

            for (int i = 0; i < idGames.Length; i++)
            {
                var (profile, displayName, saveFileName) = idGames[i];
                string path = Path.Combine(savePathRoot, saveFileName);
                if (!File.Exists(path))
                    continue;

                string prefix = profile.Replace(".xml", "");
                try
                {
                    // launchedSaveFile belongs to the game we launched; a card for
                    // any other game is unrelated and gets imported as its own save.
                    if (i == launchedGameIndex && !string.IsNullOrWhiteSpace(launchedSaveFile))
                    {
                        string targetPath = Path.Combine(backupPath, launchedSaveFile);
                        string bakPath = targetPath + ".bak";
                        long newSize = new FileInfo(path).Length;
                        long oldSize = File.Exists(targetPath) ? new FileInfo(targetPath).Length : -1;

                        // A crashed emulator can leave an empty or shrunken card;
                        // never let a suspect card replace the only good copy.
                        if (newSize == 0 || (oldSize > 0 && newSize < oldSize))
                        {
                            if (newSize > 0)
                            {
                                string recovered = GetUniqueSavePath(
                                    "Recovered_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"), prefix);
                                CopyWithRetry(path, recovered);
                                MessageBox.Show(this,
                                    $"The save from this session of {displayName} looks damaged (smaller than before), so your previous backup was kept.\n" +
                                    $"The session save was stored as '{Path.GetFileName(recovered)}' just in case.",
                                    "Save Not Updated", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                File.Delete(path);
                            }
                            else
                            {
                                MessageBox.Show(this,
                                    $"The save from this session of {displayName} came back empty, so your previous backup was kept instead.",
                                    "Save Not Updated", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                if (new FileInfo(path).Length == 0)
                                    File.Delete(path); // re-checked: if it grew after all, leave it for import
                            }
                            continue;
                        }

                        if (File.Exists(targetPath))
                        {
                            File.Copy(targetPath, bakPath, true); // previous version survives one session
                            try
                            {
                                CopyWithRetry(path, targetPath);
                            }
                            catch
                            {
                                // Never leave a half-written file as the visible save.
                                try
                                {
                                    File.Copy(bakPath, targetPath, true);
                                }
                                catch
                                {
                                    MessageBox.Show(this,
                                        $"The {displayName} save could not be written back, and restoring the previous version also failed.\n" +
                                        $"An intact copy still exists at:\n{bakPath}",
                                        "Backup Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                                throw; // report the original copy failure
                            }
                        }
                        else
                        {
                            CopyWithRetry(path, targetPath);
                        }
                        newFilenameToSelect = Path.GetFileName(targetPath);
                        DeleteSessionCard(path, displayName);
                    }
                    else
                    {
                        string extracted = SaveFileHelper.TryExtractPlayerName(path);
                        string name = extracted;

                        bool skipPrompt = chkAutoNameOnly.Checked &&
                                          !string.IsNullOrWhiteSpace(name) &&
                                          !ContainsInvalidFileNameChars(name ?? "");

                        if (!skipPrompt)
                        {
                            string promptText = ContainsInvalidFileNameChars(name ?? "")
                                ? "The extracted name contains invalid characters.\nPlease provide a valid name for your save:"
                                : $"Name the save created for {displayName} during your last session:";
                            name = PromptForValidName(promptText, "New Save Detected", extracted);
                        }

                        string rawName = string.IsNullOrWhiteSpace(name)
                            ? "AutoBackup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss")
                            : name;

                        string finalPath = GetUniqueSavePath(rawName, prefix);
                        CopyWithRetry(path, finalPath);
                        newFilenameToSelect = Path.GetFileName(finalPath);
                        DeleteSessionCard(path, displayName);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this,
                        $"Could not back up the {displayName} save ({ex.Message}).\n" +
                        "The file was left in place and will be imported the next time the manager starts.",
                        "Backup Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            LoadBackupList();

            if (!string.IsNullOrEmpty(newFilenameToSelect))
            {
                comboSaves.SelectedItem = comboSaves.Items
                    .Cast<ComboBoxItem>()
                    .FirstOrDefault(x => x.FullFilename == newFilenameToSelect);
                UpdateLaunchButtons();
            }
        }

        // The backup already succeeded when this runs, so a failed card removal
        // must not surface as "backup failed" — it only means a duplicate import
        // at next startup.
        private void DeleteSessionCard(string path, string displayName)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"The {displayName} save was backed up, but the session card could not be removed ({ex.Message}).\n" +
                    "It will be re-imported the next time the manager starts.",
                    "Backup Note", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private static void CopyWithRetry(string source, string destination)
        {
            const int attempts = 3;
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    File.Copy(source, destination, true);
                    return;
                }
                catch (IOException) when (attempt < attempts)
                {
                    // The game may still be releasing the file right after exit.
                    System.Threading.Thread.Sleep(400);
                }
            }
        }

        private void PurgeOldTrash()
        {
            try
            {
                string trashDir = Path.Combine(backupPath, "Trash");
                if (!Directory.Exists(trashDir))
                    return;
                foreach (string file in Directory.GetFiles(trashDir))
                    if (DateTime.Now - File.GetLastWriteTime(file) > TimeSpan.FromDays(30))
                        File.Delete(file);
            }
            catch
            {
                // Trash maintenance must never take the app down.
            }
        }

        private bool ContainsInvalidFileNameChars(string input)
        {
            return input.Any(c => Path.GetInvalidFileNameChars().Contains(c));
        }

        private string PromptForValidName(string message, string title, string defaultValue = "")
        {
            string name;
            do
            {
                name = Prompt.ShowDialog(message, title, defaultValue);

                if (string.IsNullOrWhiteSpace(name))
                    return null;

                if (ContainsInvalidFileNameChars(name))
                {
                    MessageBox.Show("Name contains invalid characters. Please avoid:\n\\ / : * ? \" < > |",
                        "Invalid Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    continue;
                }

                return name;
            } while (true);
        }




        private void btnRename_Click(object sender, EventArgs e)
        {
            var selectedItem = comboSaves.SelectedItem as ComboBoxItem;
            if (selectedItem == null)
                return;

            string oldName = selectedItem.FullFilename;
            string currentName = Path.GetFileNameWithoutExtension(selectedItem.DisplayText);
            string newName = PromptForValidName("Enter new name for the save:", "Rename Save", currentName);
            if (string.IsNullOrWhiteSpace(newName))
                return;

            string prefix = GetSelectedGamePrefix();
            string newFileName = prefix + "_" + newName + ".bin";
            if (newFileName == oldName)
                return; // renamed to itself

            string newPath = Path.Combine(backupPath, newFileName);
            if (File.Exists(newPath))
            {
                MessageBox.Show("A save with that name already exists.");
                return;
            }

            try
            {
                File.Move(Path.Combine(backupPath, oldName), newPath);
                try
                {
                    string oldBak = Path.Combine(backupPath, oldName + ".bak");
                    if (File.Exists(oldBak))
                        File.Move(oldBak, newPath + ".bak", true); // keep the safety copy attached
                }
                catch
                {
                    // Best effort: an orphaned .bak is harmless, and the rename
                    // itself succeeded, so this must not report a failure.
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"The save could not be renamed ({ex.Message}).",
                    "Rename Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                LoadBackupList();
                return;
            }
            LoadBackupList();
            comboSaves.SelectedItem = comboSaves.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.FullFilename == Path.GetFileName(newPath));
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            var selectedItem = comboSaves.SelectedItem as ComboBoxItem;
            if (selectedItem == null)
                return;

            var confirm = MessageBox.Show($"Are you sure you want to delete '{selectedItem.DisplayText}'?", "Confirm Delete", MessageBoxButtons.YesNo);
            if (confirm != DialogResult.Yes)
                return;

            try
            {
                // Deleted saves go to a Trash folder (purged after 30 days at
                // startup) so a misclick is recoverable.
                string trashDir = Path.Combine(backupPath, "Trash");
                Directory.CreateDirectory(trashDir);
                string trashPath = Path.Combine(trashDir,
                    DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + selectedItem.FullFilename);
                File.Move(Path.Combine(backupPath, selectedItem.FullFilename), trashPath);
                // Move preserves the old write time; restamp so the 30-day purge
                // counts from deletion, not from when the save was last played.
                File.SetLastWriteTime(trashPath, DateTime.Now);

                string bakPath = Path.Combine(backupPath, selectedItem.FullFilename + ".bak");
                if (File.Exists(bakPath))
                {
                    File.Move(bakPath, trashPath + ".bak", true);
                    File.SetLastWriteTime(trashPath + ".bak", DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"The save could not be deleted ({ex.Message}).",
                    "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            LoadBackupList();
        }

        private void btnDuplicate_Click(object sender, EventArgs e)
        {
            var selectedItem = comboSaves.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                string original = selectedItem.FullFilename;
                string originalPath = Path.Combine(backupPath, original);
                string prefix = GetSelectedGamePrefix();

                // Strip the leading prefix and extension to get the base name
                string withoutExt = Path.GetFileNameWithoutExtension(original);
                string baseName = withoutExt.StartsWith(prefix + "_") ? withoutExt.Substring(prefix.Length + 1) : withoutExt;

                // Find next available numbered suggestion
                int copyIndex = 2;
                string suggestedName;
                do
                {
                    suggestedName = $"{baseName} ({copyIndex})";
                    copyIndex++;
                }
                while (File.Exists(Path.Combine(backupPath, $"{prefix}_{suggestedName}.bin")));

                // Show pre-filled prompt with safe suggestion
                string newName = PromptForValidName("Enter name for duplicated save:", "Duplicate Save", suggestedName);
                if (!string.IsNullOrWhiteSpace(newName))
                {
                    string newPath = GetUniqueSavePath(newName, prefix);
                    try
                    {
                        File.Copy(originalPath, newPath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, $"The save could not be duplicated ({ex.Message}).",
                            "Duplicate Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    LoadBackupList();
                    comboSaves.SelectedItem = comboSaves.Items
                        .Cast<ComboBoxItem>()
                        .FirstOrDefault(i => i.FullFilename == Path.GetFileName(newPath));
                }
            }
        }


        public class ComboBoxItem
        {
            public string DisplayText { get; }
            public string FullFilename { get; }

            public ComboBoxItem(string displayText, string fullFilename)
            {
                DisplayText = displayText;
                FullFilename = fullFilename;
            }

            public override string ToString() => DisplayText;
        }

        private void DevName_TextChanged(object sender, EventArgs e)
        {

        }

        private void chkAutoNameOnly_CheckedChanged(object sender, EventArgs e)
        {

        }
    }

    public static class Prompt
    {
        public static string ShowDialog(string text, string caption, string defaultValue = "")

        {
            Form prompt = new Form()
            {
                Width = 500,
                Height = 200,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };
            var font = new Font("Segoe UI", 10F, FontStyle.Bold);

            Label lbl = new Label()
            {
                Left = 20,
                Top = 20,
                Width = 440,
                Height = 40,
                Text = text,
                Font = font,
                AutoSize = false,
                TextAlign = ContentAlignment.TopLeft
            };

            TextBox txt = new TextBox()
            { 
                Left = 20, 
                Top = 70, 
                Width = 440,
                Font = font,
                Text = defaultValue 
            };

            Button ok = new Button()
            {
                Text = "OK",
                Left = 380,
                Width = 80,
                Top = 110,
                DialogResult = DialogResult.OK,
                Font = font
            };

            ok.Click += (sender, e) => { prompt.Close(); };

            prompt.Controls.Add(lbl);
            prompt.Controls.Add(txt);
            prompt.Controls.Add(ok);
            prompt.AcceptButton = ok;

            return prompt.ShowDialog() == DialogResult.OK ? txt.Text : "";
        }
    }
}