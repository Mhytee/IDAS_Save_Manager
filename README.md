INITIAL D ARCADE STAGE SAVE MANAGER  
===================================  

Launch Games & Manage Multiple Save Files for Initial D Arcade Stage 6–8 (TeknoParrot)

> I originally made this program so that it would be easy for my girlfriend and/or guests to play on their own saves and for us to be able to switch back and forth easily...  
> Turns out she doesn't enjoy playing and I have no friends — so here it is, for anyone else who might find it useful.

📂 INSTALLATION  
---------------  
1. Unzip this folder anywhere you like (e.g., Desktop or Documents).  
2. Run the program by double-clicking `IDAS_Save_Manager.exe`.  
3. On first launch, click the large red button at the top to set your TeknoParrotUi.exe path. (REQUIRED)

🧠 WHAT THIS TOOL DOES  
----------------------  
This app helps you launch your Initial D games and safely manage multiple save files by:  
- Automatically backing up and restoring card saves  
- Supporting Initial D Arcade Stage 6, 7, and 8 (TeknoParrot versions)

✨ Features:  
- ✅ Launch games directly with a selected save (no need to open TeknoParrotUI)  
- ❌ Launch with no save file  
- 📝 Rename any save  
- 🗑️ Delete saves (with confirmation)  
- 📄 Duplicate saves with automatic name suggestions  
- ✨ Automatically reads your in-game player name from the save file and uses it when naming new saves (when possible)  
- 🔘 Option to skip save name prompts and safely auto-name silently (toggle available in UI)  
- 🔁 Automatically backs up:  
  - Any save detected in `AppData\TeknoParrot\` when the program starts  
  - New saves created during “Continue Without Save” sessions  
- 💾 Organizes backups by game prefix (`ID6_`, `ID7_`, `ID8_`)  
- 🧠 Remembers your last selected game and save between launches

💾 SAVE FILES LOCATION  
----------------------  
All backups are stored at:  
`AppData\IDAS_Save_Manager\IDAS_Backups`

⚙️ PROGRAM BEHAVIOR  
-------------------  
- If you launch without a save, the app will monitor for new save files.  
  - When TeknoParrot exits, you'll be prompted to name and store the new save (or it will auto-name if the prompt is skipped).  

- If a save is already in TeknoParrot's AppData folder at launch, it is treated as external and will be imported.

- The app always removes save files from TeknoParrot’s folder after each session to prevent accidental overwrites.

🧽 UNINSTALLING  
---------------  
To fully remove the app:  
1. Delete the EXE and extracted folder.  
2. (Optional) Delete your settings and backups at:  
   `AppData\IDAS_Save_Manager`

⚠️ Be sure to back up any important saves from `IDAS_Backups` before uninstalling!

—

Developed by Mhytee, A lonely driver.
