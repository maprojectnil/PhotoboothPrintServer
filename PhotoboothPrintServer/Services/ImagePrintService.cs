using System.Drawing;
using System.Drawing.Printing;
using PhotoboothPrintServer.Models;
using PhotoboothPrintServer.Utilities;

namespace PhotoboothPrintServer.Services;

/// <summary>
/// Mencetak file gambar ke printer yang dipilih.
/// Fase 1/2: menggunakan konfigurasi default printer (profile == null).
/// Fase 3: menerapkan PrinterProfile (Paper Size, Quality, Borderless, Color, Orientation)
/// menggunakan kemampuan asli driver Windows printer tersebut - opsi yang tidak
/// didukung driver tidak akan dipaksakan.
/// Perbaikan physical-size printing: Paper Size (kertas fisik di printer) dan Print Size
/// (ukuran fisik gambar yang dicetak, mis. 4R = 102x152mm) sekarang independen.
/// ScalingMode menentukan bagaimana gambar disesuaikan (ActualSize / FitToPage /
/// FitToPrintableArea) - lihat PrintSizeCalculator untuk kalkulasi murninya.
/// </summary>
public class ImagePrintService
{
    public void PrintImage(string printerName, string imagePath, int copies, PrinterProfile? profile = null, Action<string>? log = null)
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

            doc.PrintPage += (sender, e) => DrawImagePage(e, image, profile, log);
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

    /// <summary>
    /// Rendering pipeline baru (perbaikan physical-size printing):
    /// PrintPage -&gt; tentukan target physical print size -&gt; convert mm -&gt; Graphics units
    /// -&gt; tentukan posisi -&gt; render pada ukuran target persis -&gt; (spooler/driver).
    ///
    /// Untuk ScalingMode.ActualSize, ukuran gambar TIDAK PERNAH diturunkan dari
    /// image.Width/Height, MarginBounds, atau PrintableArea - hanya dari
    /// PrinterProfile.PrintWidthMm/PrintHeightMm (lihat audit butir 5 &amp; 16).
    /// </summary>
    private static void DrawImagePage(PrintPageEventArgs e, Image image, PrinterProfile? profile, Action<string>? log)
    {
        if (e.Graphics == null) return;

        // Catat unit/DPI ASLI (untuk logging diagnostik - audit butir 14) SEBELUM diubah.
        GraphicsUnit originalPageUnit = e.Graphics.PageUnit;
        float dpiX = e.Graphics.DpiX;
        float dpiY = e.Graphics.DpiY;

        // PENTING: PrintPageEventArgs.PageBounds / MarginBounds / PageSettings.PrintableArea
        // SELALU dalam hundredths of an inch, terlepas dari Graphics.PageUnit yang sedang aktif.
        // Kita paksa PageUnit = Millimeter untuk rendering supaya koordinat DrawImage konsisten
        // dalam mm dan tidak pernah bergantung asumsi pixel = physical unit.
        e.Graphics.PageUnit = GraphicsUnit.Millimeter;

        var pageBoundsMm = ToMm(e.PageBounds);
        var marginBoundsMm = ToMm(e.MarginBounds);
        var printableAreaMm = ToMm(e.PageSettings.PrintableArea);

        double imageAspectRatio = image.Height == 0 ? 1.0 : (double)image.Width / image.Height;

        ScalingMode scaling = profile?.Scaling ?? ScalingMode.FitToPage;
        PrintPositionMode positionMode = profile?.Position ?? PrintPositionMode.Center;
        bool landscape = profile?.Landscape ?? false;

        bool hasPrintSize = profile != null && profile.PrintWidthMm > 0 && profile.PrintHeightMm > 0;

        if (scaling == ScalingMode.ActualSize && !hasPrintSize)
        {
            // Guard: ActualSize butuh Print Size fisik yang valid (bukan 0x0). Tanpa itu jangan
            // menebak ukuran - fallback aman ke FitToPage (behavior lama) dan beri warning.
            log?.Invoke("Warning: Scaling = Actual Size dipilih tapi Print Size belum diset (0x0mm). " +
                        "Fallback ke Fit to Page untuk cetak ini.");
            scaling = ScalingMode.FitToPage;
        }

        double targetWidthMm = 0, targetHeightMm = 0;
        if (hasPrintSize)
        {
            (targetWidthMm, targetHeightMm) = PrintSizeCalculator.GetOrientedTargetSize(
                profile!.PrintWidthMm, profile.PrintHeightMm, landscape);
        }

        var position = new PrintSizeCalculator.PositionParams(
            positionMode, profile?.CustomOffsetXMm ?? 0, profile?.CustomOffsetYMm ?? 0);

        var result = PrintSizeCalculator.Calculate(
            scaling, pageBoundsMm, marginBoundsMm, printableAreaMm,
            targetWidthMm, targetHeightMm, imageAspectRatio, position);

        if (result.ExceedsPrintableArea)
        {
            log?.Invoke(
                $"Warning: Target print size ({result.TargetWidthMm:0.0} x {result.TargetHeightMm:0.0} mm) " +
                "melebihi printable area printer. Sebagian gambar mungkin ter-clip oleh printer/driver " +
                "(ukuran Actual Size TIDAK dikecilkan oleh aplikasi).");
        }

        log?.Invoke(
            "Print - " +
            $"Paper: {e.PageSettings.PaperSize?.PaperName ?? "-"} | " +
            $"Print Size: {(string.IsNullOrWhiteSpace(profile?.PrintSizeName) ? "-" : profile!.PrintSizeName)} | " +
            $"Target: {result.TargetWidthMm:0.0} x {result.TargetHeightMm:0.0} mm | " +
            $"Scaling: {scaling} | Position: {positionMode} | " +
            $"Graphics PageUnit(orig): {originalPageUnit} | DPI: {dpiX:0}x{dpiY:0} | " +
            $"Calculated Draw Size: {result.DrawRect.Width:0.0} x {result.DrawRect.Height:0.0} mm | " +
            $"Printable Area: {printableAreaMm.Width:0.0} x {printableAreaMm.Height:0.0} mm");

        e.Graphics.DrawImage(
            image,
            (float)result.DrawRect.X,
            (float)result.DrawRect.Y,
            (float)result.DrawRect.Width,
            (float)result.DrawRect.Height);
    }

    /// <summary>PrintPageEventArgs.PageBounds/MarginBounds (Rectangle) selalu dalam 1/100 inci.</summary>
    private static PrintSizeCalculator.RectMm ToMm(Rectangle hundredthsInchRect) =>
        new(
            PrintSizeCalculator.HundredthsInchToMm(hundredthsInchRect.X),
            PrintSizeCalculator.HundredthsInchToMm(hundredthsInchRect.Y),
            PrintSizeCalculator.HundredthsInchToMm(hundredthsInchRect.Width),
            PrintSizeCalculator.HundredthsInchToMm(hundredthsInchRect.Height));

    /// <summary>PageSettings.PrintableArea (RectangleF) juga selalu dalam 1/100 inci.</summary>
    private static PrintSizeCalculator.RectMm ToMm(RectangleF hundredthsInchRect) =>
        new(
            PrintSizeCalculator.HundredthsInchToMm(hundredthsInchRect.X),
            PrintSizeCalculator.HundredthsInchToMm(hundredthsInchRect.Y),
            PrintSizeCalculator.HundredthsInchToMm(hundredthsInchRect.Width),
            PrintSizeCalculator.HundredthsInchToMm(hundredthsInchRect.Height));
}