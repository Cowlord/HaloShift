using System;
using System.IO;
using System.Text.Json;

namespace HaloShift
{
    public class AppSettings
    {
        private const string SettingsFileName = "settings.json";

        public float MouseSensitivity { get; set; } = 0.5f;

        public static AppSettings Load()
        {
            try
            {
                var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HaloShift");
                var path = Path.Combine(directory, SettingsFileName);
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch
            {
            }

            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HaloShift");
                Directory.CreateDirectory(directory);
                var path = Path.Combine(directory, SettingsFileName);
                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(path, JsonSerializer.Serialize(this, options));
            }
            catch
            {
            }
        }
    }
}
