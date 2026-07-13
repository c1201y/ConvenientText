using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ConvenientText.Models;

public class ShutdownSettings : INotifyPropertyChanged
{
    private int _hours;
    private int _minutes = 30;

    public int Hours
    {
        get => _hours;
        set { _hours = value; OnPropertyChanged(); }
    }

    public int Minutes
    {
        get => _minutes;
        set { _minutes = value; OnPropertyChanged(); }
    }

    public int TotalSeconds => Hours * 3600 + Minutes * 60;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
