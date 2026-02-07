using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Komandio.Tools.Mobisync.Messages;
using Komandio.Tools.Mobisync.Models;

namespace Komandio.Tools.Mobisync.Components.Dashboard;

public partial class SecurityStatViewModel : ObservableObject
{
    [ObservableProperty] private bool _isInArmistice;
    [ObservableProperty] private string _value = "UNRESTRICTED";

    public SecurityStatViewModel()
    {
        WeakReferenceMessenger.Default.Register<GameEvent>(this, (r, m) =>
        {
            if (m is ArmisticeEvent a)
            {
                IsInArmistice = a.IsEntering;
                Value = IsInArmistice ? "ARMISTICE" : "UNRESTRICTED";
            }
        });

        WeakReferenceMessenger.Default.Register<ClearFeedsMessage>(this, (r, m) =>
        {
            IsInArmistice = false;
            Value = "UNRESTRICTED";
        });
    }
}