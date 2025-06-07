INITIAL D ARCADE STAGE SAVE MANAGER  
===================================  

Launch Games & Manage Multiple Save Files for Initial D Arcade Stage 6–8 (TeknoParrot)

> I originally made this program so that it would be easy for my girlfriend and/or guests to play on their own saves and for us to be able to switch back and forth easily...  
> Turns out she doesn't enjoy playing and I have no friends — so here it is, for anyone else who might find it useful.

📂 INSTALLATION  
---------------  
1. Download the latest `.zip` release from the [Releases](https://github.com/mhytee/idas_save_manager/releases) tab.  
2. Extract the folder anywhere you like (e.g., Desktop or Documents).  
3. Run the program by double-clicking `IDAS_Save_Manager.exe`.  
4. On first launch, click the large red button at the top to set your `TeknoParrotUi.exe` path. _(This is required before launching any games.)_


🧠 WHAT THIS TOOL DOES  
----------------------  
This app helps you launch your Initial D games and safely manage multiple save files by:  
- Automatically backing up and restoring card saves  
- Supporting Initial D Arcade Stage 6, 7, and 8 (TeknoParrot versions)

✨ Features:  
- 🔁 **Automatically backs up** save files from `AppData\TeknoParrot\`
- 💾 **Restore and launch** any save directly — no need to open TeknoParrot manually
- ❌ **Launch without a save** — ideal for creating new profiles
- 📝 **Rename**, 📄 **Duplicate**, and 🗑️ **Delete** saves
- ✨ **Reads your in-game player name** and uses it as the save name (when possible)
- 🔘 Optional **toggle to skip save name prompts** and auto-name silently
- 🧠 **Remembers your last selected game and save** between launches

💾 SAVE FILES LOCATION  
----------------------  
All backups are stored at:  
`AppData\IDAS_Save_Manager\IDAS_Backups`

⚙️ PROGRAM BEHAVIOR  
-------------------  
- On first launch, the program will detect your existing save file and promot you to import it.

- If you launch a game without a save, the app will monitor for new save files.  
  - When Teknoparrot exits, you'll be prompted to name and store the new save (or it will auto-name if the prompt is skipped).  

- If a save is already in TeknoParrot's AppData folder at launch, it is treated as external and will be imported.

- The app always moves save files from TeknoParrot’s folder after each session to prevent accidental overwrites.

🧽 UNINSTALLING  
---------------  
To fully remove the app:  
1. Delete the EXE and extracted folder.  
2. (Optional) Delete your settings and backups at:  
   `AppData\IDAS_Save_Manager`

⚠️ Be sure to back up any important saves from `IDAS_Backups` before uninstalling!

—

Developed by Mhytee, A lonely driver.
