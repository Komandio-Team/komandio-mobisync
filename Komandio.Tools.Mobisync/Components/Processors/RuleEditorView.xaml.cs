using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Messaging;
using Komandio.Tools.Mobisync.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace Komandio.Tools.Mobisync.Components.Processors
{
    public partial class RuleEditorView : UserControl, IRecipient<ScrollToFirstMatchMessage>
    {
        public RuleEditorView()
        {
            InitializeComponent();
            var viewModel = App.Host?.Services.GetRequiredService<RuleEditorViewModel>();
            DataContext = viewModel;
            
            if (viewModel != null)
            {
                viewModel.TestBench.CollectionChanged += (s, e) => RefreshTestBench();
                viewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == "Rule" || e.PropertyName == "SelectedStyle" || e.PropertyName == "SelectedFontSize") 
                        RefreshTestBench();
                };
                
                RefreshTestBench();
            }

            WeakReferenceMessenger.Default.Register<ScrollToFirstMatchMessage>(this);
        }

        private void RefreshTestBench()
        {
            if (DataContext is not RuleEditorViewModel viewModel) return;

            Dispatcher.Invoke(() =>
            {
                var doc = new FlowDocument();
                doc.LineHeight = 1;
                doc.PageWidth = 2500; 
                
                var regexPattern = viewModel.Rule?.Regex;
                Regex? regex = null;
                if (!string.IsNullOrWhiteSpace(regexPattern))
                {
                    try { regex = new Regex(regexPattern, RegexOptions.IgnoreCase); } catch { }
                }

                foreach (var logLine in viewModel.TestBench)
                {
                    var p = new Paragraph { Margin = new Thickness(0, 0, 0, 1) };
                    var line = logLine.Content;

                    if (line.StartsWith("<") && line.Contains(">"))
                    {
                        int closingBracket = line.IndexOf(">");
                        var rawTs = line.Substring(0, closingBracket + 1);
                        line = line.Substring(closingBracket + 1);

                        var dt = logLine.Timestamp;
                        string formattedTs = rawTs;
                        string colorHex = "#475569"; 
                        string contentColorHex = "#94a3b8"; 
                        string spacer = "  "; // Added spacer for non-grid themes

                        switch (viewModel.SelectedStyle)
                        {
                            case TimestampStyle.ModernTerminal:
                                formattedTs = $"[{dt:HH:mm:ss}]";
                                colorHex = "#67e8f9"; 
                                contentColorHex = "#a5f3fc"; 
                                break;
                            case TimestampStyle.ClassicDev:
                                formattedTs = $"{dt:HH:mm:ss.fff} \u203a";
                                colorHex = "#fbbf24"; 
                                contentColorHex = "#fde68a"; 
                                break;
                            case TimestampStyle.CyberAudit:
                                formattedTs = $"({dt:HH:mm:ss})";
                                colorHex = "#c084fc"; 
                                contentColorHex = "#e9d5ff"; 
                                break;
                            case TimestampStyle.StealthPro:
                                formattedTs = $"{dt:yyyy-MM-dd HH:mm:ss}".PadRight(22); // Reduced from 28
                                colorHex = "#475569"; 
                                contentColorHex = "#94a3b8"; 
                                spacer = ""; // Uses its own padding
                                break;
                            case TimestampStyle.FullIso:
                                formattedTs = $"[{dt:yyyy-MM-dd HH:mm:ss}]";
                                colorHex = "#e2e8f0"; 
                                contentColorHex = "#ffffff";
                                break;
                        }

                        var tsRun = new Run(formattedTs)
                        {
                            Foreground = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex)),
                            FontSize = viewModel.SelectedFontSize,
                            FontFamily = new System.Windows.Media.FontFamily("Consolas")
                        };
                        p.Inlines.Add(tsRun);
                        if (!string.IsNullOrEmpty(spacer)) p.Inlines.Add(new Run(spacer));

                        var contentBrush = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(contentColorHex));
                        var highlightBrush = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#60a5fa"));
                        var highlightBg = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#1a60a5fa"));

                        if (regex != null)
                        {
                            int lastIndex = 0;
                            foreach (Match match in regex.Matches(line))
                            {
                                p.Inlines.Add(new Run(line.Substring(lastIndex, match.Index - lastIndex)) { Foreground = contentBrush, FontSize = viewModel.SelectedFontSize });
                                
                                var matchRun = new Run(match.Value)
                                {
                                    Background = highlightBg,
                                    Foreground = highlightBrush,
                                    FontWeight = FontWeights.Bold,
                                    FontSize = viewModel.SelectedFontSize
                                };
                                p.Inlines.Add(matchRun);
                                
                                lastIndex = match.Index + match.Length;
                            }
                            p.Inlines.Add(new Run(line.Substring(lastIndex)) { Foreground = contentBrush, FontSize = viewModel.SelectedFontSize });
                        }
                        else
                        {
                            p.Inlines.Add(new Run(line) { Foreground = contentBrush, FontSize = viewModel.SelectedFontSize });
                        }
                    }
                    else
                    {
                        p.Inlines.Add(new Run(line) { Foreground = Brushes.White, FontSize = viewModel.SelectedFontSize });
                    }

                    doc.Blocks.Add(p);
                }

                TestBenchBox.Document = doc;
            });
        }

        public void Receive(ScrollToFirstMatchMessage message)
        {
            RefreshTestBench();
        }
    }
}
