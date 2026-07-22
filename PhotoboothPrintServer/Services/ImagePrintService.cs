using System.Drawing;
using System.Drawing.Printing;
using PhotoboothPrintServer.Models;

namespace PhotoboothPrintServer.Services;

/// <summary>
/// Mencetak file gambar ke printer yang dipilih.
/// Fase 1/2: menggunakan konfigurasi default printer (profile == null).
/// Fase 3: menerapkan PrinterProfile (Paper Size, Quality, Borderless, Color, Orientation)
/// menggunakan kemampuan asli driver Windows printer tersebut - opsi yang tidak
/// didukung driver tidak akan dipaksakan.
/// Gambar otomatis di-scale agar pas dengan area cetak (letterbox, tidak gepeng).
/// </summary>
public class ImagePrintService
{
    public void PrintImage(string printerName, string imagePath, int copies, PrinterProfile? profile = null)
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

            ApplyProfile(doc, profile);

            doc.PrintPage += (sender, e) => DrawImagePage(e, image);
            doc.Print();
        }
    }

    /// <summary>
    /// Menerapkan Printer Profile ke PrintDocument. Setiap opsi hanya diterapkan jika
    /// benar-benar tersedia di driver printer yang aktif (tidak dipaksakan).
    /// </summary>
    private static void ApplyProfile(PrintDocument doc, PrinterProfile? profile)
    {
        if (profile == null) return; // fallback ke default driver (perilaku Fase 1/2)

        var settings = doc.PrinterSettings;
        var pageSettings = doc.DefaultPageSettings;

        // Paper Size - hanya diterapkan jika nama persis tersedia di driver.
        if (!string.IsNullOrWhiteSpace(profile.PaperSizeName))
        {
            foreach (PaperSize size in settings.PaperSizes)
            {
                if (string.Equals(size.PaperName, profile.PaperSizeName, StringComparison.OrdinalIgnoreCase))
                {
                    pageSettings.PaperSize = size;
                    break;
                }
            }
        }

        // Orientation.
        pageSettings.Landscape = profile.Landscape;

        // Color Mode - hanya diterapkan jika printer mendukung warna.
        if (settings.SupportsColor)
        {
            pageSettings.Color = profile.ColorMode;
        }

        // Print Quality - cari resolusi driver yang paling sesuai level yang dipilih.
        var matchedResolution = FindResolution(settings, profile.PrintQuality);
        if (matchedResolution != null)
        {
            pageSettings.PrinterResolution = matchedResolution;
        }

        // Borderless - best-effort dengan menghilangkan margin cetak.
        // Hasil akhir full-bleed tetap tergantung dukungan driver/paper size printer.
        if (profile.Borderless)
        {
            pageSettings.Margins = new Margins(0, 0, 0, 0);
        }
    }

    private static PrinterResolution? FindResolution(PrinterSettings settings, PrintQualityLevel level)
    {
        var resolutions = settings.PrinterResolutions.Cast<PrinterResolution>().ToList();
        if (resolutions.Count == 0) return null;

        var targetKind = level switch
        {
            PrintQualityLevel.High => PrinterResolutionKind.High,
            PrintQualityLevel.Normal => PrinterResolutionKind.Medium,
            PrintQualityLevel.Draft => PrinterResolutionKind.Draft,
            _ => PrinterResolutionKind.High
        };

        var exact = resolutions.FirstOrDefault(r => r.Kind == targetKind);
        if (exact != null) return exact;

        // Fallback jika driver tidak melaporkan Kind standar: urutkan berdasarkan DPI.
        var sortedByDpi = resolutions.OrderByDescending(r => r.X).ToList();
        return level switch
        {
            PrintQualityLevel.High => sortedByDpi.First(),
            PrintQualityLevel.Draft => sortedByDpi.Last(),
            _ => sortedByDpi[sortedByDpi.Count / 2]
        };
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