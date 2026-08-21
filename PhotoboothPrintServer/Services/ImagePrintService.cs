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

            ApplyProfile(doc, profile, image, log);

            doc.PrintPage += (sender, e) => DrawImagePage(e, image, profile, log);
            doc.Print();
        }
    }

    /// <summary>
    /// Menerapkan Printer Profile ke PrintDocument. Setiap opsi hanya diterapkan jika
    /// benar-benar tersedia di driver printer yang aktif (tidak dipaksakan).
    /// </summary>
    private static void ApplyProfile(PrintDocument doc, PrinterProfile? profile, Image image, Action<string>? log = null)
    {
        if (profile == null) return; // fallback ke default driver (perilaku Fase 1/2)

        var settings = doc.PrinterSettings;
        var pageSettings = doc.DefaultPageSettings;

        // REVISI (setelah dikonfirmasi lewat pengetesan nyata): untuk driver Epson (dan
        // kebanyakan printer foto consumer), toggle Borderless yang SESUNGGUHNYA tersimpan di
        // data privat/vendor-specific milik driver (DEVMODE extension) yang TIDAK terekspos
        // lewat System.Drawing.Printing sama sekali - HardMarginX/Y yang dibaca .NET untuk
        // Paper Size TIDAK berubah walau Borderless aktif/nonaktif di driver, jadi TIDAK BISA
        // dipakai sebagai sinyal "apakah driver ini sudah borderless". Auto-select di bawah
        // masih berguna untuk driver LAIN yang memang punya entri Paper Size terpisah untuk
        // borderless (mis. beberapa driver Canon/HP), tapi untuk Epson biasanya cuma ada SATU
        // entri per ukuran fisik - jadi jangan berharap ini "menyalakan" borderless sendirian.
        bool borderlessPaperAutoSelected = false;
        if (profile.Borderless && profile.PrintWidthMm > 0 && profile.PrintHeightMm > 0 &&
            TryFindBestBorderlessPaperSize(settings, profile.PrintWidthMm, profile.PrintHeightMm,
                out var autoPaper, out var autoHmX, out var autoHmY))
        {
            pageSettings.PaperSize = autoPaper;
            borderlessPaperAutoSelected = true;
            log?.Invoke(
                $"Borderless: Paper Size '{autoPaper!.PaperName}' dipakai untuk target " +
                $"{profile.PrintWidthMm:0.0} x {profile.PrintHeightMm:0.0} mm " +
                $"(hard margin terlapor {autoHmX:0.00} x {autoHmY:0.00} mm - " +
                "ANGKA INI TIDAK MENJAMIN status borderless sesungguhnya untuk driver Epson, " +
                "lihat catatan di bawah).");
        }

        // Paper Size manual - dipakai kalau bukan mode borderless (atau auto-select borderless
        // di atas gagal menemukan kandidat sama sekali), sama seperti perilaku lama.
        if (!borderlessPaperAutoSelected && !string.IsNullOrWhiteSpace(profile.PaperSizeName))
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

        // Orientation - Auto ditentukan dari aspect ratio gambar (lihat ResolveLandscape),
        // selain itu mengikuti pilihan manual Portrait/Landscape seperti sebelumnya.
        bool landscape = ResolveLandscape(profile, image);
        pageSettings.Landscape = landscape;

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
        // PENTING (dikonfirmasi dari pengetesan nyata): baris ini TIDAK PERNAH bisa menyalakan
        // borderless sesungguhnya di printer Epson - ia cuma menol-kan margin versi .NET/GDI,
        // sedangkan flag borderless asli Epson ada di data privat driver yang tidak tersentuh
        // sama sekali oleh System.Drawing.Printing. Baris Margins=0 ini dipertahankan karena
        // tidak merugikan (dan membantu driver lain yang memang membaca margin GDI), TAPI
        // satu-satunya cara borderless benar-benar aktif adalah menyalakannya PERMANEN sebagai
        // default di Windows: klik kanan printer > Printing Preferences > centang Borderless >
        // OK/Apply (di luar aplikasi ini). Kalau langkah itu belum dilakukan, print job akan
        // tetap punya hard margin fisik meski checkbox di app ini dicentang - lihat juga warning
        // di DrawImagePage yang mendeteksi kondisi ini saat mencetak.
        if (profile.Borderless)
        {
            pageSettings.Margins = new Margins(0, 0, 0, 0);
        }

        // Paper Type / Media Type - System.Drawing.Printing tidak punya abstraksi untuk ini,
        // jadi diterapkan langsung ke DEVMODE mentah lewat NativePrintingInterop. Best-effort:
        // MediaTypeId < 0 ("Driver Default") tidak melakukan apa pun; kegagalan pada driver
        // tertentu di-log tapi tidak menggagalkan print job (paper size/quality tetap jalan).
        if (profile.MediaTypeId >= 0)
        {
            if (!NativePrintingInterop.ApplyMediaType(settings, pageSettings, profile.MediaTypeId, out var mediaTypeError))
            {
                log?.Invoke($"Warning: Paper Type '{profile.MediaTypeName}' gagal diterapkan ke driver " +
                            $"({mediaTypeError ?? "unknown error"}). Job tetap dicetak dengan tipe kertas default driver.");
            }
        }
    }

    /// <summary>
    /// FIX borderless: cari Paper Size driver yang dimensinya cocok dengan target fisik
    /// (targetWidthMm x targetHeightMm, toleransi kecil untuk pembulatan driver) DAN punya
    /// hard margin paling kecil di antara kandidat yang cocok. Ini menggantikan asumsi lama
    /// bahwa "PaperSizeName yang tersimpan di profile pasti benar" - sumber utama laporan
    /// hasil borderless masih kepotong (app sebelumnya bisa saja pakai varian "4x6in" biasa
    /// yang punya hard margin, padahal driver punya varian borderless terpisah).
    /// Tidak hardcode nama per merk printer - murni baca dimensi &amp; HardMarginX/Y asli dari
    /// driver Windows, jadi generik untuk printer apa pun (termasuk Epson L8050).
    /// </summary>
    private static bool TryFindBestBorderlessPaperSize(
        PrinterSettings settings, double targetWidthMm, double targetHeightMm,
        out PaperSize? bestMatch, out double hardMarginXMm, out double hardMarginYMm)
    {
        const double sizeToleranceMm = 3.0; // toleransi pembulatan ukuran kertas vs Print Size

        PaperSize? best = null;
        double bestScore = double.MaxValue;
        double bestHmX = 0, bestHmY = 0;

        foreach (PaperSize size in settings.PaperSizes)
        {
            double wMm = size.Width / 100.0 * PrintSizeCalculator.MmPerInch;
            double hMm = size.Height / 100.0 * PrintSizeCalculator.MmPerInch;

            bool matchesPortrait =
                Math.Abs(wMm - targetWidthMm) <= sizeToleranceMm && Math.Abs(hMm - targetHeightMm) <= sizeToleranceMm;
            bool matchesLandscape =
                Math.Abs(wMm - targetHeightMm) <= sizeToleranceMm && Math.Abs(hMm - targetWidthMm) <= sizeToleranceMm;
            if (!matchesPortrait && !matchesLandscape) continue;

            // PageSettings(PrinterSettings) + assign PaperSize supaya driver menghitung
            // HardMarginX/Y KHUSUS untuk paper size kandidat ini (tidak mencetak apa pun).
            var probe = new PageSettings(settings) { PaperSize = size };
            double hmX = probe.HardMarginX / 100.0 * PrintSizeCalculator.MmPerInch;
            double hmY = probe.HardMarginY / 100.0 * PrintSizeCalculator.MmPerInch;

            // Skor lebih rendah = lebih diprioritaskan. Nama yang eksplisit mengandung
            // "border"/"tanpa tepi" jadi tie-breaker kuat kalau hard margin-nya sama-sama kecil.
            double nameBonus = size.PaperName.Contains("border", StringComparison.OrdinalIgnoreCase) ? -1000 : 0;
            double score = hmX + hmY + nameBonus;

            if (score < bestScore)
            {
                bestScore = score;
                best = size;
                bestHmX = hmX;
                bestHmY = hmY;
            }
        }

        bestMatch = best;
        hardMarginXMm = bestHmX;
        hardMarginYMm = bestHmY;
        return best != null;
    }

    /// <summary>
    /// Menentukan orientasi cetak final untuk satu gambar. Auto (profile.OrientationAuto)
    /// membandingkan lebar vs tinggi gambar - lebih lebar dari tinggi dicetak Landscape,
    /// selain itu Portrait; gambar bujur sangkar (width == height) dicetak Portrait. Selain
    /// Auto, mengikuti pilihan manual profile.Landscape seperti sebelumnya (tidak berubah).
    /// </summary>
    private static bool ResolveLandscape(PrinterProfile? profile, Image image)
    {
        if (profile == null) return false;
        if (profile.OrientationAuto) return image.Width > image.Height;
        return profile.Landscape;
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
        bool landscape = ResolveLandscape(profile, image);

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

        // REVISI KE-2 (setelah dikonfirmasi borderless driver sudah aktif via Windows Printing
        // Preferences, tapi zoom masih terasa berlebihan): app TIDAK LAGI menambahkan bleed
        // sendiri sama sekali (dulu 0.5mm - kecil, tapi tetap menambah zoom di atas zoom yang
        // SUDAH dilakukan driver Epson sendiri untuk borderless-nya - dua sumber zoom
        // bertumpuk). Sekarang app cuma merender TEPAT di ukuran fisik target (mis. 4R =
        // 102x152mm persis, TANPA pembesaran tambahan sama sekali) - identik dengan
        // ScalingMode.ActualSize. Kalau image aspect ratio == target aspect ratio (kasus normal
        // untuk foto photobooth), hasilnya 0% crop dari sisi app. Sisa "zoom" yang terlihat di
        // hasil cetak sepenuhnya berasal dari driver Epson (Printing Preferences > Borderless >
        // tombol Settings... > Amount of Enlargement/Expansion) - itu SATU-SATUNYA tempat yang
        // sekarang mengontrol seberapa banyak pembesaran borderless, kecilkan di sana kalau
        // masih terlalu zoom bagi Anda.
        PrintSizeCalculator.DrawResult result;
        bool usingBorderlessFill = profile?.Borderless == true;

        if (usingBorderlessFill)
        {
            double fillTargetWidthMm = hasPrintSize ? targetWidthMm : pageBoundsMm.Width;
            double fillTargetHeightMm = hasPrintSize ? targetHeightMm : pageBoundsMm.Height;

            double hardMarginXMm = PrintSizeCalculator.HundredthsInchToMm(e.PageSettings.HardMarginX);
            double hardMarginYMm = PrintSizeCalculator.HundredthsInchToMm(e.PageSettings.HardMarginY);

            const double bleedMm = 0.0; // tidak ada bleed tambahan dari app - lihat komentar di atas

            result = PrintSizeCalculator.CalculateBorderlessFill(
                pageBoundsMm, fillTargetWidthMm, fillTargetHeightMm, bleedMm, imageAspectRatio, position);

            log?.Invoke(
                "Borderless: app render TEPAT ukuran fisik target (0 bleed tambahan). " +
                $"ScalingMode '{scaling}' diabaikan selama Borderless dicentang. Zoom borderless " +
                "sepenuhnya dikontrol driver Epson (Printing Preferences > Borderless > Settings...).");

            // Deteksi dini: kalau hard margin driver untuk page settings SAAT INI masih cukup
            // besar, kemungkinan besar toggle Borderless BELUM di-set permanen di Windows
            // Printing Preferences printer ini - app tidak bisa memperbaiki ini sendiri.
            if (hardMarginXMm > 1.0 || hardMarginYMm > 1.0)
            {
                log?.Invoke(
                    $"PERINGATAN PENTING: Hard margin printer saat ini {hardMarginXMm:0.00} x " +
                    $"{hardMarginYMm:0.00} mm - kemungkinan besar mode Borderless BELUM aktif " +
                    "permanen di driver Windows printer ini. Aplikasi TIDAK BISA menyalakan " +
                    "borderless asli secara programatis (flag-nya privat milik driver Epson). " +
                    "Buka: klik kanan printer di Windows > Printing Preferences > centang " +
                    "Borderless > Apply/OK, supaya setting ini permanen untuk semua print job.");
            }
        }
        else
        {
            result = PrintSizeCalculator.Calculate(
                scaling, pageBoundsMm, marginBoundsMm, printableAreaMm,
                targetWidthMm, targetHeightMm, imageAspectRatio, position);

            if (result.ExceedsPrintableArea)
            {
                log?.Invoke(
                    $"Warning: Target print size ({result.TargetWidthMm:0.0} x {result.TargetHeightMm:0.0} mm) " +
                    "melebihi printable area printer. Sebagian gambar mungkin ter-clip oleh printer/driver " +
                    "(ukuran Actual Size TIDAK dikecilkan oleh aplikasi).");
            }
        }

        log?.Invoke(
            "Print - " +
            $"Paper: {e.PageSettings.PaperSize?.PaperName ?? "-"} | " +
            $"Print Size: {(string.IsNullOrWhiteSpace(profile?.PrintSizeName) ? "-" : profile!.PrintSizeName)} | " +
            $"Orientation: {(profile?.OrientationAuto == true ? $"Auto -> {(landscape ? "Landscape" : "Portrait")}" : (landscape ? "Landscape" : "Portrait"))} | " +
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