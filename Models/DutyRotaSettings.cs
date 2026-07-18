using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ConvenientText.Models;

public class DutyRotaSettings : INotifyPropertyChanged
{
    private DateTime _startDate = DateTime.Today;
    private int _interval = 1;
    private List<string> _names = new();

    public DateTime StartDate
    {
        get => _startDate;
        set { _startDate = value; OnPropertyChanged(); }
    }

    public int Interval
    {
        get => _interval;
        set { _interval = Math.Max(1, value); OnPropertyChanged(); }
    }

    public List<string> Names
    {
        get => _names;
        set { _names = value; OnPropertyChanged(); }
    }

    public string GetTodayDuty()
    {
        if (_names.Count == 0) return "未设置值日生";
        var days = (DateTime.Today - _startDate.Date).Days;
        if (days < 0) return _names[0];
        var slot = days / _interval;
        var index = ((slot % _names.Count) + _names.Count) % _names.Count;
        return _names[index];
    }

    public string GetDutyForDate(DateTime date)
    {
        if (_names.Count == 0) return "未设置值日生";
        var days = (date.Date - _startDate.Date).Days;
        if (days < 0) return _names[0];
        var slot = days / _interval;
        var index = ((slot % _names.Count) + _names.Count) % _names.Count;
        return _names[index];
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
