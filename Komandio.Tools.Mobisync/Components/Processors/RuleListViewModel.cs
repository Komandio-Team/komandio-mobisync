using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Komandio.Tools.Mobisync.Models;
using Komandio.Tools.Mobisync.Services;

namespace Komandio.Tools.Mobisync.Components.Processors;

public partial class RuleListViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    public ObservableCollection<DynamicProcessorRule> Rules { get; } = new();
    [ObservableProperty] private DynamicProcessorRule? _selectedRule;

    public RuleListViewModel(ISettingsService settings)
    {
        _settings = settings;
        foreach (var rule in _settings.CustomProcessors) Rules.Add(rule);

        WeakReferenceMessenger.Default.Register<string>(this, (r, m) =>
        {
            if (m == "RuleDeleted")
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Refresh from settings
                    Rules.Clear();
                    foreach (var rule in _settings.CustomProcessors) Rules.Add(rule);
                });
            }
        });
    }

    partial void OnSelectedRuleChanged(DynamicProcessorRule? value)
    {
        if (value != null)
        {
            WeakReferenceMessenger.Default.Send(value);
        }
    }

    [RelayCommand]
    private void AddRule()
    {
        var newRule = new DynamicProcessorRule { Name = "New Rule", Icon = "Heartpulse24" };
        Rules.Add(newRule);
        SelectedRule = newRule;
        _settings.CustomProcessors.Add(newRule);
        _settings.Save();
    }
}