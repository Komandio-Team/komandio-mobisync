using System.Windows;
using Wpf.Ui.Controls;

namespace Komandio.Tools.Mobisync.Components.Shared;

public partial class StatCard : UserControl
{
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register("Icon", typeof(SymbolRegular), typeof(StatCard));

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register("Label", typeof(string), typeof(StatCard));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register("Value", typeof(string), typeof(StatCard));

    public static readonly DependencyProperty IconColorProperty =
        DependencyProperty.Register("IconColor", typeof(Brush), typeof(StatCard));

    public StatCard()
    {
        InitializeComponent();
    }

    public SymbolRegular Icon
    {
        get => (SymbolRegular)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public Brush IconColor
    {
        get => (Brush)GetValue(IconColorProperty);
        set => SetValue(IconColorProperty, value);
    }
}