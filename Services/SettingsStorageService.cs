using System;
using System.IO;
using System.Text.Json;
using ConvenientText.Models;

namespace ConvenientText.Services;

public class SettingsStorageService
{
    private readonly string _shutdownFile;
    private readonly string _statsFile;
    private readonly string _dutyFile;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public SettingsStorageService()
    {
        var pluginDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClassIsland", "Plugins", "ConvenientText");
        Directory.CreateDirectory(pluginDir);
        _shutdownFile = Path.Combine(pluginDir, "shutdown_settings.json");
        _statsFile = Path.Combine(pluginDir, "school_stats_settings.json");
        _dutyFile = Path.Combine(pluginDir, "duty_rota_settings.json");
    }

    public ShutdownSettings LoadShutdown()
    {
        return Load(_shutdownFile, () => new ShutdownSettings());
    }

    public void SaveShutdown(ShutdownSettings settings)
    {
        Save(_shutdownFile, settings);
    }

    public SchoolStatsSettings LoadSchoolStats()
    {
        return Load(_statsFile, () => new SchoolStatsSettings());
    }

    public void SaveSchoolStats(SchoolStatsSettings settings)
    {
        Save(_statsFile, settings);
    }

    public DutyRotaSettings LoadDutyRota()
    {
        return Load(_dutyFile, () => new DutyRotaSettings());
    }

    public void SaveDutyRota(DutyRotaSettings settings)
    {
        Save(_dutyFile, settings);
    }

    private T Load<T>(string path, Func<T> factory) where T : class
    {
        if (!File.Exists(path))
            return factory();
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json) ?? factory();
        }
        catch
        {
            return factory();
        }
    }

    private void Save<T>(string path, T data) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            File.WriteAllText(path, json);
        }
        catch { }
    }
}
