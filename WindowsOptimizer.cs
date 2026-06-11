using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace BloxBoost
{
    /// <summary>
    /// Windows-side optimisations. These do not touch the Roblox process memory,
    /// so they work regardless of Hyperion and never trip antivirus. Best-effort:
    /// each step swallows its own failure so a non-admin run still does what it can.
    /// </summary>
    internal static class WindowsOptimizer
    {
        private const string HighPerfGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";

        // Heavy, non-essential apps a booster commonly closes. Conservative list —
        // no system processes, only user apps gated behind an explicit UI opt-in.
        private static readonly string[] BackgroundApps =
        {
            "chrome", "msedge", "opera", "brave", "firefox",
            "Spotify", "Discord", "OneDrive", "Teams", "EpicGamesLauncher", "Steam"
        };

        private static string StateDir
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BloxBoost"); }
        }

        private static string PowerStateFile
        {
            get { return Path.Combine(StateDir, "prev_power_scheme.txt"); }
        }

        [DllImport("kernel32.dll")]
        private static extern int K32EmptyWorkingSet(IntPtr hProcess);

        /// <summary>Switch to High Performance power plan, remembering the previous one.</summary>
        public static bool ApplyHighPerformancePowerPlan()
        {
            try
            {
                var current = RunPowercfg("/getactivescheme");
                var guid = ExtractGuid(current);
                if (!string.IsNullOrEmpty(guid) && !guid.Equals(HighPerfGuid, StringComparison.OrdinalIgnoreCase))
                {
                    Directory.CreateDirectory(StateDir);
                    File.WriteAllText(PowerStateFile, guid);
                }
                RunPowercfg("/setactive " + HighPerfGuid);
                return true;
            }
            catch { return false; }
        }

        public static bool RestorePowerPlan()
        {
            try
            {
                if (!File.Exists(PowerStateFile)) return false;
                var prev = File.ReadAllText(PowerStateFile).Trim();
                if (string.IsNullOrEmpty(prev)) return false;
                RunPowercfg("/setactive " + prev);
                File.Delete(PowerStateFile);
                return true;
            }
            catch { return false; }
        }

        /// <summary>Raise priority of the running Roblox client so it gets more CPU.</summary>
        public static int BoostRobloxPriority()
        {
            int n = 0;
            foreach (var name in new[] { "RobloxPlayerBeta", "Windows10Universal", "RobloxPlayerBeta_tcy" })
            {
                foreach (var p in SafeGetProcesses(name))
                {
                    try { p.PriorityClass = ProcessPriorityClass.High; n++; }
                    catch { /* may be denied without admin; ignore */ }
                }
            }
            return n;
        }

        /// <summary>Trim working sets to free standby RAM. Best-effort across accessible processes.</summary>
        public static long FreeMemory()
        {
            long trimmed = 0;
            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    if (K32EmptyWorkingSet(p.Handle) != 0) trimmed++;
                }
                catch { /* access denied on some processes; ignore */ }
                finally { try { p.Dispose(); } catch { } }
            }
            return trimmed;
        }

        /// <summary>Closes heavy background apps. Only called when the user opts in.</summary>
        public static int CloseBackgroundApps()
        {
            int n = 0;
            foreach (var name in BackgroundApps)
            {
                foreach (var p in SafeGetProcesses(name))
                {
                    try { p.CloseMainWindow(); if (!p.WaitForExit(1500)) p.Kill(); n++; }
                    catch { }
                }
            }
            return n;
        }

        private static Process[] SafeGetProcesses(string name)
        {
            try { return Process.GetProcessesByName(name); }
            catch { return new Process[0]; }
        }

        private static string RunPowercfg(string args)
        {
            var psi = new ProcessStartInfo("powercfg", args)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (var p = Process.Start(psi))
            {
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit(4000);
                return output;
            }
        }

        private static string ExtractGuid(string powercfgOutput)
        {
            if (string.IsNullOrEmpty(powercfgOutput)) return null;
            foreach (var token in powercfgOutput.Split(' ', '\r', '\n', '\t'))
            {
                Guid g;
                if (Guid.TryParse(token, out g)) return token;
            }
            return null;
        }
    }
}
