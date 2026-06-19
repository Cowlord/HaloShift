using System;
using System.IO;
using System.Text.Json;

namespace HaloShift
{
    public class AppSettings
    {
        private const string SettingsFileName = "settings.json";

        public float MouseSensitivity { get; set; } = 0.5f;
        public bool StartOnBoot { get; set; } = false;

        public static AppSettings Load()
        {
            try
            {
                var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HaloShift");
                var path = Path.Combine(directory, SettingsFileName);
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                    settings.Sanitize();
                    return settings;
                }
            }
            catch
            {
            }

            return new AppSettings();
        }

        private void Sanitize()
        {
            if (float.IsNaN(MouseSensitivity) || float.IsInfinity(MouseSensitivity))
                MouseSensitivity = 0.5f;
            MouseSensitivity = Math.Clamp(MouseSensitivity, 0.5f, 3.0f);
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
