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

[Tasks]
Name: "desktopicon"; Description: "Créer une icône sur le bureau"; GroupDescription: "Raccourcis supplémentaires :"

[Files]
Source: "..\publish_output\appsettings.json"; DestDir: "{app}"; Flags: onlyifdoesntexist
Source: "..\assets\icons\Logo-Delta-Racing-rouge-et-blanc-1.ico"; DestDir: "{app}"; DestName: "LMU-DeltaRacing.ico"; Flags: ignoreversion
Source: "..\publish_output\*"; DestDir: "{app}"; Excludes: "appsettings.json,appsettings.Development.json,*.pdb,web.config"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\LMU-DeltaRacing"; Filename: "{app}\LMU-DeltaRacing.UI.exe"; WorkingDir: "{app}"; IconFilename: "{app}\LMU-DeltaRacing.ico"
Name: "{autodesktop}\LMU-DeltaRacing"; Filename: "{app}\LMU-DeltaRacing.UI.exe"; WorkingDir: "{app}"; IconFilename: "{app}\LMU-DeltaRacing.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\LMU-DeltaRacing.UI.exe"; Description: "Lancer LMU-DeltaRacing"; Flags: postinstall nowait skipifsilent

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

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultsDirectory: String;
  ConfigPath: String;
  ConfigLines: TArrayOfString;
  ConfigLine: String;
  JsonPath: String;
  SearchText: String;
  ReplaceText: String;
  ConfigLineIndex: Integer;
  ConfigUpdated: Boolean;
begin
  if CurStep = ssPostInstall then
  begin
    ResultsDirectory := FindResultsDirectory();
    if ResultsDirectory = '' then
      ResultsDirectory := ExpandConstant('{app}\TestAssets\Results');

    ConfigPath := ExpandConstant('{app}\appsettings.json');
    if not LoadStringsFromFile(ConfigPath, ConfigLines) then
      RaiseException('Impossible de lire la configuration installée : ' + ConfigPath);

    SearchText := '"ResultsDirectory": ""';
    JsonPath := ResultsDirectory;
    StringChangeEx(JsonPath, '\', '\\', True);
    ReplaceText := '"ResultsDirectory": "' + JsonPath + '"';
    ConfigUpdated := False;
    for ConfigLineIndex := 0 to GetArrayLength(ConfigLines) - 1 do
    begin
      ConfigLine := ConfigLines[ConfigLineIndex];
      if Pos(SearchText, ConfigLine) > 0 then
      begin
        StringChangeEx(ConfigLine, SearchText, ReplaceText, True);
        ConfigLines[ConfigLineIndex] := ConfigLine;
        ConfigUpdated := True;
      end;
    end;

    if ConfigUpdated and not SaveStringsToUTF8FileWithoutBOM(ConfigPath, ConfigLines, False) then
    begin
      RaiseException('Impossible d''enregistrer le dossier de résultats dans ' + ConfigPath);
    end;
  end;
end;
