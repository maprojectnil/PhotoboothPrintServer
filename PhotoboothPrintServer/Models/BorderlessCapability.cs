namespace PhotoboothPrintServer.Models;

/// <summary>
/// Hasil cek kapabilitas borderless suatu printer untuk Paper Size tertentu, diambil
/// LANGSUNG dari driver Windows (PageSettings.HardMarginX/Y) tanpa perlu mencetak apa pun.
/// Dipakai untuk memberi tahu operator SEBELUM sesi photobooth berjalan, bukan baru
/// ketahuan dari log setelah hasil cetak sudah terlanjur salah.
/// </summary>
public class BorderlessCapability
{
    public bool PaperSizeFound { get; set; }

    /// <summary>Area tak-tercetak (hard margin) kiri-kanan, dalam mm.</summary>
    public double HardMarginXMm { get; set; }

    /// <summary>Area tak-tercetak (hard margin) atas-bawah, dalam mm.</summary>
    public double HardMarginYMm { get; set; }

    /// <summary>
    /// true jika hard margin cukup kecil (&lt;0.5mm) untuk dianggap true borderless -
    /// ambang batas ini sama dengan yang dipakai ImagePrintService untuk logging.
    /// </summary>
    public bool LikelyTrueBorderless => PaperSizeFound && HardMarginXMm < 0.5 && HardMarginYMm < 0.5;
}