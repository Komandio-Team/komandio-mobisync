using CommunityToolkit.Mvvm.ComponentModel;

namespace Komandio.Tools.Mobisync.Models;

public partial class DynamicProcessorRule : ObservableObject
{
    [ObservableProperty] private string _category = "CUSTOM";
    [ObservableProperty] private string _descriptionTemplate = "Captured: {1}";
    [ObservableProperty] private string _icon = "Activity24";
    [ObservableProperty] private string _id = Guid.NewGuid().ToString();
    [ObservableProperty] private bool _isBuiltIn;
    [ObservableProperty] private string _name = "New Rule";
    [ObservableProperty] private string _regex = "";
    [ObservableProperty] private string _titleTemplate = "Match Found";
}