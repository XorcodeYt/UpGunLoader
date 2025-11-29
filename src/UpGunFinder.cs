using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public static class UpGunFinder
{
    public static string GetUpGunPath()
    {
        var possibleSteamRoots = new List<string>();

        try
        {
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string commonProgram = programFiles ?? programFilesX86;
            if (!string.IsNullOrEmpty(programFiles)) possibleSteamRoots.Add(Path.Combine(programFiles, "Steam"));
            if (!string.IsNullOrEmpty(programFilesX86)) possibleSteamRoots.Add(Path.Combine(programFilesX86, "Steam"));

            string localPrograms = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) ?? "", "Programs", "Steam");
            possibleSteamRoots.Add(localPrograms);
        }
        catch { }

        foreach (var root in possibleSteamRoots.Distinct())
        {
            try
            {
                if (string.IsNullOrWhiteSpace(root)) continue;
                if (Directory.Exists(root))
                {
                    var result = FindGameFromSteamRoot(root);
                    if (result != null) return result;
                }
            }
            catch { }
        }

        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
        {
            try
            {
                var found = FindSteamappsUnderRoot(drive.RootDirectory.FullName, maxDepth: 3);
                foreach (var steamapps in found)
                {
                    var root = Path.GetDirectoryName(steamapps.TrimEnd(Path.DirectorySeparatorChar));
                    var candidateRoot = root;
                    var result = FindGameFromSteamRoot(candidateRoot);
                    if (result != null) return result;
                }
            }
            catch { }
        }

        return null;
    }

    private static string FindGameFromSteamRoot(string steamRoot)
    {
        if (string.IsNullOrWhiteSpace(steamRoot)) return null;

        try
        {
            string steamappsPath = Path.Combine(steamRoot, "steamapps");
            if (!Directory.Exists(steamappsPath)) return null;

            string commonUpGun = Path.Combine(steamappsPath, "common", "UpGun");
            if (Directory.Exists(commonUpGun)) return commonUpGun;

            string manifestPath = Path.Combine(steamappsPath, "appmanifest_1575870.acf");
            if (File.Exists(manifestPath))
            {
                string txt = File.ReadAllText(manifestPath);
                var installdir = ExtractKeyValue(txt, "installdir");
                if (!string.IsNullOrEmpty(installdir))
                {
                    var candidate = Path.Combine(steamappsPath, "common", installdir);
                    if (Directory.Exists(candidate)) return candidate;
                }
            }

            var manifestFiles = Directory.EnumerateFiles(steamappsPath, "appmanifest_*.acf", SearchOption.TopDirectoryOnly);
            foreach (var mf in manifestFiles)
            {
                try
                {
                    string txt = File.ReadAllText(mf);
                    if (ContainsGameNameUpGun(txt))
                    {
                        var installdir = ExtractKeyValue(txt, "installdir");
                        if (!string.IsNullOrEmpty(installdir))
                        {
                            var candidate = Path.Combine(steamappsPath, "common", installdir);
                            if (Directory.Exists(candidate)) return candidate;
                        }

                        if (Directory.Exists(commonUpGun)) return commonUpGun;
                    }
                }
                catch { }
            }

            var libVdf = Path.Combine(steamappsPath, "libraryfolders.vdf");
            if (File.Exists(libVdf))
            {
                var libs = ParseLibraryFoldersVdf(libVdf);
                foreach (var lib in libs)
                {
                    try
                    {
                        string libSteamapps = Path.Combine(lib, "steamapps");
                        if (!Directory.Exists(libSteamapps)) continue;

                        var libCommonUpGun = Path.Combine(libSteamapps, "common", "UpGun");
                        if (Directory.Exists(libCommonUpGun)) return libCommonUpGun;

                        var manifs = Directory.EnumerateFiles(libSteamapps, "appmanifest_*.acf", SearchOption.TopDirectoryOnly);
                        foreach (var mf in manifs)
                        {
                            string txt = File.ReadAllText(mf);
                            if (ContainsGameNameUpGun(txt))
                            {
                                var installdir = ExtractKeyValue(txt, "installdir");
                                if (!string.IsNullOrEmpty(installdir))
                                {
                                    var candidate = Path.Combine(libSteamapps, "common", installdir);
                                    if (Directory.Exists(candidate)) return candidate;
                                }

                                if (Directory.Exists(libCommonUpGun)) return libCommonUpGun;
                            }
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }

        return null;
    }

    private static bool ContainsGameNameUpGun(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        var regex = new Regex("\"name\"\\s+\"([^\"]+)\"", RegexOptions.IgnoreCase);
        var m = regex.Match(text);
        if (m.Success)
        {
            var name = m.Groups[1].Value;
            if (string.Equals(name, "UpGun", StringComparison.OrdinalIgnoreCase)) return true;
        }

        if (text.IndexOf("UpGun", StringComparison.OrdinalIgnoreCase) >= 0) return true;

        return false;
    }

    private static string ExtractKeyValue(string text, string key)
    {
        if (string.IsNullOrEmpty(text)) return null;
        var regex = new Regex("\"" + Regex.Escape(key) + "\"\\s+\"([^\"]+)\"", RegexOptions.IgnoreCase);
        var m = regex.Match(text);
        if (m.Success) return m.Groups[1].Value;
        return null;
    }

    private static IEnumerable<string> ParseLibraryFoldersVdf(string filePath)
    {
        var results = new List<string>();
        try
        {
            var lines = File.ReadAllLines(filePath);
            var pathRegex = new Regex("\"[0-9]+\"\\s+\"([A-Za-z]:\\\\.+?)\"", RegexOptions.IgnoreCase);
            var pathRegex2 = new Regex("\"path\"\\s+\"([A-Za-z]:\\\\.+?)\"", RegexOptions.IgnoreCase);

            foreach (var l in lines)
            {
                var m = pathRegex.Match(l);
                if (m.Success) results.Add(m.Groups[1].Value);
                else
                {
                    var m2 = pathRegex2.Match(l);
                    if (m2.Success) results.Add(m2.Groups[1].Value);
                    else
                    {
                        var anyPath = Regex.Match(l, "\"([A-Za-z]:\\\\.+?)\"");
                        if (anyPath.Success)
                        {
                            var p = anyPath.Groups[1].Value;
                            if (Directory.Exists(p)) results.Add(p);
                        }
                    }
                }
            }
        }
        catch { }
        return results.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> FindSteamappsUnderRoot(string root, int maxDepth = 3)
    {
        var results = new List<string>();
        var queue = new Queue<(string path, int depth)>();
        queue.Enqueue((root, 0));
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { root };

        while (queue.Count > 0)
        {
            var (path, depth) = queue.Dequeue();
            if (depth > maxDepth) continue;

            IEnumerable<string> subdirs;
            try
            {
                subdirs = Directory.EnumerateDirectories(path);
            }
            catch { continue; }

            foreach (var d in subdirs)
            {
                if (seen.Contains(d)) continue;
                seen.Add(d);

                try
                {
                    var name = Path.GetFileName(d).Trim().ToLowerInvariant();
                    if (name == "steamapps")
                    {
                        results.Add(d);
                        continue;
                    }

                    if (depth < maxDepth)
                    {
                        queue.Enqueue((d, depth + 1));
                    }
                }
                catch { }
            }
        }

        return results;
    }

    public static string GetUpGunPaksPath()
    {
        var root = GetUpGunPath();
        if (string.IsNullOrEmpty(root))
            return null;

        var paksPath = Path.Combine(root, "UpGun", "Content", "Paks");

        var result = Directory.Exists(paksPath) ? paksPath : null;

        return result;
    }

    public static string GetInstalledVersion()
    {
        try
        {
            var upGunPath = GetUpGunPath();
            if (string.IsNullOrEmpty(upGunPath))
                return null;

            var steamappsPath = Directory.GetParent(upGunPath)?.Parent?.FullName;
            if (string.IsNullOrEmpty(steamappsPath))
                return null;

            var manifestPath = Path.Combine(steamappsPath, "appmanifest_1575870.acf");
            if (!File.Exists(manifestPath))
                return null;

            string text = File.ReadAllText(manifestPath);
            var buildId = ExtractKeyValue(text, "buildid");
            if (!string.IsNullOrEmpty(buildId))
                return buildId;

            var lastUpdated = ExtractKeyValue(text, "LastUpdated");
            if (!string.IsNullOrEmpty(lastUpdated))
                return $"Build {buildId} (updated {lastUpdated})";
        }
        catch
        {
            return null;
        }

        return null;
    }

}
