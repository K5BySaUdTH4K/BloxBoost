using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BloxBoost
{
    /// <summary>
    /// Writes Roblox FastFlags (graphics optimisation) into the client's
    /// ClientSettings\ClientAppSettings.json. Non-invasive: only writes a config
    /// file under the user's LOCALAPPDATA. No process tampering, no admin needed.
    /// </summary>
    internal static class FlagEngine
    {
        private const string BackupSuffix = ".bloxboost.bak";

        // --- Presets (FastFlag name -> value). Roblox expects all values as strings. ---
        // NOTE: exact effective set must be tuned on a live client (Roblox allowlists
        // some flags). The Windows-side optimiser does not depend on these.
        public static readonly Dictionary<string, string> MaxFps = new Dictionary<string, string>
        {
            { "DFIntDebugFRMQualityLevelOverride", "1" },
            { "FFlagDisablePostFx", "True" },
            { "FIntDebugForceMSAASamples", "0" },
            { "FIntRenderShadowIntensity", "0" },
            { "DFFlagDebugRenderForceTechnologyVoxel", "True" },
            { "DFFlagDebugPauseVoxelizer", "True" },
            { "FIntFRMMinGrassDistance", "0" },
            { "FIntFRMMaxGrassDistance", "0" },
            { "FIntRenderGrassDetailStrands", "0" },
            { "DFIntTextureQualityOverride", "0" },
            { "FFlagDebugSkyGray", "True" },
            { "FIntRenderLocalLightUpdatesMax", "4" },
            { "DFIntTaskSchedulerTargetFps", "240" },
        };

        public static readonly Dictionary<string, string> Balanced = new Dictionary<string, string>
        {
            { "DFIntDebugFRMQualityLevelOverride", "10" },
            { "FFlagDisablePostFx", "False" },
            { "FIntRenderShadowIntensity", "0" },
            { "DFFlagDebugRenderForceTechnologyVoxel", "True" },
            { "FIntFRMMinGrassDistance", "0" },
            { "DFIntTaskSchedulerTargetFps", "240" },
        };

        /// <summary>Roblox Player version folders that actually contain the player exe.</summary>
        public static List<string> GetRobloxVersionDirs()
        {
            var dirs = new List<string>();
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var versionsRoot = Path.Combine(localAppData, "Roblox", "Versions");
            if (!Directory.Exists(versionsRoot)) return dirs;

            foreach (var dir in Directory.GetDirectories(versionsRoot))
            {
                if (File.Exists(Path.Combine(dir, "RobloxPlayerBeta.exe")))
                    dirs.Add(dir);
            }
            return dirs;
        }

        public static bool IsRobloxInstalled() { return GetRobloxVersionDirs().Count > 0; }

        /// <summary>Applies a preset to every detected Roblox version folder. Returns folders touched.</summary>
        public static int ApplyPreset(Dictionary<string, string> flags)
        {
            int count = 0;
            foreach (var versionDir in GetRobloxVersionDirs())
            {
                var settingsDir = Path.Combine(versionDir, "ClientSettings");
                Directory.CreateDirectory(settingsDir);
                var file = Path.Combine(settingsDir, "ClientAppSettings.json");

                // Preserve the user's original file once, so Revert can restore it.
                var backup = file + BackupSuffix;
                if (File.Exists(file) && !File.Exists(backup))
                    File.Copy(file, backup);

                File.WriteAllText(file, Serialize(flags), new UTF8Encoding(false));
                count++;
            }
            return count;
        }

        /// <summary>Removes BloxBoost flags, restoring any original file we backed up.</summary>
        public static int Revert()
        {
            int count = 0;
            foreach (var versionDir in GetRobloxVersionDirs())
            {
                var settingsDir = Path.Combine(versionDir, "ClientSettings");
                var file = Path.Combine(settingsDir, "ClientAppSettings.json");
                var backup = file + BackupSuffix;

                if (File.Exists(backup))
                {
                    if (File.Exists(file)) File.Delete(file);
                    File.Move(backup, file);
                    count++;
                }
                else if (File.Exists(file))
                {
                    // No original existed; the file is ours to remove.
                    File.Delete(file);
                    count++;
                }
            }
            return count;
        }

        public static bool IsApplied()
        {
            foreach (var versionDir in GetRobloxVersionDirs())
            {
                var file = Path.Combine(versionDir, "ClientSettings", "ClientAppSettings.json");
                if (File.Exists(file) &&
                    File.ReadAllText(file).IndexOf("DFIntDebugFRMQualityLevelOverride", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private static string Serialize(Dictionary<string, string> flags)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            var items = flags.ToList();
            for (int i = 0; i < items.Count; i++)
            {
                var comma = i < items.Count - 1 ? "," : "";
                sb.AppendLine("  \"" + Esc(items[i].Key) + "\": \"" + Esc(items[i].Value) + "\"" + comma);
            }
            sb.Append("}");
            return sb.ToString();
        }

        private static string Esc(string s) { return s.Replace("\\", "\\\\").Replace("\"", "\\\""); }
    }
}
