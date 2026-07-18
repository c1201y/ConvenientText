using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ConvenientText.Models;

namespace ConvenientText.Services;

public class HolidayService
{
    private readonly string _cacheDir;
    private readonly string _cacheFile;
    private readonly JsonSerializerOptions _saveOptions = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true };
    private HolidayData? _currentData;

    public HolidayService()
    {
        var pluginDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClassIsland", "Plugins", "ConvenientText");
        Directory.CreateDirectory(pluginDir);
        _cacheDir = pluginDir;
        _cacheFile = Path.Combine(_cacheDir, "holidays.json");
    }

    public HolidayData? CurrentData => _currentData;

    public HolidayData Load()
    {
        if (File.Exists(_cacheFile))
        {
            try
            {
                var json = File.ReadAllText(_cacheFile);
                _currentData = JsonSerializer.Deserialize<HolidayData>(json);
                if (_currentData != null) return _currentData;
            }
            catch { }
        }

        _currentData = new HolidayData { Year = DateTime.Now.Year };
        return _currentData;
    }

    public void Save()
    {
        if (_currentData == null) return;
        try
        {
            var json = JsonSerializer.Serialize(_currentData, _saveOptions);
            File.WriteAllText(_cacheFile, json);
        }
        catch { }
    }

    public string LastError { get; private set; } = "";

    public async Task<bool> UpdateFromApiAsync(int? year = null)
    {
        var targetYear = year ?? DateTime.Now.Year;
        try
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromSeconds(15);
            http.DefaultRequestHeaders.Accept.Clear();
            http.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            var url = $"https://timor.tech/api/holiday/year/{targetYear}/";
            LastError = $"请求: {url}";
            var response = await http.GetAsync(url);
            LastError = $"状态: {response.StatusCode}";
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            LastError = $"收到 {json.Length} 字符";

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("code", out var code) || code.GetInt32() != 0)
            {
                LastError = $"API code != 0: {code}";
                return false;
            }

            if (!doc.RootElement.TryGetProperty("holiday", out var holidayObj))
                return false;

            var data = new HolidayData
            {
                Year = targetYear,
                LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            };

            foreach (var prop in holidayObj.EnumerateObject())
            {
                var item = prop.Value;
                var dateStr = item.TryGetProperty("date", out var dateEl) ? dateEl.GetString() ?? "" : "";
                if (string.IsNullOrEmpty(dateStr)) continue;

                var name = item.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "" : "";
                var holiday = item.TryGetProperty("holiday", out var holEl) && holEl.GetBoolean();

                data.Holidays.Add(new HolidayEntry
                {
                    Date = dateStr,
                    Name = name,
                    Type = holiday ? "holiday" : "makeup"
                });
            }

            _currentData = data;
            Save();
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
    }

    public bool IsHoliday(DateTime date)
    {
        if (_currentData == null) return false;
        var dateStr = date.ToString("yyyy-MM-dd");
        return _currentData.Holidays.Exists(h => h.Date == dateStr && h.Type == "holiday");
    }

    public bool IsMakeupDay(DateTime date)
    {
        if (_currentData == null) return false;
        var dateStr = date.ToString("yyyy-MM-dd");
        return _currentData.Holidays.Exists(h => h.Date == dateStr && h.Type == "makeup");
    }

    public bool IsSchoolDay(DateTime date)
    {
        if (IsHoliday(date)) return false;
        if (IsMakeupDay(date)) return true;

        var dayOfWeek = date.DayOfWeek;
        return dayOfWeek != DayOfWeek.Saturday && dayOfWeek != DayOfWeek.Sunday;
    }

    public void AddManualEntry(string date, string name, string type)
    {
        _currentData ??= new HolidayData { Year = DateTime.Now.Year };
        _currentData.Holidays.RemoveAll(h => h.Date == date);
        _currentData.Holidays.Add(new HolidayEntry { Date = date, Name = name, Type = type });
        _currentData.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm") + " (手动)";
        Save();
    }

    public void RemoveManualEntry(string date)
    {
        if (_currentData == null) return;
        _currentData.Holidays.RemoveAll(h => h.Date == date);
        _currentData.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm") + " (手动)";
        Save();
    }

    public int CountSchoolDaysInRange(DateTime start, DateTime end)
    {
        int count = 0;
        var current = start.Date;
        while (current <= end.Date)
        {
            if (IsSchoolDay(current)) count++;
            current = current.AddDays(1);
        }
        return count;
    }
}
