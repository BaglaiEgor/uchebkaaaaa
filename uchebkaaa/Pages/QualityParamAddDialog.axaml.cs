using Avalonia.Controls;
using Avalonia.Interactivity;

namespace uchebkaaa.Pages;

public partial class QualityParamAddDialog : Window
{
    public string ResultName { get; private set; } = "";

    public QualityParamAddDialog()
    {
        InitializeComponent();
        OkButton.Click += (_, _) =>
        {
            ResultName = NameTextBox.Text?.Trim() ?? "";
            Close();
        };
        CancelButton.Click += (_, _) => Close();
    }
}
