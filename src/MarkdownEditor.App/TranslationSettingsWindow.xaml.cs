using System.Windows;
using System.Windows.Controls;

namespace MarkdownEditor.App;

public partial class TranslationSettingsWindow : Window
{
    public string SourceLanguageCode { get; private set; }
    public string DestinationLanguageCode { get; private set; }

    public TranslationSettingsWindow(string sourceLanguageCode, string destinationLanguageCode)
    {
        InitializeComponent();
        SourceLanguageCode = sourceLanguageCode;
        DestinationLanguageCode = destinationLanguageCode;
        Select(SourceLanguage, sourceLanguageCode);
        Select(DestinationLanguage, destinationLanguageCode);
    }

    private static void Select(ComboBox comboBox, string code) =>
        comboBox.SelectedItem = comboBox.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(item => Equals(item.Tag, code)) ?? comboBox.Items[0];

    private void Save(object sender, RoutedEventArgs e)
    {
        SourceLanguageCode = (SourceLanguage.SelectedItem as ComboBoxItem)?.Tag as string ?? "en";
        DestinationLanguageCode =
            (DestinationLanguage.SelectedItem as ComboBoxItem)?.Tag as string ?? "th";
        DialogResult = true;
    }
}
