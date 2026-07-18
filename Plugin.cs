using ClassIsland.Core;
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Extensions.Registry;
using ConvenientText.Components;
using ConvenientText.Models;
using ConvenientText.Services;
using ConvenientText.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ConvenientText;

[PluginEntrance]
public class Plugin : PluginBase
{
    public static ShutdownSettings ShutdownSettings { get; } = new();
    public static SchoolStatsSettings SchoolStatsSettings { get; } = new();
    public static DutyRotaSettings DutyRotaSettings { get; } = new();
    public static HolidayService HolidayService { get; } = new();
    public static SettingsStorageService SettingsStorage { get; } = new();

    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        LoadSettings();
        SetupAutoSave();

        services.AddSingleton<TextDataModel>();
        services.AddSingleton<DataStorageService>();
        services.AddSingleton<SchoolStatsSettings>(SchoolStatsSettings);
        services.AddSingleton<DutyRotaSettings>(DutyRotaSettings);
        services.AddSingleton<HolidayService>(HolidayService);
        services.AddComponent<ConvenientTextComponent, ConvenientTextSettingsControl>();
        services.AddComponent<SchoolStatsComponent>();
        services.AddComponent<DutyRotaComponent>();
        services.AddSettingsPage<ShutdownSettingsControl>();
        services.AddAction("convenienttext.timedshutdown", "计时关机", MaterialDesignThemes.Wpf.PackIconKind.Power, (settings, param) =>
        {
            Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                var win = new ShutdownWindow(ShutdownSettings);
                win.Show();
            }));
        });
        services.AddHostedService<FloatingWindowHostedService>();
        services.AddHostedService<HolidayUpdateHostedService>();
    }

    private static void LoadSettings()
    {
        var saved = SettingsStorage.LoadShutdown();
        ShutdownSettings.Hours = saved.Hours;
        ShutdownSettings.Minutes = saved.Minutes;
        ShutdownSettings.Delay = saved.Delay;
        ShutdownSettings.Reminder = saved.Reminder;
        ShutdownSettings.NoBeep = saved.NoBeep;
        ShutdownSettings.NoShake = saved.NoShake;
        ShutdownSettings.Force = saved.Force;
        ShutdownSettings.HideCancel = saved.HideCancel;

        var statsSaved = SettingsStorage.LoadSchoolStats();
        SchoolStatsSettings.SemesterStart = statsSaved.SemesterStart;
        SchoolStatsSettings.SemesterEnd = statsSaved.SemesterEnd;
        SchoolStatsSettings.IsDetailedMode = statsSaved.IsDetailedMode;
        SchoolStatsSettings.AutoUpdateHolidays = statsSaved.AutoUpdateHolidays;

        var dutySaved = SettingsStorage.LoadDutyRota();
        DutyRotaSettings.StartDate = dutySaved.StartDate;
        DutyRotaSettings.Interval = dutySaved.Interval;
        DutyRotaSettings.Names = dutySaved.Names;
    }

    private static void SetupAutoSave()
    {
        ShutdownSettings.PropertyChanged += (_, _) => SettingsStorage.SaveShutdown(ShutdownSettings);
        SchoolStatsSettings.PropertyChanged += (_, _) => SettingsStorage.SaveSchoolStats(SchoolStatsSettings);
        DutyRotaSettings.PropertyChanged += (_, _) => SettingsStorage.SaveDutyRota(DutyRotaSettings);
    }

    private class HolidayUpdateHostedService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (!SchoolStatsSettings.AutoUpdateHolidays) return Task.CompletedTask;
            Application.Current?.Dispatcher.BeginInvoke(async () =>
            {
                try { await HolidayService.UpdateFromApiAsync(); }
                catch { HolidayService.Load(); }
            });
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private class FloatingWindowHostedService : IHostedService
    {
        private readonly TextDataModel _dataModel;
        private readonly DataStorageService _storage;
        private FloatingButton? _floatingButton;

        public FloatingWindowHostedService(TextDataModel dataModel, DataStorageService storage)
        {
            _dataModel = dataModel;
            _storage = storage;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                var saved = _storage.Load();
                _dataModel.DisplayText = saved.DisplayText;
                _dataModel.TextColor = saved.TextColor;
                _dataModel.FontSize = saved.FontSize;

                _floatingButton = new FloatingButton(_dataModel, _storage);

                var mainWindow = AppBase.Current?.MainWindow;
                if (mainWindow != null)
                {
                    _floatingButton.Topmost = mainWindow.Topmost;
                }

                _floatingButton.Show();
            }));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                _floatingButton?.Close();
                _floatingButton = null;
            }));
            return Task.CompletedTask;
        }
    }
}
