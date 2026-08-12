namespace PhotoboothPrintServer.Models;

/// <summary>
/// Menentukan di mana target print (hasil Scaling) ditempatkan pada kertas.
/// Untuk ScalingMode.ActualSize, posisi dihitung terhadap FULL page/paper area
/// (bukan printable area) - lihat PrintSizeCalculator.
/// </summary>
public enum PrintPositionMode
{
    /// <summary>Default. Pusatkan target print terhadap full page/paper area.</summary>
    Center = 0,

    /// <summary>Rata kiri-atas terhadap full page/paper area.</summary>
    TopLeft = 1,

    /// <summary>
    /// Offset custom (lihat PrinterProfile.CustomOffsetXMm/CustomOffsetYMm).
    /// Tersedia di model/kalkulasi untuk pengembangan lanjutan; belum diekspos di UI
    /// (lihat catatan di MainForm - hanya Center &amp; TopLeft yang ditampilkan agar
    /// tidak ada UI yang terlihat mendukung fitur padahal belum ada input pendukungnya).
    /// </summary>
    Custom = 2
}