using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ConvenientText.Models;

public class SchoolStatsSettings : INotifyPropertyChanged
{
    private DateTime _semesterStart = new(DateTime.Now.Year, 2, 20);
    private DateTime _semesterEnd = new(DateTime.Now.Year, 7, 10);
    private bool _isDetailedMode;
    private bool _autoUpdateHolidays = true;

    public DateTime SemesterStart
    {
        get => _semesterStart;
        set { _semesterStart = value; OnPropertyChanged(); }
    }

    public DateTime SemesterEnd
    {
        get => _semesterEnd;
        set { _semesterEnd = value; OnPropertyChanged(); }
    }

    public bool IsDetailedMode
    {
        get => _isDetailedMode;
        set { _isDetailedMode = value; OnPropertyChanged(); }
    }

    public bool AutoUpdateHolidays
    {
        get => _autoUpdateHolidays;
        set { _autoUpdateHolidays = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
