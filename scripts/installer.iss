#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif

[Setup]
AppId={{E046CC80-0C19-4F06-BF44-8DA1FE6725E2}
AppName=LMU-DeltaRacing
AppVersion={#AppVersion}
AppPublisher=Fugeekies
DefaultDirName={localappdata}\Programs\LMU-DeltaRacing
DefaultGroupName=LMU-DeltaRacing
SetupIconFile=..\assets\icons\Logo-Delta-Racing-rouge-et-blanc-1.ico
UninstallDisplayIcon={app}\LMU-DeltaRacing.ico
OutputDir=..\output
OutputBaseFilename=LMU-DeltaRacing-Setup
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=lowest
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
; The setup speaks the five languages of the application, proposed from the language of Windows.
ShowLanguageDialog=auto
LanguageDetectionMethod=uilanguage

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "fr"; MessagesFile: "compiler:Languages\French.isl"
Name: "de"; MessagesFile: "compiler:Languages\German.isl"
Name: "it"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"

[CustomMessages]
en.ConfigReadFailed=Cannot read the installed settings: %1
fr.ConfigReadFailed=Impossible de lire la configuration installée : %1
de.ConfigReadFailed=Die installierten Einstellungen können nicht gelesen werden: %1
it.ConfigReadFailed=Impossibile leggere le impostazioni installate: %1
es.ConfigReadFailed=No se puede leer la configuración instalada: %1
en.ConfigSaveFailed=Cannot save the settings in %1
fr.ConfigSaveFailed=Impossible d'enregistrer la configuration dans %1
de.ConfigSaveFailed=Die Einstellungen können nicht in %1 gespeichert werden
it.ConfigSaveFailed=Impossibile salvare le impostazioni in %1
es.ConfigSaveFailed=No se puede guardar la configuración en %1

[Tasks]
; Messages of the Inno Setup language files: "Create a desktop shortcut", "Additional shortcuts:".
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\publish_output\appsettings.json"; DestDir: "{app}"; Flags: onlyifdoesntexist
Source: "..\assets\icons\Logo-Delta-Racing-rouge-et-blanc-1.ico"; DestDir: "{app}"; DestName: "LMU-DeltaRacing.ico"; Flags: ignoreversion
Source: "..\publish_output\*"; DestDir: "{app}"; Excludes: "appsettings.json,appsettings.Development.json,*.pdb,web.config"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\LMU-DeltaRacing"; Filename: "{app}\LMU-DeltaRacing.UI.exe"; WorkingDir: "{app}"; IconFilename: "{app}\LMU-DeltaRacing.ico"
Name: "{autodesktop}\LMU-DeltaRacing"; Filename: "{app}\LMU-DeltaRacing.UI.exe"; WorkingDir: "{app}"; IconFilename: "{app}\LMU-DeltaRacing.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\LMU-DeltaRacing.UI.exe"; Description: "{cm:LaunchProgram,LMU-DeltaRacing}"; Flags: postinstall nowait skipifsilent

[Code]
function HasXmlResults(Directory: String): Boolean;
var
  FindRec: TFindRec;
begin
  Result := False;
  if DirExists(Directory) and FindFirst(AddBackslash(Directory) + '*.xml', FindRec) then
  begin
    Result := (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) = 0;
    FindClose(FindRec);
  end;
end;

function FindResultsDirectory: String;
var
  Candidates: array[0..4] of String;
  Candidate: String;
  SteamPath: String;
  GamePath: String;
  Index: Integer;
begin
  Candidates[0] := ExpandConstant('{userdocs}\Le Mans Ultimate\UserData\Log\Results');
  Candidates[1] := ExpandConstant('{userdocs}\My Games\Le Mans Ultimate\UserData\Log\Results');
  Candidates[2] := ExpandConstant('{localappdata}\Le Mans Ultimate\UserData\Log\Results');
  Candidates[3] := ExpandConstant('{app}\UserData\Log\Results');

  SteamPath := '';
  GamePath := '';
  if not RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 2399420', 'InstallLocation', GamePath) then
  begin
    if RegQueryStringValue(HKCU, 'Software\Valve\Steam', 'SteamPath', SteamPath) then
    begin
      StringChangeEx(SteamPath, '/', '\', True);
      GamePath := AddBackslash(SteamPath) + 'steamapps\common\Le Mans Ultimate';
    end;
  end;
  if GamePath <> '' then
    Candidates[3] := AddBackslash(GamePath) + 'UserData\Log\Results';

  for Index := 0 to 3 do
  begin
    Candidate := Candidates[Index];
    if (Candidate <> '') and HasXmlResults(Candidate) then
    begin
      Result := Candidate;
      Exit;
    end;
  end;

  Result := '';
end;

var
  IsFirstInstall: Boolean;

// Replaces a setting left empty by the packaged appsettings.json; returns whether a line changed.
function ReplaceSetting(var ConfigLines: TArrayOfString; SearchText: String; ReplaceText: String): Boolean;
var
  ConfigLine: String;
  ConfigLineIndex: Integer;
begin
  Result := False;
  for ConfigLineIndex := 0 to GetArrayLength(ConfigLines) - 1 do
  begin
    ConfigLine := ConfigLines[ConfigLineIndex];
    if Pos(SearchText, ConfigLine) > 0 then
    begin
      StringChangeEx(ConfigLine, SearchText, ReplaceText, True);
      ConfigLines[ConfigLineIndex] := ConfigLine;
      Result := True;
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultsDirectory: String;
  ConfigPath: String;
  ConfigLines: TArrayOfString;
  JsonPath: String;
  ConfigUpdated: Boolean;
begin
  ConfigPath := ExpandConstant('{app}\appsettings.json');

  // Before the files are copied: an update keeps the settings file, and so its language.
  if CurStep = ssInstall then
    IsFirstInstall := not FileExists(ConfigPath);

  if CurStep = ssPostInstall then
  begin
    ResultsDirectory := FindResultsDirectory();
    if ResultsDirectory = '' then
      ResultsDirectory := ExpandConstant('{app}\TestAssets\Results');

    if not LoadStringsFromFile(ConfigPath, ConfigLines) then
      RaiseException(FmtMessage(CustomMessage('ConfigReadFailed'), [ConfigPath]));

    JsonPath := ResultsDirectory;
    StringChangeEx(JsonPath, '\', '\\', True);
    ConfigUpdated := ReplaceSetting(ConfigLines, '"ResultsDirectory": ""', '"ResultsDirectory": "' + JsonPath + '"');

    // The application starts in the language chosen for the setup (en, fr, de, it or es). Its
    // date format is already "Language" in the packaged file: dates follow that language.
    if IsFirstInstall then
      ConfigUpdated := ReplaceSetting(ConfigLines, '"Language": ""', '"Language": "' + ActiveLanguage() + '"') or ConfigUpdated;

    if ConfigUpdated and not SaveStringsToUTF8FileWithoutBOM(ConfigPath, ConfigLines, False) then
      RaiseException(FmtMessage(CustomMessage('ConfigSaveFailed'), [ConfigPath]));
  end;
end;
