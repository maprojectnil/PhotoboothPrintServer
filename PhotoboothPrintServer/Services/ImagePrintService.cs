using System.Drawing;
using System.Drawing.Printing;

namespace PhotoboothPrintServer.Services;

/// <summary>
/// Mencetak file gambar ke printer yang dipilih, menggunakan
/// konfigurasi default printer tersebut (dari Fase 1).
/// Gambar otomatis di-scale agar pas dengan area cetak (letterbox, tidak gepeng).
/// </summary>
public class ImagePrintService
{
    public void PrintImage(string printerName, string imagePath, int copies)
    {
        if (string.IsNullOrWhiteSpace(printerName))
            throw new InvalidOperationException("Tidak ada printer aktif yang dipilih di Print Server.");

        if (!File.Exists(imagePath))
            throw new FileNotFoundException("File foto tidak ditemukan di server.", imagePath);

        using var image = Image.FromFile(imagePath);

        int totalCopies = Math.Max(1, copies);

        for (int i = 0; i < totalCopies; i++)
        {
            using var doc = new PrintDocument();
            doc.PrinterSettings.PrinterName = printerName;

            if (!doc.PrinterSettings.IsValid)
            {
                throw new InvalidOperationException(
                    $"Printer '{printerName}' tidak tersedia / tidak terhubung.");
            }

            doc.PrintPage += (sender, e) => DrawImagePage(e, image);
            doc.Print();
        }
    }

    private static void DrawImagePage(PrintPageEventArgs e, Image image)
    {
        if (e.Graphics == null) return;

        var bounds = e.MarginBounds;

        double imageRatio = (double)image.Width / image.Height;
        double boundsRatio = (double)bounds.Width / bounds.Height;

        int drawWidth, drawHeight;
        if (imageRatio > boundsRatio)
        {
            drawWidth = bounds.Width;
            drawHeight = (int)(bounds.Width / imageRatio);
        }
        else
        {
            drawHeight = bounds.Height;
            drawWidth = (int)(bounds.Height * imageRatio);
        }

        int x = bounds.Left + (bounds.Width - drawWidth) / 2;
        int y = bounds.Top + (bounds.Height - drawHeight) / 2;

        e.Graphics.DrawImage(image, x, y, drawWidth, drawHeight);
    }
}