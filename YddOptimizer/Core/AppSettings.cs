using System;
using System.IO;

namespace YddOptimizer.Core
{
    public class AppSettings
    {
        public string Language { get; set; } = "tr";
        public string Gta5Path { get; set; } = "";
        public string CodeWalkerPath { get; set; } = "";

        private static readonly string SettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.cfg");

        public static AppSettings Load()
        {
            var settings = new AppSettings();
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var lines = File.ReadAllLines(SettingsPath);
                    foreach (var line in lines)
                    {
                        int eq = line.IndexOf('=');
                        if (eq <= 0) continue;
                        string key = line.Substring(0, eq).Trim();
                        string val = line.Substring(eq + 1).Trim();

                        if (key.Equals("Language", StringComparison.OrdinalIgnoreCase))
                            settings.Language = val;
                        else if (key.Equals("Gta5Path", StringComparison.OrdinalIgnoreCase))
                            settings.Gta5Path = val;
                        else if (key.Equals("CodeWalkerPath", StringComparison.OrdinalIgnoreCase))
                            settings.CodeWalkerPath = val;
                    }
                }
            }
            catch { }
            return settings;
        }

        public void Save()
        {
            try
            {
                using (var sw = new StreamWriter(SettingsPath, false))
                {
                    sw.WriteLine("Language=" + (Language ?? "tr"));
                    sw.WriteLine("Gta5Path=" + (Gta5Path ?? ""));
                    sw.WriteLine("CodeWalkerPath=" + (CodeWalkerPath ?? ""));
                }
            }
            catch { }
        }
    }
}
