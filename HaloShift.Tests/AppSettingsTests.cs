using System;
using System.IO;
using System.Text.Json;
using Xunit;

namespace HaloShift.Tests
{
    public class AppSettingsTests : IDisposable
    {
        private readonly string _testDir;

        public AppSettingsTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), $"HaloShiftTest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, true);
        }

        [Fact]
        public void Default_MouseSensitivity_IsHalf()
        {
            var settings = new AppSettings();
            Assert.Equal(0.5f, settings.MouseSensitivity);
        }

        [Fact]
        public void Default_StartOnBoot_IsFalse()
        {
            var settings = new AppSettings();
            Assert.False(settings.StartOnBoot);
        }

        [Fact]
        public void MouseSensitivity_CanBeSet()
        {
            var settings = new AppSettings { MouseSensitivity = 2.0f };
            Assert.Equal(2.0f, settings.MouseSensitivity);
        }

        [Fact]
        public void StartOnBoot_CanBeSet()
        {
            var settings = new AppSettings { StartOnBoot = true };
            Assert.True(settings.StartOnBoot);
        }

        [Fact]
        public void Serialization_RoundTrips()
        {
            var original = new AppSettings
            {
                MouseSensitivity = 1.7f,
                StartOnBoot = true
            };

            var json = JsonSerializer.Serialize(original, new JsonSerializerOptions { WriteIndented = true });
            var deserialized = JsonSerializer.Deserialize<AppSettings>(json);

            Assert.NotNull(deserialized);
            Assert.Equal(original.MouseSensitivity, deserialized!.MouseSensitivity);
            Assert.Equal(original.StartOnBoot, deserialized.StartOnBoot);
        }

        [Fact]
        public void Deserialize_EmptyJson_ReturnsDefaults()
        {
            var deserialized = JsonSerializer.Deserialize<AppSettings>("{}");

            Assert.NotNull(deserialized);
            Assert.Equal(0.5f, deserialized!.MouseSensitivity);
            Assert.False(deserialized.StartOnBoot);
        }

        [Fact]
        public void Deserialize_PartialJson_UsesDefaults()
        {
            var json = "{\"MouseSensitivity\": 2.5}";
            var deserialized = JsonSerializer.Deserialize<AppSettings>(json);

            Assert.NotNull(deserialized);
            Assert.Equal(2.5f, deserialized!.MouseSensitivity);
            Assert.False(deserialized.StartOnBoot);
        }

        [Fact]
        public void Load_WhenNoFileExists_ReturnsDefaults()
        {
            var settings = AppSettings.Load();
            Assert.Equal(0.5f, settings.MouseSensitivity);
            Assert.False(settings.StartOnBoot);
        }

        [Fact]
        public void Save_CreatesDirectoryAndFile()
        {
            var settings = new AppSettings { MouseSensitivity = 1.5f };
            settings.Save();

            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HaloShift");
            var path = Path.Combine(directory, "settings.json");

            Assert.True(File.Exists(path));

            // Clean up
            if (File.Exists(path))
                File.Delete(path);
        }

        [Fact]
        public void SaveAndLoad_RoundTrips()
        {
            var original = new AppSettings
            {
                MouseSensitivity = 2.3f,
                StartOnBoot = true
            };
            original.Save();

            var loaded = AppSettings.Load();

            Assert.Equal(original.MouseSensitivity, loaded.MouseSensitivity);
            Assert.Equal(original.StartOnBoot, loaded.StartOnBoot);

            // Clean up
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HaloShift");
            var path = Path.Combine(directory, "settings.json");
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
