using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
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
        string currentSaveName = "";
        bool pendingSaveNeedsName = false;
        Timer teknoExitTimer;

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
            this.MinimumSize = new Size(479, 501);
        }

        private async void Form2_Load(object sender, EventArgs e)
{
    // ✅ Check for updates before doing anything else
    await UpdateChecker.CheckAndPerformUpdate();

    Directory.CreateDirectory(appDataPath);
    Directory.CreateDirectory(backupPath);
    LoadOrPromptTeknoParrotPath();
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
}



        private void UpdateSetPathButton()
        {
            btnSetPath.Text = string.IsNullOrWhiteSpace(teknoExePath)
                ? "Click to Set TeknoParrot Path (Not Set)"
                : "Click to Change TeknoParrot Path ✅";

            btnSetPath.BackColor = string.IsNullOrWhiteSpace(teknoExePath)
                ? Color.LightCoral
                : Color.LightGreen;
        }
        private void SaveConfig()
        {
            Directory.CreateDirectory(appDataPath);
            File.WriteAllLines(configFile, new[]
            {
        "Path to TeknoParrotUI.exe:",
        teknoExePath,
        $"SelectedGameIndex:{selectedGameIndex}",
        $"SelectedSaveName:{lastSelectedSaveName}",
        $"AutoNameEnabled={chkAutoNameOnly.Checked.ToString().ToLower()}"
    });
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
                string displayName = fullFile.Replace(prefix + "_", "");
                comboSaves.Items.Add(new ComboBoxItem(displayName, fullFile));
            }

            if (comboSaves.Items.Count > 0)
                comboSaves.SelectedIndex = 0;

            UpdateLaunchButtons();
        }

        private void SavePreferences()
        {
            var lines = new[]
            {
                "Path to TeknoParrotUI.exe:",
                teknoExePath,
                $"SelectedGameIndex:{selectedGameIndex}",
                $"SelectedSaveName:{(comboSaves.SelectedItem as ComboBoxItem)?.FullFilename}"
            };
            File.WriteAllLines(configFile, lines);
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
            btnLaunch.Enabled = comboSaves.SelectedItem != null && selectedGameIndex >= 0;
            btnNoSave.Enabled = selectedGameIndex >= 0;
        }

        private string GetSelectedGamePrefix() => selectedGameIndex >= 0 ? idGames[selectedGameIndex].profile.Replace(".xml", "") : "Unknown";
        private string GetSelectedProfileFile() => selectedGameIndex >= 0 ? idGames[selectedGameIndex].profile : "";
        private string GetSelectedSaveFileName() => selectedGameIndex >= 0 ? idGames[selectedGameIndex].saveFileName : "";

        private void HandleFirstRunBackup()
        {
            foreach (var (profile, displayName, saveFileName) in idGames)
            {
                string prefix = profile.Replace(".xml", "");
                string saveFilePath = Path.Combine(savePathRoot, saveFileName);

                if (File.Exists(saveFilePath))
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

                    string finalPath = GetUniqueSavePath(name, prefix);
                    File.Copy(saveFilePath, finalPath);
                    LoadBackupList();
                    comboSaves.SelectedItem = comboSaves.Items
                        .Cast<ComboBoxItem>()
                        .FirstOrDefault(i => i.FullFilename == Path.GetFileName(finalPath));
                    File.Delete(saveFilePath);
                }
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
                teknoExePath = dialog.FileName;
                Directory.CreateDirectory(appDataPath);
                File.WriteAllText(configFile, $"Path to TeknoParrotUI.exe:\n{teknoExePath}");
            }
            else
            {
                MessageBox.Show("Program will not function without setting the TeknoParrotUI.exe path!", "Path Not Set", MessageBoxButtons.OK, MessageBoxIcon.Information);
                teknoExePath = "";
            }
            UpdateSetPathButton();
        }

        private void btnLaunch_Click(object sender, EventArgs e)
        {
            string selectedSave = (comboSaves.SelectedItem as ComboBoxItem)?.FullFilename;
            if (selectedGameIndex < 0 || string.IsNullOrWhiteSpace(selectedSave)) return;

            string prefix = GetSelectedGamePrefix();
            string saveFileName = GetSelectedSaveFileName();

            currentSaveName = Path.GetFileNameWithoutExtension(selectedSave).Replace(prefix + "_", "");
            string sourcePath = Path.Combine(backupPath, selectedSave);
            string targetPath = Path.Combine(savePathRoot, saveFileName);
            File.Copy(sourcePath, targetPath, true);

            profileFile = GetSelectedProfileFile();
            SavePreferences();
            LaunchGame(profileFile);
        }

        private void btnNoSave_Click(object sender, EventArgs e)
        {
            if (selectedGameIndex < 0) return;
            string saveFileName = GetSelectedSaveFileName();
            string targetPath = Path.Combine(savePathRoot, saveFileName);
            if (File.Exists(targetPath)) File.Delete(targetPath);

            profileFile = GetSelectedProfileFile();
            currentSaveName = "";
            SavePreferences();
            LaunchGame(profileFile);
        }

        private void LaunchGame(string profile)
        {
            var info = new ProcessStartInfo
            {
                FileName = teknoExePath,
                Arguments = $"--profile=\"{profile}\" --start",
                WorkingDirectory = Path.GetDirectoryName(teknoExePath),
                UseShellExecute = true
            };
            Process.Start(info);
            this.WindowState = FormWindowState.Minimized;
            StartTeknoWatchdog();
        }

        private void StartTeknoWatchdog()
        {
            teknoExitTimer = new Timer { Interval = 500 };
            teknoExitTimer.Tick += (s, e) =>
            {
                var running = Process.GetProcessesByName("TeknoParrotUi").Any();
                if (!running)
                {
                    teknoExitTimer.Stop();
                    teknoExitTimer.Dispose();
                    teknoExitTimer = null;

                    this.Invoke((MethodInvoker)delegate
                    {
                        string newFilenameToSelect = null;

                        foreach (var (profile, displayName, saveFileName) in idGames)
                        {
                            string path = Path.Combine(savePathRoot, saveFileName);
                            if (File.Exists(path))
                            {
                                string prefix = profile.Replace(".xml", "");

                                if (!string.IsNullOrWhiteSpace(currentSaveName))
                                {
                                    string backupName = prefix + "_" + currentSaveName + ".bin";
                                    string targetPath = Path.Combine(backupPath, backupName);
                                    File.Copy(path, targetPath, true);
                                    newFilenameToSelect = Path.GetFileName(targetPath);
                                }
                                else
                                {
                                    pendingSaveNeedsName = true;

                                    string extracted = SaveFileHelper.TryExtractPlayerName(path);
                                    string name = extracted;

                                    bool skipPrompt = chkAutoNameOnly.Checked &&
                                                      !string.IsNullOrWhiteSpace(name) &&
                                                      !ContainsInvalidFileNameChars(name);

                                    string promptText = ContainsInvalidFileNameChars(name)
                                    ? "The extracted name contains invalid characters.\nPlease provide a valid name for your save:"
    :                               $"Name the save created for {displayName} during your last session:";

                                    if (!skipPrompt)
                                    {
                                        name = PromptForValidName(
                                            promptText,
                                            "New Save Detected",
                                            extracted
                                        );
                                    }
                                    string rawName = string.IsNullOrWhiteSpace(name)
                                        ? "AutoBackup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss")
                                        : name;

                                    string finalPath = GetUniqueSavePath(rawName, prefix);
                                    File.Copy(path, finalPath);
                                    newFilenameToSelect = Path.GetFileName(finalPath);
                                }

                                File.Delete(path);
                            }
                        }


                        LoadBackupList();

                        if (!string.IsNullOrEmpty(newFilenameToSelect))
                        {
                            comboSaves.SelectedItem = comboSaves.Items
                                .Cast<ComboBoxItem>()
                                .FirstOrDefault(i => i.FullFilename == newFilenameToSelect);
                            UpdateLaunchButtons();
                        }

                        this.WindowState = FormWindowState.Normal;
                        this.BringToFront();
                        this.Activate();
                    });
                }
            };
            teknoExitTimer.Start();
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
            if (selectedItem != null)
            {
                string oldName = selectedItem.FullFilename;
                string newName = PromptForValidName("Enter new name for the save:", "Rename Save", selectedItem.DisplayText);
                if (!string.IsNullOrWhiteSpace(newName))
                {
                    string prefix = GetSelectedGamePrefix();
                    string newPath = Path.Combine(backupPath, prefix + "_" + newName + ".bin");
                    if (File.Exists(newPath))
                    {
                        MessageBox.Show("A save with that name already exists.");
                        return;
                    }
                    File.Move(Path.Combine(backupPath, oldName), newPath);
                    LoadBackupList();
                    comboSaves.SelectedItem = comboSaves.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.FullFilename == Path.GetFileName(newPath));
                }
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            var selectedItem = comboSaves.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                string selected = selectedItem.FullFilename;
                var confirm = MessageBox.Show($"Are you sure you want to delete '{selected}'?", "Confirm Delete", MessageBoxButtons.YesNo);
                if (confirm == DialogResult.Yes)
                {
                    File.Delete(Path.Combine(backupPath, selected));
                    LoadBackupList();
                }
            }
        }

        private void btnDuplicate_Click(object sender, EventArgs e)
        {
            var selectedItem = comboSaves.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                string original = selectedItem.FullFilename;
                string originalPath = Path.Combine(backupPath, original);
                string prefix = GetSelectedGamePrefix();

                // Strip prefix and extension to get base name
                string baseName = Path.GetFileNameWithoutExtension(original).Replace(prefix + "_", "");

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
                    File.Copy(originalPath, newPath);

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