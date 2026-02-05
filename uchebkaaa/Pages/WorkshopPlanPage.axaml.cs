using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using uchebkaaa.Services;

namespace uchebkaaa.Pages;

public partial class WorkshopPlanPage : UserControl
{
    private string? _currentWorkshop;
    private readonly List<(string Type, double X, double Y)> _icons = new();
    private string? _dragIconType;

    public WorkshopPlanPage()
    {
        InitializeComponent();

        BackButton.Click += (_, _) => MainWindow.NavigateTo(new DirectorScreen());
        LogoutButton.Click += (_, _) => { MainWindow.Logout(); MainWindow.NavigateTo(new LoginPage()); };
        WorkshopComboBox.SelectionChanged += WorkshopComboBox_SelectionChanged;
        ZoomSlider.ValueChanged += (_, _) => RenderPlan();
        SaveButton.Click += SaveButton_Click;
        CancelButton.Click += CancelButton_Click;

        IconEquipment.PointerPressed += (s, e) => StartDrag("Equipment", e);
        IconFireExt.PointerPressed += (s, e) => StartDrag("FireExtinguisher", e);
        IconFirstAid.PointerPressed += (s, e) => StartDrag("FirstAid", e);
        IconExit.PointerPressed += (s, e) => StartDrag("Exit", e);

        PlanCanvas.PointerPressed += PlanCanvas_PointerPressed;

        LoadWorkshops();
    }

    private void LoadWorkshops()
    {
        WorkshopComboBox.ItemsSource = WorkshopLayoutStorage.WorkshopNames.ToList();
        if (WorkshopLayoutStorage.WorkshopNames.Count > 0)
            WorkshopComboBox.SelectedIndex = 0;
    }

    private void WorkshopComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (WorkshopComboBox.SelectedItem is string name)
        {
            _currentWorkshop = name;
            LoadIcons();
            RenderPlan();
        }
    }

    private void LoadIcons()
    {
        _icons.Clear();
        if (string.IsNullOrEmpty(_currentWorkshop)) return;
        foreach (var icon in WorkshopLayoutStorage.LoadIcons(_currentWorkshop))
            _icons.Add(icon);
    }

    private void StartDrag(string type, PointerPressedEventArgs e)
    {
        _dragIconType = type;
    }

    private void PlanCanvas_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var pos = e.GetPosition(PlanCanvas);
        if (_dragIconType != null)
        {
            _icons.Add((_dragIconType, pos.X, pos.Y));
            _dragIconType = null;
            RenderPlan();
        }
        else
        {
            var hit = HitTest(pos.X, pos.Y);
            if (hit >= 0)
            {
                _icons.RemoveAt(hit);
                RenderPlan();
            }
        }
    }

    private int HitTest(double x, double y)
    {
        const double size = 32;
        for (int i = _icons.Count - 1; i >= 0; i--)
        {
            var (_, ix, iy) = _icons[i];
            if (x >= ix - size / 2 && x <= ix + size / 2 && y >= iy - size / 2 && y <= iy + size / 2)
                return i;
        }
        return -1;
    }

    private void RenderPlan()
    {
        PlanCanvas.Children.Clear();
        var zoom = ZoomSlider.Value;
        PlanCanvas.Width = 600 * zoom;
        PlanCanvas.Height = 500 * zoom;

        if (!string.IsNullOrEmpty(_currentWorkshop))
        {
            var uri = WorkshopLayoutStorage.GetPlanImageUri(_currentWorkshop);
            try
            {
                using var stream = AssetLoader.Open(new Uri(uri));
                var bitmap = new Bitmap(stream);
                var img = new Image
                {
                    Source = bitmap,
                    Width = PlanCanvas.Width,
                    Height = PlanCanvas.Height,
                    Stretch = Stretch.Uniform
                };
                PlanCanvas.Children.Add(img);
            }
            catch { }
        }

        foreach (var (type, x, y) in _icons)
        {
            var label = type switch
            {
                "Equipment" => "Оборуд.",
                "FireExtinguisher" => "Огнетуш.",
                "FirstAid" => "Аптечка",
                "Exit" => "Выход",
                _ => "?"
            };
            var brd = new Border
            {
                Width = 32,
                Height = 32,
                Background = new SolidColorBrush(Color.FromRgb(72, 121, 172)),
                CornerRadius = new CornerRadius(4),
                Child = new TextBlock
                {
                    Text = label,
                    FontSize = 8,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            Canvas.SetLeft(brd, x - 16);
            Canvas.SetTop(brd, y - 16);
            PlanCanvas.Children.Add(brd);
        }
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentWorkshop)) return;
        WorkshopLayoutStorage.SaveIcons(_currentWorkshop, _icons);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        LoadIcons();
        RenderPlan();
    }
}
