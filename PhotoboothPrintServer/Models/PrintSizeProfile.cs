namespace PhotoboothPrintServer.Models;

/// <summary>
/// Target ukuran fisik cetak (Print Size) - terpisah dari Paper Size.
/// Paper Size = ukuran kertas fisik yang dimasukkan ke printer (mis. A4).
/// Print Size  = ukuran fisik gambar yang ingin dicetak di atas kertas itu (mis. 4R).
/// Nilai fisiknya SELALU tersedia sebagai mm numerik (WidthMm/HeightMm), bukan
/// hanya sebagai string nama, supaya bisa dipakai langsung untuk kalkulasi rendering.
/// </summary>
public class PrintSizeProfile
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Lebar fisik dalam orientasi "portrait" preset ini, dalam milimeter.</summary>
    public double WidthMm { get; set; }

    /// <summary>Tinggi fisik dalam orientasi "portrait" preset ini, dalam milimeter.</summary>
    public double HeightMm { get; set; }

    /// <summary>Nama khusus untuk opsi "ukuran custom" (user mengisi mm sendiri).</summary>
    public const string CustomName = "Custom";

    /// <summary>
    /// Preset ukuran cetak umum untuk photobooth. Ukuran fisik 4R = 102 x 152 mm
    /// (4 x 6 inci) - ini yang paling penting dan wajib benar.
    /// </summary>
    public static readonly IReadOnlyList<PrintSizeProfile> Presets = new List<PrintSizeProfile>
    {
        new() { Name = "3R", WidthMm = 89,  HeightMm = 127 },   // 3.5 x 5 in
        new() { Name = "4R", WidthMm = 102, HeightMm = 152 },   // 4 x 6 in
        new() { Name = "5R", WidthMm = 127, HeightMm = 178 },   // 5 x 7 in
        new() { Name = "6R", WidthMm = 152, HeightMm = 203 },   // 6 x 8 in
        new() { Name = "A4", WidthMm = 210, HeightMm = 297 },
        new() { Name = "A5", WidthMm = 148, HeightMm = 210 },
    }.AsReadOnly();

    /// <summary>Cari preset berdasarkan nama (case-insensitive). Null jika tidak ditemukan (mis. "Custom").</summary>
    public static PrintSizeProfile? FindPreset(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return Presets.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}