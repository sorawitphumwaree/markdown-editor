using CommunityToolkit.Mvvm.ComponentModel;
using MarkdownEditor.Domain.Documents;

namespace MarkdownEditor.App.ViewModels;

public sealed partial class DocumentTabViewModel : ObservableObject
{
    public DocumentTabViewModel(Document document) => Model = document;

    public Document Model { get; }
    public Guid Id => Model.Id.Value;
    public string FilePath => Model.FilePath;
    public string DisplayName => Model.DisplayName;
    public string Header => Model.IsModified ? $"{Model.DisplayName} •" : Model.DisplayName;

    public void ReplaceContent(string content)
    {
        Model.ReplaceContent(content);
        NotifyContentChanged();
    }

    public void NotifyContentChanged()
    {
        OnPropertyChanged(nameof(Header));
    }

    public void MarkSaved()
    {
        OnPropertyChanged(nameof(FilePath));
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(Header));
    }

    public void MarkReloaded()
    {
        OnPropertyChanged(nameof(Header));
    }
}
