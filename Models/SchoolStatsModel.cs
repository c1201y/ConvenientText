using CommunityToolkit.Mvvm.ComponentModel;

namespace ConvenientText.Models;

public class SchoolStatsModel : ObservableObject
{
    private int _totalSchoolDays;
    private int _passedSchoolDays;
    private int _remainingSchoolDays;
    private double _progress;
    private string _todayInfo = string.Empty;
    private string _daysUntilBreak = string.Empty;
    private string _semesterRange = string.Empty;
    private bool _isTodaySchoolDay;

    public int TotalSchoolDays
    {
        get => _totalSchoolDays;
        set => SetProperty(ref _totalSchoolDays, value);
    }

    public int PassedSchoolDays
    {
        get => _passedSchoolDays;
        set => SetProperty(ref _passedSchoolDays, value);
    }

    public int RemainingSchoolDays
    {
        get => _remainingSchoolDays;
        set => SetProperty(ref _remainingSchoolDays, value);
    }

    public double Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }

    public string TodayInfo
    {
        get => _todayInfo;
        set => SetProperty(ref _todayInfo, value);
    }

    public string DaysUntilBreak
    {
        get => _daysUntilBreak;
        set => SetProperty(ref _daysUntilBreak, value);
    }

    public string SemesterRange
    {
        get => _semesterRange;
        set => SetProperty(ref _semesterRange, value);
    }

    public bool IsTodaySchoolDay
    {
        get => _isTodaySchoolDay;
        set => SetProperty(ref _isTodaySchoolDay, value);
    }
}
