[Setup]
AppName=UpGunModLoader
AppVersion=1.1
DefaultDirName={userappdata}\UpGunMods
DefaultGroupName=UpGunMods
OutputBaseFilename=Setup_UpGunModLoader
Compression=lzma2/ultra64
SolidCompression=yes
DisableDirPage=yes
DisableProgramGroupPage=yes
AppCopyright=Copyright © 2025 Xorcode & ZNS.
SetupIconFile=C:\Users\ZNS\Documents\GitHub\UpGunLoader\Setup\app.ico
WizardStyle=classic dark
WizardImageFile=C:\Users\ZNS\Documents\GitHub\UpGunLoader\Setup\finish.bmp
WizardSmallImageFile=C:\Users\ZNS\Documents\GitHub\UpGunLoader\Setup\image.bmp

[Files]
Source: "C:\Users\ZNS\Documents\GitHub\UpGunLoader\Setup\Source\*"; DestDir: "{app}"; Flags: recursesubdirs

[Icons]
Name: "{userdesktop}\UpGunModLoader"; Filename: "{app}\UpGunModLoader.exe"

[Code]
function InitializeSetup(): Boolean;
var
  ModLoaderPath: String;
  FindRec: TFindRec;
begin
  ModLoaderPath := ExpandConstant('{userappdata}\UpGunMods');

  if FileExists(ModLoaderPath + '\UpGunModLoader.exe') then
  begin
    if FindFirst(ModLoaderPath + '\*', FindRec) then
    begin
      try
        repeat
          if (FindRec.Name <> '.') and (FindRec.Name <> '..') and (CompareText(FindRec.Name, 'Mods') <> 0) then
          begin
            if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0 then
              DelTree(ModLoaderPath + '\' + FindRec.Name, True, True, True)
            else
              DeleteFile(ModLoaderPath + '\' + FindRec.Name);
          end;
        until not FindNext(FindRec);
      finally
        FindClose(FindRec);
      end;
    end;
  end;

  Result := True;
end;


procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if not DirExists(ExpandConstant('{app}\Mods')) then
      ForceDirectories(ExpandConstant('{app}\Mods'));
  end;
end;

procedure DeleteExceptPakSig(const Path: String);
var
  FindRec: TFindRec;
  FilePath: String;
begin
  if FindFirst(Path + '\*', FindRec) then
  begin
    try
      repeat
        FilePath := Path + '\' + FindRec.Name;

        if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0 then
        begin
          if (FindRec.Name <> '.') and (FindRec.Name <> '..') then
          begin
            DelTree(FilePath, True, True, True);
          end;
        end
        else
        begin
          if (CompareText(FindRec.Name, 'UpGun-WindowsNoEditor.pak') <> 0) and
             (CompareText(FindRec.Name, 'UpGun-WindowsNoEditor.sig') <> 0) then
          begin
            DeleteFile(FilePath);
          end;
        end;

      until not FindNext(FindRec);
    finally
      FindClose(FindRec);
    end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  GamePathFile: String;
  GamePathAnsi: AnsiString;
  GamePath: String;
begin
  if CurUninstallStep = usUninstall then
  begin
    GamePathFile := ExpandConstant('{userappdata}\UpGunMods\gamepath.txt');

    if LoadStringFromFile(GamePathFile, GamePathAnsi) then
    begin
      GamePath := Trim(String(GamePathAnsi));

      if DirExists(GamePath) then
      begin
        if (CompareText(GamePath, 'C:\') = 0) or
           (CompareText(GamePath, 'C:\Windows') = 0) or
           (Pos('Windows', GamePath) > 0) then
        begin
          MsgBox('Error while deleting mods please delete all game paks and reinstall game', mbError, MB_OK);
        end
        else
        begin
          try
            DeleteExceptPakSig(GamePath);
          except
            MsgBox('Error while deleting : ' + GamePath, mbError, MB_OK);
          end;
        end;
      end;
    end;
    try
      DelTree(ExpandConstant('{userappdata}\UpGunMods'), True, True, True);
    except
      MsgBox('Unable to delete UpGunMods folder please delete it yourself', mbError, MB_OK);
    end;
  end;
end;
