using System.Drawing;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using PhotoboothPrintServer.Models;
using PhotoboothPrintServer.Utilities;

namespace PhotoboothPrintServer.Services;

/// <summary>
/// Mencetak file gambar ke printer yang dipilih.
/// Fase 1/2: menggunakan konfigurasi default printer (profile == null).
/// Fase 3: menerapkan PrinterProfile (Paper Size, Quality, Borderless, Color, Orientation)
/// menggunakan kemampuan asli driver Windows printer tersebut - opsi yang tidak
/// didukung driver tidak akan dipaksakan.
///
/// Fase 3.1 (bugfix "ukuran hasil cetak tidak sesuai Paper Size"):
/// 1) Paper Size yang tidak lagi cocok dengan driver TIDAK diam-diam diabaikan -
///    job akan gagal dengan pesan jelas, bukan tercetak diam-diam di ukuran default driver.
/// 2) Setelah PageSettings di-set, nilainya di-roundtrip lewat DEVMODE asli Windows
///    (PrinterSettings.GetHdevmode/SetHdevmode) supaya benar-benar dikomit ke driver,
///    bukan cuma properti .NET di memori yang mungkin diabaikan sebagian driver.
/// 3) Borderless: dicoba mencari varian Paper Size "borderless/no-margin" terpisah yang
///    memang disediakan sebagian driver printer foto (DNP/Citizen/Mitsubishi/HiTi), selain
///    tetap menghilangkan margin. Saat borderless aktif, gambar di-"cover fit" (mengisi
///    penuh area cetak, kelebihan di-crop) alih-alih letterbox, supaya benar-benar full-bleed.
/// </summary>
public class ImagePrintService
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr hMem);

    public void PrintImage(
        string printerName,
        string imagePath,
        int copies,
        PrinterProfile? profile = null,
        Action<string>? log = null)
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

            // Pastikan tidak ada dialog / print controller lain yang menimpa PageSettings
            // yang sudah kita siapkan secara eksplisit.
            doc.PrintController = new StandardPrintController();

            var (coverFit, bleedX, bleedY) = ApplyProfile(doc, profile, log);

            // QueryPageSettings dipasang sebagai lapisan pengaman tambahan: beberapa driver
            // (terutama printer foto dye-sub) membangun ulang PageSettings per halaman dari
            // cache internal printer, bukan murni dari DefaultPageSettings. Dengan meng-assign
            // ulang di sini, tiap halaman/copy dijamin memakai PageSettings yang sama persis
            // dengan yang sudah kita terapkan dan komit ke DEVMODE.
            doc.QueryPageSettings += (sender, e) =>
            {
                e.PageSettings.PaperSize = doc.DefaultPageSettings.PaperSize;
                e.PageSettings.Landscape = doc.DefaultPageSettings.Landscape;
                e.PageSettings.Color = doc.DefaultPageSettings.Color;
                e.PageSettings.Margins = doc.DefaultPageSettings.Margins;
                e.PageSettings.PrinterResolution = doc.DefaultPageSettings.PrinterResolution;
            };

            doc.PrintPage += (sender, e) => DrawImagePage(e, image, coverFit, bleedX, bleedY);
            doc.Print();
        }
    }

    /// <summary>
    /// Menerapkan Printer Profile ke PrintDocument. Setiap opsi hanya diterapkan jika
    /// benar-benar tersedia di driver printer yang aktif (tidak dipaksakan).
    /// Mengembalikan (coverFit, bleedX, bleedY):
    /// - coverFit: true jika mode borderless/full-bleed diterapkan (dipakai DrawImagePage
    ///   untuk memilih strategi scaling "cover" vs "contain").
    /// - bleedX/bleedY: area tak-tercetak riil (hardware) di sisi kiri-kanan/atas-bawah
    ///   untuk Paper Size yang dipakai (PageSettings.HardMarginX/Y, dalam per-seratus
    ///   inci - satuan yang sama dengan PrintPageEventArgs.MarginBounds), dibaca LANGSUNG
    ///   dari driver - bukan angka tebakan. Banyak printer inkjet consumer (mis. seri
    ///   Epson EcoTank L3xx/L3xxx) TIDAK punya kapabilitas true 0mm-edge borderless -
    ///   bahkan Paper Size yang secara eksplisit dinamai "borderless/full" tetap memiliki
    ///   pinggiran non-printable beberapa mm karena keterbatasan fisik print head/nozzle
    ///   dekat tepi kertas. Nilai ini dipakai DrawImagePage untuk overscan secukupnya saja
    ///   (sesuai kapabilitas printer sebenarnya), bukan persentase tetap yang sembarangan.
    /// </summary>
    private static (bool coverFit, float bleedX, float bleedY) ApplyProfile(
        PrintDocument doc, PrinterProfile? profile, Action<string>? log)
    {
        if (profile == null)
        {
            log?.Invoke("Printer Profile tidak diset - mencetak dengan pengaturan default driver.");
            return (false, 0f, 0f);
        }

        var settings = doc.PrinterSettings;
        var pageSettings = doc.DefaultPageSettings;
        bool borderlessApplied = false;

        // ---------- Paper Size ----------
        if (!string.IsNullOrWhiteSpace(profile.PaperSizeName))
        {
            PaperSize? matched = FindPaperSize(settings, profile.PaperSizeName);

            // Sebagian driver printer foto menyediakan Paper Size TERPISAH untuk mode
            // borderless (mis. "4x6" vs "4x6(Borderless)"). Kalau ada, pakai varian itu -
            // ini penting karena hanya meng-nol-kan Margins tidak menjamin full-bleed jika
            // driver sendiri masih menganggap ukuran kertas itu versi "ada margin".
            if (profile.Borderless)
            {
                PaperSize? borderlessVariant = FindBorderlessVariant(settings, profile.PaperSizeName);
                if (borderlessVariant != null)
                {
                    matched = borderlessVariant;
                    log?.Invoke(
                        $"Borderless: memakai varian Paper Size '{borderlessVariant.PaperName}' " +
                        $"({FormatMm(borderlessVariant)}) dari driver, bukan '{profile.PaperSizeName}'.");
                }
            }

            if (matched == null)
            {
                var available = string.Join(", ",
                    settings.PaperSizes.Cast<PaperSize>().Select(p => p.PaperName));

                // JANGAN diam-diam jatuh ke ukuran default driver (biasanya Letter/A4) -
                // inilah penyebab utama laporan "ukuran hasil cetak tidak sesuai Paper Size
                // yang dipilih": profil tersimpan tapi driver tidak lagi mengenali namanya
                // (driver di-update / printer diganti / port berubah), sehingga tercetak
                // diam-diam di ukuran lain tanpa ada yang tahu.
                throw new InvalidOperationException(
                    $"Paper Size '{profile.PaperSizeName}' tidak ditemukan pada driver printer aktif " +
                    $"'{settings.PrinterName}'. Paper Size yang tersedia saat ini: " +
                    $"{(available.Length == 0 ? "(tidak ada)" : available)}. " +
                    "Buka Printer Profile dan pilih ulang Paper Size supaya sesuai driver saat ini.");
            }

            pageSettings.PaperSize = matched;
            log?.Invoke(
                $"Paper Size diterapkan: '{matched.PaperName}' ({FormatMm(matched)}, " +
                $"Kind={matched.Kind}).");
        }
        else
        {
            log?.Invoke("Printer Profile tidak menentukan Paper Size - memakai ukuran default driver.");
        }

        // ---------- Orientation ----------
        pageSettings.Landscape = profile.Landscape;

        // ---------- Color Mode - hanya diterapkan jika printer mendukung warna ----------
        if (settings.SupportsColor)
        {
            pageSettings.Color = profile.ColorMode;
        }

        // ---------- Print Quality - cari resolusi driver yang paling sesuai ----------
        var matchedResolution = FindResolution(settings, profile.PrintQuality);
        if (matchedResolution != null)
        {
            pageSettings.PrinterResolution = matchedResolution;
        }
        else
        {
            log?.Invoke(
                $"Print Quality '{profile.PrintQuality}' tidak menemukan resolusi driver yang cocok - " +
                "memakai resolusi default driver.");
        }

        // ---------- Media Type/tipe kertas - tidak ada di PageSettings .NET, dicocokkan
        // di sini dan ditulis ke DEVMODE mentah sekaligus dengan commit di bawah ----------
        PrinterMediaType? matchedMediaType = null;
        if (!string.IsNullOrWhiteSpace(profile.MediaTypeName))
        {
            var mediaTypes = NativePrintingInterop.EnumerateMediaTypes(settings.PrinterName);
            matchedMediaType = mediaTypes.FirstOrDefault(m =>
                string.Equals(m.Name, profile.MediaTypeName, StringComparison.OrdinalIgnoreCase));

            if (matchedMediaType == null)
            {
                var available = string.Join(", ", mediaTypes.Select(m => m.Name));
                log?.Invoke(
                    $"Media Type '{profile.MediaTypeName}' tidak ditemukan pada driver printer " +
                    $"'{settings.PrinterName}'. Tipe yang tersedia: " +
                    $"{(available.Length == 0 ? "(driver tidak mengekspos kapabilitas ini)" : available)}. " +
                    "Melanjutkan cetak dengan Media Type default driver.");
            }
        }

        // ---------- Commit Paper Size/Orientation/Color/Resolution/Media Type ke DEVMODE asli ----------
        // Meng-assign properti PageSettings hanya mengubah objek .NET di memori. Supaya
        // benar-benar dipakai driver Windows saat StartDoc/ResetDC (bukan cuma tampilan di
        // UI), nilai-nilai di atas di-roundtrip lewat DEVMODE asli printer: GetHdevmode()
        // membangun DEVMODE dari pageSettings kita (memvalidasi ke driver), lalu (kalau ada)
        // dmMediaType ditulis langsung ke DEVMODE mentah itu - System.Drawing.Printing tidak
        // punya properti .NET untuk Media Type sama sekali - baru SetHdevmode() menuliskannya
        // balik ke PrinterSettings, sehingga saat PrintDocument.Print() memanggil
        // PrinterSettings.GetHdevmode(DefaultPageSettings) lagi, driver menerima devmode yang
        // sudah tervalidasi dan konsisten (termasuk Media Type).
        // Catatan: Margins BUKAN bagian dari struktur DEVMODE Windows (murni properti GDI+/.NET
        // untuk menghitung MarginBounds), jadi tidak perlu dan tidak boleh ikut di-roundtrip -
        // di-set di bawah, setelah commit devmode, langsung ke objek pageSettings yang sudah
        // dipakai PrintDocument (doc.DefaultPageSettings), tanpa mengganti referensi objeknya.
        IntPtr hDevMode = IntPtr.Zero;
        try
        {
            hDevMode = settings.GetHdevmode(pageSettings);

            if (matchedMediaType != null)
            {
                if (NativePrintingInterop.TryPatchMediaType(hDevMode, matchedMediaType.Id, out var patchError))
                {
                    log?.Invoke($"Media Type diterapkan: '{matchedMediaType.Name}'.");
                }
                else
                {
                    log?.Invoke($"Media Type '{matchedMediaType.Name}' gagal diterapkan ke DEVMODE " +
                                $"({patchError}) - melanjutkan tanpa Media Type khusus.");
                }
            }

            settings.SetHdevmode(hDevMode);
        }
        catch (Exception ex)
        {
            log?.Invoke(
                $"Peringatan: gagal commit pengaturan ke DEVMODE driver ({ex.Message}). " +
                "Melanjutkan dengan PageSettings apa adanya (mungkin tidak sepenuhnya diterapkan driver).");
        }
        finally
        {
            if (hDevMode != IntPtr.Zero) GlobalFree(hDevMode);
        }

        // ---------- Margin cetak ----------
        // Diterapkan SETELAH commit devmode (lihat catatan di atas) supaya tidak tertimpa.
        float bleedX = 0f, bleedY = 0f;

        if (profile.Borderless)
        {
            pageSettings.Margins = new Margins(0, 0, 0, 0);
            borderlessApplied = true;

            // Baca hard margin RIIL printer untuk Paper Size yang sedang dipakai. Banyak
            // printer inkjet consumer (mis. Epson EcoTank seri L3xx/L3xxx) tidak punya
            // kapabilitas true 0mm-edge borderless - bahkan entry Paper Size yang secara
            // eksplisit dinamai "borderless/full" tetap punya pinggiran non-printable
            // beberapa mm (keterbatasan fisik nozzle dekat tepi kertas). Nilai ini dipakai
            // DrawImagePage untuk overscan SECUKUPNYA sesuai kapabilitas printer sebenarnya.
            bleedX = pageSettings.HardMarginX;
            bleedY = pageSettings.HardMarginY;

            if (bleedX > 0.5f || bleedY > 0.5f)
            {
                log?.Invoke(
                    $"Borderless: printer melaporkan area tak-tercetak (hard margin) " +
                    $"{bleedX / 100.0 * 25.4:0.#} x {bleedY / 100.0 * 25.4:0.#} mm bahkan untuk " +
                    "Paper Size ini - true 0mm-edge borderless kemungkinan tidak didukung " +
                    "hardware printer. Gambar akan sedikit di-overscan untuk mengkompensasi.");
            }
        }
        else
        {
            // BUG UTAMA "gambar jauh terlalu kecil dari ukuran kertas": PrintDocument.
            // DefaultPageSettings.Margins TIDAK PERNAH disentuh untuk mode non-borderless,
            // sehingga tetap memakai default WinForms 100/100/100/100 (1 inci di SETIAP
            // sisi) - konstanta generik yang sama sekali tidak memperhitungkan ukuran
            // kertas. Untuk kertas 4x6in (400x600 per-seratus-inci), margin 1 inci di tiap
            // sisi menyisakan area cetak cuma 2x4in dari 4x6in kertas - persis gejala yang
            // dilaporkan. Perbaikan: pakai margin cetak MINIMUM yang benar-benar didukung
            // hardware printer untuk kertas ini (HardMarginX/Y, dilaporkan driver via
            // DEVMODE/DeviceCapabilities), supaya foto memakai area cetak maksimal yang
            // tersedia alih-alih margin default yang sembarangan.
            int marginX = (int)Math.Ceiling(pageSettings.HardMarginX);
            int marginY = (int)Math.Ceiling(pageSettings.HardMarginY);
            pageSettings.Margins = new Margins(marginX, marginY, marginX, marginY);

            log?.Invoke(
                $"Non-borderless: margin cetak diset ke minimum hardware printer " +
                $"({marginX / 100.0 * 25.4:0.#} x {marginY / 100.0 * 25.4:0.#} mm) - " +
                "bukan margin default 1 inci.");
        }

        return (borderlessApplied, bleedX, bleedY);
    }

    /// <summary>
    /// Cari Paper Size persis seperti nama yang tersimpan di profile. Coba exact match dulu,
    /// lalu fallback ke perbandingan yang mengabaikan spasi/kapitalisasi/tanda baca ringan
    /// (nama Paper Size yang dilaporkan driver kadang berubah sedikit antar versi driver).
    /// </summary>
    private static PaperSize? FindPaperSize(PrinterSettings settings, string name)
    {
        foreach (PaperSize size in settings.PaperSizes)
        {
            if (string.Equals(size.PaperName, name, StringComparison.OrdinalIgnoreCase))
                return size;
        }

        string normalizedTarget = Normalize(name);
        foreach (PaperSize size in settings.PaperSizes)
        {
            if (Normalize(size.PaperName) == normalizedTarget)
                return size;
        }

        return null;
    }

    /// <summary>
    /// Cari varian Paper Size "borderless/no-margin/full-bleed" dari nama dasar yang sama,
    /// yang lazim disediakan terpisah oleh driver printer foto dye-sub (DNP/Citizen/
    /// Mitsubishi/HiTi). Best-effort: jika tidak ada, caller tetap pakai Paper Size biasa
    /// dan hanya menghilangkan margin.
    /// </summary>
    private static PaperSize? FindBorderlessVariant(PrinterSettings settings, string baseName)
    {
        string normalizedBase = Normalize(baseName);
        string[] borderlessHints =
        {
            "borderless", "noborder", "nomargin", "no margin", "fullbleed", "full bleed",
            "edgetoedge", "edge to edge", "tanpaborder", "tanpamargin"
        };

        foreach (PaperSize size in settings.PaperSizes)
        {
            string normalizedName = Normalize(size.PaperName);
            bool relatedToBase = normalizedName.Contains(normalizedBase) || normalizedBase.Contains(normalizedName);
            bool looksBorderless = borderlessHints.Any(h => normalizedName.Contains(Normalize(h)));

            if (relatedToBase && looksBorderless)
                return size;
        }

        return null;
    }

    private static string Normalize(string value)
    {
        return string.Concat(value.ToLowerInvariant().Where(c => !char.IsWhiteSpace(c) && c != '-' && c != '_'));
    }

    /// <summary>PaperSize.Width/Height dalam per-seratus inci - dikonversi ke mm untuk logging.</summary>
    private static string FormatMm(PaperSize size)
    {
        double widthMm = size.Width / 100.0 * 25.4;
        double heightMm = size.Height / 100.0 * 25.4;
        return $"{widthMm:0.#} x {heightMm:0.#} mm";
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
    /// Menggambar foto ke halaman cetak.
    /// coverFit=false (default/non-borderless): "contain" - foto di-fit utuh di dalam area
    /// cetak, letterbox jika rasio berbeda (tidak ada yang terpotong).
    /// coverFit=true (borderless): "cover" - foto mengisi PENUH area cetak (fit to page),
    /// kelebihan di-crop di tengah, ditambah overscan sebesar bleedX/bleedY (hard margin
    /// RIIL printer, per-seratus inci - lihat ApplyProfile) supaya area yang secara fisik
    /// tidak bisa dicetak printer (dekat tepi kertas) tidak menyisakan garis putih kosong.
    /// Catatan: kalau printer tidak punya kapabilitas true 0mm-edge borderless (banyak
    /// printer inkjet consumer begitu), akan selalu ada sedikit bagian tepi foto yang tidak
    /// tercetak - itu batas hardware, bukan sesuatu yang bisa dihilangkan lewat software.
    /// </summary>
    private static void DrawImagePage(
        PrintPageEventArgs e, Image image, bool coverFit, float bleedX = 0f, float bleedY = 0f)
    {
        if (e.Graphics == null) return;

        var bounds = e.MarginBounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        double imageRatio = (double)image.Width / image.Height;
        double boundsRatio = (double)bounds.Width / bounds.Height;

        int drawWidth, drawHeight;

        bool imageWiderThanBounds = imageRatio > boundsRatio;

        // coverFit: isi penuh (dimensi terkecil relatif disamakan, sisi lain melebihi lalu
        // di-crop). contain (default): fit di dalam (dimensi terbesar relatif disamakan).
        bool matchByWidth = coverFit ? !imageWiderThanBounds : imageWiderThanBounds;

        if (matchByWidth)
        {
            drawWidth = bounds.Width;
            drawHeight = (int)Math.Round(bounds.Width / imageRatio);
        }
        else
        {
            drawHeight = bounds.Height;
            drawWidth = (int)Math.Round(bounds.Height * imageRatio);
        }

        if (coverFit && (bleedX > 0 || bleedY > 0))
        {
            // Skala minimum supaya sisi yang overscan menutupi bleedX/bleedY di kedua sisi
            // (kiri+kanan / atas+bawah), dipilih dari kebutuhan overscan terbesar antara
            // dua dimensi supaya kedua sisi sama-sama tertutup penuh.
            double scaleX = (bounds.Width + 2.0 * bleedX) / bounds.Width;
            double scaleY = (bounds.Height + 2.0 * bleedY) / bounds.Height;
            double overscan = Math.Max(scaleX, scaleY);

            // Pagar pengaman: printer/driver yang salah lapor HardMarginX/Y sangat besar
            // (driver bermasalah, hardware tidak lazim) tidak boleh membuat overscan
            // membesar liar dan memotong konten foto secara signifikan. Di atas batas ini,
            // lebih baik menyisakan sedikit tepi tak-tercetak daripada merusak foto.
            const double MaxOverscan = 1.10; // maksimum 10% overscan tiap sisi
            overscan = Math.Min(overscan, MaxOverscan);

            drawWidth = (int)Math.Round(drawWidth * overscan);
            drawHeight = (int)Math.Round(drawHeight * overscan);
        }

        int x = bounds.Left + (bounds.Width - drawWidth) / 2;
        int y = bounds.Top + (bounds.Height - drawHeight) / 2;

        if (coverFit)
        {
            // Bagian gambar yang melebihi area cetak (hasil crop + bleed overscan) tidak
            // boleh menggambar di luar bounds halaman - dibatasi dengan clip region.
            var previousClip = e.Graphics.Clip;
            e.Graphics.SetClip(bounds);
            e.Graphics.DrawImage(image, x, y, drawWidth, drawHeight);
            e.Graphics.Clip = previousClip;
        }
        else
        {
            e.Graphics.DrawImage(image, x, y, drawWidth, drawHeight);
        }
    }
}