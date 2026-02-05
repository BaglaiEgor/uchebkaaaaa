using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using uchebkaaa.Data;

namespace uchebkaaa.Pages;

public partial class InventoryReportPage : UserControl
{
    private readonly string _role;

    public InventoryReportPage()
    {
        InitializeComponent();
        _role = App.CurrentUser?.Role ?? "";
        BackButton.Click += (_, _) =>
        {
            MainWindow.NavigateTo(_role == "Директор" ? (UserControl)new DirectorScreen() : new ManagerScreen());
        };
        LogoutButton.Click += (_, _) => { MainWindow.Logout(); MainWindow.NavigateTo(new LoginPage()); };
        TypeComboBox.SelectedIndex = 0;
        TypeComboBox.SelectionChanged += (_, _) => LoadProductTypes();
        LoadProductTypes();
        BuildButton.Click += BuildButton_Click;
        PrintButton.Click += PrintButton_Click;
    }

    private void PrintButton_Click(object? sender, RoutedEventArgs e)
    {
        if (ReportDataGrid == null)
            return;

        PrintDataGridToPdf(ReportDataGrid, "Отчет по материалам и комплектующим");
    }

    private void PrintDataGridToPdf(DataGrid grid, string reportTitle)
    {
        if (grid.ItemsSource == null)
            return;

        using var document = new PdfDocument();
        var page = document.AddPage();
        page.Size = PdfSharpCore.PageSize.A4;
        using var gfx = XGraphics.FromPdfPage(page);

        double margin = 40;
        double yPoint = margin;

        var titleFont = new XFont("Arial", 18, XFontStyle.Bold);
        gfx.DrawString(reportTitle, titleFont, XBrushes.Black, new XRect(0, yPoint, page.Width, 30),
            XStringFormats.TopCenter);
        yPoint += 40;

        var headerFont = new XFont("Arial", 12, XFontStyle.Bold);
        double x = margin;
        foreach (var col in grid.Columns)
        {
            if (col is DataGridTextColumn textCol)
            {
                double width = textCol.Width.IsStar ? 100 : textCol.Width.Value;
                gfx.DrawString(textCol.Header.ToString(), headerFont, XBrushes.Black,
                    new XRect(x, yPoint, width, 20), XStringFormats.TopLeft);
                x += width;
            }
        }
        yPoint += 25;

        var rowFont = new XFont("Arial", 12, XFontStyle.Regular);

        XGraphics gfxPage = gfx;

        foreach (var item in grid.ItemsSource)
        {
            x = margin;
            foreach (var col in grid.Columns)
            {
                if (col is DataGridTextColumn textCol)
                {
                    double width = textCol.Width.IsStar ? 100 : textCol.Width.Value;

                    var bindingPath = (textCol.Binding as Avalonia.Data.Binding)?.Path ?? "";
                    var value = item.GetType().GetProperty(bindingPath)?.GetValue(item)?.ToString() ?? "";
                    gfxPage.DrawString(value, rowFont, XBrushes.Black,
                        new XRect(x, yPoint, width, 20), XStringFormats.TopLeft);
                    x += width;
                }
            }

            yPoint += 20;

            if (yPoint > page.Height - 50)
            {
                page = document.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                gfxPage = XGraphics.FromPdfPage(page);
                yPoint = margin;
            }
        }

        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Report.pdf");
        document.Save(path);

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    private void LoadProductTypes()
    {
        var tag = (TypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "m";
        var types = tag == "m"
            ? App.DbContext.Materials.Select(m => m.ProductType).Distinct().ToList()
            : App.DbContext.Accessories.Select(a => a.ProductType).Distinct().ToList();
        types.Insert(0, "Все типы");
        ProductTypeComboBox.ItemsSource = types;
        ProductTypeComboBox.SelectedIndex = 0;
    }

    private void BuildButton_Click(object? sender, RoutedEventArgs e)
    {
        var tag = (TypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "m";
        var filterType = ProductTypeComboBox.SelectedItem as string;
        var isAllTypes = filterType == "Все типы";

        var rows = new List<object>();

        if (tag == "m")
        {
            var materials = App.DbContext.Materials.ToDictionary(m => m.Article);
            foreach (var mw in App.DbContext.MaterialWarehouses.Include(m => m.Warehouse))
            {
                if (!materials.TryGetValue(mw.MaterialArticle, out var m)) continue;
                if (!isAllTypes && m.ProductType != filterType) continue;
                rows.Add(new { Warehouse = mw.Warehouse.Name, Article = m.Article, Name = m.Name, ProductType = m.ProductType, Quantity = mw.Quantity });
            }
        }
        else
        {
            var accessories = App.DbContext.Accessories.ToDictionary(a => a.Article);
            foreach (var cw in App.DbContext.ComponentWarehouses.Include(c => c.Warehouse))
            {
                if (!accessories.TryGetValue(cw.ComponentArticle, out var a)) continue;
                if (!isAllTypes && a.ProductType != filterType) continue;
                rows.Add(new { Warehouse = cw.Warehouse.Name, Article = a.Article, Name = a.Name, ProductType = a.ProductType, Quantity = cw.Quantity });
            }
        }

        rows = rows.OrderBy(r => ((dynamic)r).Warehouse).ThenBy(r => ((dynamic)r).Name).ToList();
        ReportDataGrid.ItemsSource = rows;
    }
}
