namespace PhotoboothPrintServer.Models;

/// <summary>
/// Menentukan bagaimana gambar disesuaikan terhadap target Print Size.
/// Perbaikan physical-size printing: sebelumnya hanya ada satu behavior
/// (letterbox-fit ke <c>PrintPageEventArgs.MarginBounds</c>). Sekarang eksplisit
/// dipisah menjadi 3 mode supaya "Actual Size" benar-benar tidak pernah
/// mengecilkan gambar mengikuti kertas/printable area.
/// </summary>
public enum ScalingMode
{
    /// <summary>
    /// Letterbox-fit ke <c>MarginBounds</c> (perilaku lama, dipertahankan sebagai default).
    /// Nilai 0 sengaja dijadikan default enum ini: profil lama (JSON tanpa field
    /// <c>Scaling</c>) akan otomatis ter-deserialize ke FitToPage, sehingga behavior
    /// existing tidak berubah sama sekali untuk konfigurasi yang sudah tersimpan.
    /// </summary>
    FitToPage = 0,

    /// <summary>
    /// Gambar dirender persis pada ukuran fisik target (Print Size), dikonversi ke mm,
    /// TIDAK mengikuti MarginBounds/PrintableArea/PageBounds. Clipping oleh printer/driver
    /// boleh terjadi jika target melebihi printable area; scaling otomatis TIDAK boleh.
    /// </summary>
    ActualSize = 1,

    /// <summary>
    /// Letterbox-fit eksplisit ke printable area asli printer (<c>PageSettings.PrintableArea</c>),
    /// bukan ke MarginBounds (yang dipengaruhi margin yang diset user/driver).
    /// </summary>
    FitToPrintableArea = 2
}