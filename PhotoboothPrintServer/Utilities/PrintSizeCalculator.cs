using PhotoboothPrintServer.Models;

namespace PhotoboothPrintServer.Utilities;

/// <summary>
/// Kalkulasi murni (tanpa dependency ke System.Drawing.Printing) untuk menentukan
/// ukuran &amp; posisi rendering cetak, berbasis milimeter. Dipisah dari ImagePrintService
/// supaya logikanya bisa diuji dengan unit test biasa tanpa perlu PrintDocument/Graphics
/// (yang butuh Windows + printer nyata).
///
/// PENTING: PrintPageEventArgs.PageBounds, MarginBounds, dan PageSettings.PrintableArea
/// SELALU dinyatakan dalam hundredths of an inch (1/100 inci) oleh .NET, TERLEPAS dari
/// Graphics.PageUnit yang sedang aktif. Caller (ImagePrintService) bertanggung jawab
/// mengonversi nilai-nilai itu ke mm (lihat HundredthsInchToMm) sebelum memanggil kelas ini,
/// dan mengatur Graphics.PageUnit = Millimeter sebelum DrawImage supaya rectangle mm yang
/// dihasilkan di sini bisa dipakai langsung sebagai koordinat gambar.
/// </summary>
public static class PrintSizeCalculator
{
    public const double MmPerInch = 25.4;

    /// <summary>Konversi dari hundredths of an inch (satuan asli PrintPageEventArgs) ke mm.</summary>
    public static double HundredthsInchToMm(double hundredthsInch) => hundredthsInch / 100.0 * MmPerInch;

    /// <summary>Rectangle sederhana dalam mm (independen dari System.Drawing).</summary>
    public readonly record struct RectMm(double X, double Y, double Width, double Height);

    public readonly record struct PositionParams(PrintPositionMode Mode, double CustomOffsetXMm = 0, double CustomOffsetYMm = 0);

    public readonly record struct DrawResult(
        RectMm DrawRect,
        double TargetWidthMm,
        double TargetHeightMm,
        bool ExceedsPrintableArea);

    /// <summary>
    /// Menentukan ukuran target Print Size sesuai orientasi. Orientation HANYA memutar
    /// target print (menukar width/height) - tidak pernah membuat target berubah menjadi
    /// ukuran lain karena mengikuti Paper Size.
    /// Contoh: 4R Portrait = 102x152mm, 4R Landscape = 152x102mm.
    /// </summary>
    public static (double WidthMm, double HeightMm) GetOrientedTargetSize(
        double printWidthMm, double printHeightMm, bool landscape)
    {
        return landscape ? (printHeightMm, printWidthMm) : (printWidthMm, printHeightMm);
    }

    /// <summary>
    /// Entry point utama. Memilih strategi kalkulasi berdasarkan ScalingMode, tanpa
    /// mencampur behavior antar mode (lihat audit butir 4).
    /// </summary>
    /// <param name="scaling">Mode scaling yang dipilih user.</param>
    /// <param name="pageBoundsMm">Full page/paper area (mm) - dipakai sebagai acuan Position untuk ActualSize.</param>
    /// <param name="marginBoundsMm">MarginBounds (mm) - dipakai untuk FitToPage (behavior lama).</param>
    /// <param name="printableAreaMm">Printable area asli printer (mm) - dipakai untuk FitToPrintableArea,
    /// dan untuk deteksi warning clipping pada ActualSize.</param>
    /// <param name="targetWidthMm">Target lebar fisik (mm) sudah memperhitungkan orientasi. Hanya dipakai untuk ActualSize.</param>
    /// <param name="targetHeightMm">Target tinggi fisik (mm) sudah memperhitungkan orientasi. Hanya dipakai untuk ActualSize.</param>
    /// <param name="imageAspectRatio">image.Width / image.Height - hanya dipakai untuk FitToPage/FitToPrintableArea.</param>
    /// <param name="position">Mode posisi &amp; offset custom.</param>
    public static DrawResult Calculate(
        ScalingMode scaling,
        RectMm pageBoundsMm,
        RectMm marginBoundsMm,
        RectMm printableAreaMm,
        double targetWidthMm,
        double targetHeightMm,
        double imageAspectRatio,
        PositionParams position)
    {
        return scaling switch
        {
            ScalingMode.ActualSize => CalculateActualSize(pageBoundsMm, printableAreaMm, targetWidthMm, targetHeightMm, position),
            ScalingMode.FitToPrintableArea => CalculateFitToBounds(printableAreaMm, imageAspectRatio, position),
            _ => CalculateFitToBounds(marginBoundsMm, imageAspectRatio, position), // FitToPage = behavior lama
        };
    }

    /// <summary>
    /// ActualSize: ukuran output = target fisik persis (mm), TIDAK PERNAH dikecilkan oleh
    /// MarginBounds/PrintableArea/PageBounds. Posisi dihitung terhadap full page (paper),
    /// bukan printable area. Jika target melebihi printable area, ExceedsPrintableArea=true
    /// (caller boleh log warning) tapi ukuran TIDAK diubah - clipping dibiarkan terjadi di
    /// level printer/driver, bukan di-scale oleh aplikasi.
    /// </summary>
    public static DrawResult CalculateActualSize(
        RectMm pageBoundsMm,
        RectMm printableAreaMm,
        double targetWidthMm,
        double targetHeightMm,
        PositionParams position)
    {
        var (x, y) = ComputePosition(pageBoundsMm, targetWidthMm, targetHeightMm, position);

        bool exceeds =
            targetWidthMm > printableAreaMm.Width + 0.01 ||
            targetHeightMm > printableAreaMm.Height + 0.01 ||
            x < printableAreaMm.X - 0.01 ||
            y < printableAreaMm.Y - 0.01 ||
            (x + targetWidthMm) > (printableAreaMm.X + printableAreaMm.Width + 0.01) ||
            (y + targetHeightMm) > (printableAreaMm.Y + printableAreaMm.Height + 0.01);

        return new DrawResult(new RectMm(x, y, targetWidthMm, targetHeightMm), targetWidthMm, targetHeightMm, exceeds);
    }

    /// <summary>
    /// Letterbox-fit (aspect-fit, tidak gepeng) gambar ke dalam <paramref name="bounds"/>.
    /// Dipakai untuk FitToPage (bounds = MarginBounds) dan FitToPrintableArea
    /// (bounds = PageSettings.PrintableArea) - matematikanya sama, hanya bounds-nya beda,
    /// sesuai audit butir 4 ("jangan mencampur ketiga mode").
    /// </summary>
    public static DrawResult CalculateFitToBounds(RectMm bounds, double imageAspectRatio, PositionParams position)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0 || imageAspectRatio <= 0)
            return new DrawResult(bounds, Math.Max(bounds.Width, 0), Math.Max(bounds.Height, 0), false);

        double boundsRatio = bounds.Width / bounds.Height;

        double drawWidth, drawHeight;
        if (imageAspectRatio > boundsRatio)
        {
            drawWidth = bounds.Width;
            drawHeight = bounds.Width / imageAspectRatio;
        }
        else
        {
            drawHeight = bounds.Height;
            drawWidth = bounds.Height * imageAspectRatio;
        }

        var (x, y) = ComputePosition(bounds, drawWidth, drawHeight, position);
        return new DrawResult(new RectMm(x, y, drawWidth, drawHeight), drawWidth, drawHeight, false);
    }

    private static (double X, double Y) ComputePosition(RectMm reference, double drawWidth, double drawHeight, PositionParams position)
    {
        return position.Mode switch
        {
            PrintPositionMode.TopLeft => (reference.X, reference.Y),
            PrintPositionMode.Custom => (reference.X + position.CustomOffsetXMm, reference.Y + position.CustomOffsetYMm),
            _ => (reference.X + (reference.Width - drawWidth) / 2.0, reference.Y + (reference.Height - drawHeight) / 2.0),
        };
    }

    /// <summary>
    /// FIX borderless: "cover + bleed", BUKAN letterbox. Dipakai HANYA saat
    /// PrinterProfile.Borderless == true, menggantikan (override) ScalingMode apa pun yang
    /// dipilih user, karena tujuannya berbeda secara fundamental dari FitToPage/
    /// FitToPrintableArea/ActualSize:
    ///
    /// - FitToPage/FitToPrintableArea = "contain" (letterbox): gambar SELALU utuh terlihat,
    ///   tapi kalau aspect ratio tidak pas -&gt; muncul tepi putih. Tidak cocok untuk borderless.
    /// - ActualSize = ukuran fisik persis, tidak pernah dikecilkan; kalau melebihi printable
    ///   area (hard margin printer/driver), sisanya betul-betul di-clip oleh printer -&gt;
    ///   itulah penyebab "kepotong" yang dilaporkan.
    /// - CalculateBorderlessFill = "cover": gambar diperbesar sampai MENUTUPI PENUH target
    ///   area (targetAreaMm) DITAMBAH bleed (mm) di semua sisi. Bleed sengaja dibuat lebih
    ///   besar dari sisa hard margin driver (lihat caller di ImagePrintService), jadi bagian
    ///   yang "dipotong" oleh hardware printer PASTI cuma bleed (area ekstra yang sengaja
    ///   digambar melebihi target), TIDAK PERNAH memakan konten foto inti di dalam
    ///   targetAreaMm. Efek samping: kalau aspect ratio foto beda dari target (mis. foto 3:4
    ///   dicetak ke kertas 2:3), sedikit sisi foto ikut ter-crop supaya tidak ada tepi putih -
    ///   ini standar cetak borderless di percetakan manapun, bukan bug.
    /// </summary>
    /// <param name="pageBoundsMm">Full page/paper area (mm), acuan penempatan target (sama seperti ActualSize).</param>
    /// <param name="targetWidthMm">Lebar fisik target print (mm), sudah memperhitungkan orientasi.</param>
    /// <param name="targetHeightMm">Tinggi fisik target print (mm), sudah memperhitungkan orientasi.</param>
    /// <param name="bleedMm">
    /// Ekstra ukuran (mm) di SETIAP sisi di luar targetWidthMm/targetHeightMm. Caller wajib
    /// mengisinya berdasarkan hard margin printer yang terukur (lihat ImagePrintService.
    /// DrawImagePage) + toleransi kecil, supaya area yang di-clip printer selalu berada di
    /// dalam bleed, bukan di dalam target asli.
    /// </param>
    public static DrawResult CalculateBorderlessFill(
        RectMm pageBoundsMm, double targetWidthMm, double targetHeightMm, double bleedMm,
        double imageAspectRatio, PositionParams position)
    {
        // Posisi target dasar (sebelum bleed) dihitung sama persis seperti ActualSize -
        // supaya perilaku Position (Center/TopLeft/Custom) konsisten antar mode.
        var (targetX, targetY) = ComputePosition(pageBoundsMm, targetWidthMm, targetHeightMm, position);

        var bleedArea = new RectMm(
            targetX - bleedMm,
            targetY - bleedMm,
            targetWidthMm + bleedMm * 2.0,
            targetHeightMm + bleedMm * 2.0);

        if (bleedArea.Width <= 0 || bleedArea.Height <= 0 || imageAspectRatio <= 0)
            return new DrawResult(bleedArea, Math.Max(bleedArea.Width, 0), Math.Max(bleedArea.Height, 0), false);

        double boundsRatio = bleedArea.Width / bleedArea.Height;

        // COVER (kebalikan dari CalculateFitToBounds yang "contain"): gambar dibesarkan
        // sampai sisi TERPENDEKnya menutupi bleedArea, sisi lain melebihi bleedArea (nanti
        // otomatis terpotong oleh GDI/printer di luar halaman - itu memang tujuannya).
        double drawWidth, drawHeight;
        if (imageAspectRatio > boundsRatio)
        {
            drawHeight = bleedArea.Height;
            drawWidth = bleedArea.Height * imageAspectRatio;
        }
        else
        {
            drawWidth = bleedArea.Width;
            drawHeight = bleedArea.Width / imageAspectRatio;
        }

        var (x, y) = ComputePosition(bleedArea, drawWidth, drawHeight, position);
        return new DrawResult(new RectMm(x, y, drawWidth, drawHeight), targetWidthMm, targetHeightMm, false);
    }
}