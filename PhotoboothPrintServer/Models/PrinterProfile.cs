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

    // ===================== Physical Print Size (perbaikan printing) =====================

    /// <summary>
    /// Nama preset Print Size (3R/4R/5R/6R/A4/A5/Custom) - lihat PrintSizeProfile.Presets.
    /// Kosong = belum pernah diset user. Ini BEDA dengan PaperSizeName: Paper Size adalah
    /// kertas fisik di printer, Print Size adalah ukuran gambar yang ingin dicetak di atasnya.
    /// </summary>
    public string PrintSizeName { get; set; } = string.Empty;

    /// <summary>
    /// Lebar target cetak fisik dalam milimeter (orientasi Portrait profil ini).
    /// Sumber kebenaran untuk ScalingMode.ActualSize - BUKAN diturunkan dari pixel gambar
    /// atau dari Paper Size. 0 berarti belum diset (lihat PrintSizeName).
    /// </summary>
    public double PrintWidthMm { get; set; }

    /// <summary>Tinggi target cetak fisik dalam milimeter (orientasi Portrait profil ini).</summary>
    public double PrintHeightMm { get; set; }

    /// <summary>
    /// Bagaimana gambar disesuaikan terhadap Print Size / area cetak.
    /// PENTING untuk backward compatibility: default-nya SENGAJA FitToPage (nilai enum 0),
    /// sama seperti behavior lama, supaya profil yang sudah tersimpan sebelum fitur ini
    /// ada (JSON tanpa field "Scaling") otomatis ter-deserialize ke FitToPage - tidak ada
    /// perubahan hasil cetak untuk konfigurasi existing.
    /// </summary>
    public ScalingMode Scaling { get; set; } = ScalingMode.FitToPage;

    /// <summary>Di mana target print ditempatkan pada kertas. Default Center.</summary>
    public PrintPositionMode Position { get; set; } = PrintPositionMode.Center;

    /// <summary>Offset X (mm) dari top-left halaman, hanya dipakai jika Position = Custom.</summary>
    public double CustomOffsetXMm { get; set; }

    /// <summary>Offset Y (mm) dari top-left halaman, hanya dipakai jika Position = Custom.</summary>
    public double CustomOffsetYMm { get; set; }
}