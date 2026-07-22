namespace PhotoboothPrintServer.Models;

/// <summary>
/// Konfigurasi cetak untuk satu printer tertentu (Fase 3 - STEP 1).
/// Setiap printer yang terdeteksi di sistem punya PrinterProfile sendiri,
/// disimpan terpisah dan tidak hilang saat user berpindah printer aktif.
/// </summary>
public class PrinterProfile
{
    /// <summary>Nama printer persis seperti terdaftar di Windows (kunci profil).</summary>
    public string PrinterName { get; set; } = string.Empty;

    /// <summary>Nama Paper Size persis seperti yang dilaporkan driver (PaperSize.PaperName).</summary>
    public string PaperSizeName { get; set; } = string.Empty;

    public PrintQualityLevel PrintQuality { get; set; } = PrintQualityLevel.High;

    /// <summary>
    /// Best-effort: menghilangkan margin cetak agar mendekati full-bleed.
    /// Hasil akhir tetap tergantung dukungan driver/paper size terhadap borderless.
    /// </summary>
    public bool Borderless { get; set; } = false;

    /// <summary>true = Color, false = Monochrome. Hanya diterapkan jika printer mendukung warna.</summary>
    public bool ColorMode { get; set; } = true;

    /// <summary>true = Landscape, false = Portrait.</summary>
    public bool Landscape { get; set; } = false;
}
