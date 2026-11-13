← [Contents](README.md) · **English** · [Français](../fr/settings.md) · [Deutsch](../de/settings.md) · [Italiano](../it/settings.md) · [Español](../es/settings.md)

# Settings

| Setting | What it does |
|---|---|
| **XML results folder** | Where the game saves your results, usually `Documents\Le Mans Ultimate\UserData\Log\Results`. Pick it with **Browse…**, then **Apply**. Empty: automatic detection. |
| **Language and date** | **Interface language**: English, Français, Deutsch, Italiano or Español (by default the language of Windows, otherwise English). **Date format**: follow the language, or day/month, month/day, ISO, day.month. The page reloads; lap times and numbers do not change. |
| **Main driver** | The driver you follow (you). The list is sorted by number of appearances. |
| **Driver merge** | Changed your nickname? Pick the old name and the kept name, then **Merge**. Your files are not modified and the merge can be undone. A warning shows if both names raced together. |
| **Auto start** | Watch for new results from launch (on by default). |
| **Loaded sessions** | **Load the practice sessions**: also load practice, for the **PRACTICE** tab of [Race details](race-details.md). **Load the offline races against the AI**: also load your solo races (the game’s “Race Weekend” mode), left out so they do not skew the driver rating and statistics between players. Both are off by default; the results reload as soon as you tick or untick. |
| **Updates** | Installed version and download of the new one. The application asks GitHub for the latest version number at startup; untick to stop. |

Below the settings, the **import status** shows loading progress, unreadable files and your recent sessions.

Settings are saved in `appsettings.json`, in the installation folder (`%LOCALAPPDATA%\Programs\LMU-DeltaRacing`).
