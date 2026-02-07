using System.Windows;
using Wpf.Ui.Controls;

namespace Komandio.Tools.Mobisync.Components.Shared;

public partial class SummaryRow : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register("Label", typeof(string), typeof(SummaryRow));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register("Value", typeof(string), typeof(SummaryRow));

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register("Icon", typeof(SymbolRegular), typeof(SummaryRow));

    public static readonly DependencyProperty ColorProperty = DependencyProperty.Register("Color", typeof(Brush),
        typeof(SummaryRow), new PropertyMetadata(Brushes.White));

    public SummaryRow()
    {
        InitializeComponent();
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

    public SymbolRegular Icon
    {
        get => (SymbolRegular)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public Brush Color
    {
        get => (Brush)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }
}