using Avalonia.Controls;
using Avalonia.Interactivity;

namespace uchebkaaa.Pages;

public partial class QualityRateDialog : Window
{
    public bool? ResultAcceptable { get; private set; }
    public string? ResultComment { get; private set; }

    public QualityRateDialog(string paramName, bool? current, string comment)
    {
        InitializeComponent();
        ParamTextBlock.Text = $"Параметр: {paramName}";
        CommentTextBox.Text = comment;

        PlusButton.Click += (_, _) =>
        {
            ResultAcceptable = true;
            ResultComment = null;
            Close();
        };
        MinusButton.Click += (_, _) =>
        {
            ResultAcceptable = false;
            ResultComment = CommentTextBox.Text?.Trim();
            Close();
        };
        OkButton.Click += (_, _) => Close();
    }
}
