using System.Drawing;
using System.Drawing.Printing;

namespace PhotoboothPrintServer.Services;

public class TestPrintResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class TestPrintService
{
    public TestPrintResult PrintTestPage(string printerName)
    {
        if (string.IsNullOrWhiteSpace(printerName))
        {
            return new TestPrintResult { Success = false, Message = "Tidak ada printer yang dipilih." };
        }

        try
        {
            using var doc = new PrintDocument();
            doc.PrinterSettings.PrinterName = printerName;

            if (!doc.PrinterSettings.IsValid)
            {
                return new TestPrintResult
                {
                    Success = false,
                    Message = $"Printer '{printerName}' tidak tersedia / tidak terhubung."
                };
            }

            doc.PrintPage += (sender, e) => DrawTestPage(e);
            doc.Print();

            return new TestPrintResult
            {
                Success = true,
                Message = $"Test print berhasil dikirim ke '{printerName}'."
            };
        }
        catch (Exception ex)
        {
            return new TestPrintResult
            {
                Success = false,
                Message = $"Test print gagal: {ex.Message}"
            };
        }
    }

    private void DrawTestPage(PrintPageEventArgs e)
    {
        if (e.Graphics == null) return;

        var g = e.Graphics;
        var bounds = e.MarginBounds;

        using var titleFont = new Font("Segoe UI", 18, FontStyle.Bold);
        using var normalFont = new Font("Segoe UI", 10);
        using var pen = new Pen(Color.Black, 2);

        g.DrawRectangle(pen, bounds);

        g.DrawString("Photobooth Print Server", titleFont, Brushes.Black, bounds.Left + 20, bounds.Top + 20);
        g.DrawString("Test Print Page", normalFont, Brushes.Black, bounds.Left + 20, bounds.Top + 60);
        g.DrawString($"Printed at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", normalFont, Brushes.Black, bounds.Left + 20, bounds.Top + 80);

        int barWidth = 60;
        int barHeight = 40;
        int startX = bounds.Left + 20;
        int startY = bounds.Top + 120;

        Color[] colors = { Color.Red, Color.Green, Color.Blue, Color.Yellow, Color.Black, Color.Cyan, Color.Magenta };
        for (int i = 0; i < colors.Length; i++)
        {
            using var brush = new SolidBrush(colors[i]);
            int x = startX + i * (barWidth + 5);
            g.FillRectangle(brush, x, startY, barWidth, barHeight);
            g.DrawRectangle(Pens.Black, x, startY, barWidth, barHeight);
        }

        g.DrawString("Jika teks ini terbaca jelas dan warna di atas terlihat benar,",
            normalFont, Brushes.Black, bounds.Left + 20, startY + barHeight + 30);
        g.DrawString("printer sudah terhubung dengan baik ke Photobooth Print Server.",
            normalFont, Brushes.Black, bounds.Left + 20, startY + barHeight + 50);
    }
}