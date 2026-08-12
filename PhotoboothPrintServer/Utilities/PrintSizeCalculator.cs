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
}