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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ConvenientText;

[PluginEntrance]
public class Plugin : PluginBase
{
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        services.AddSingleton<TextDataModel>();
        services.AddSingleton<DataStorageService>();
        services.AddComponent<ConvenientTextComponent, ConvenientTextSettingsControl>();
        services.AddHostedService<FloatingWindowHostedService>();
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