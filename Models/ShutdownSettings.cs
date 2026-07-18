using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Runtime.CompilerServices;

namespace ConvenientText.Models;

public class ShutdownSettings : INotifyPropertyChanged
{
    private int _hours;
    private int _minutes = 30;
    private int _delay = 180;
    private int _reminder = 60;
    private bool _noBeep;
    private bool _noShake;
    private bool _force;
    private bool _hideCancel;

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

    public int Delay
    {
        get => _delay;
        set { _delay = value; OnPropertyChanged(); }
    }

    public int Reminder
    {
        get => _reminder;
        set { _reminder = value; OnPropertyChanged(); }
    }

    public bool NoBeep
    {
        get => _noBeep;
        set { _noBeep = value; OnPropertyChanged(); }
    }

    public bool NoShake
    {
        get => _noShake;
        set { _noShake = value; OnPropertyChanged(); }
    }

    public bool Force
    {
        get => _force;
        set { _force = value; OnPropertyChanged(); }
    }

    public bool HideCancel
    {
        get => _hideCancel;
        set { _hideCancel = value; OnPropertyChanged(); }
    }

    [JsonIgnore]
    public int TotalSeconds => Hours * 3600 + Minutes * 60;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
