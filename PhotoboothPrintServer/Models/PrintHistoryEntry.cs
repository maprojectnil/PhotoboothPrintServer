namespace PhotoboothPrintServer.Models;

/// <summary>
/// Satu catatan riwayat print job yang sudah selesai diproses (Completed atau Failed).
/// Dibuat oleh PrintManager setelah job selesai dicetak, berisi konfigurasi yang
/// benar-benar dipakai saat itu (printer + profil), bukan hanya referensi job aktif.
/// </summary>
public class PrintHistoryEntry
{
    public string JobId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int Copies { get; set; } = 1;

    /// <summary>Nama printer yang dipakai saat itu.</summary>
    public string PrinterName { get; set; } = string.Empty;

    /// <summary>Ringkasan Printer Profile yang dipakai saat itu (paper size, quality, color, orientation).</summary>
    public string ProfileSummary { get; set; } = string.Empty;

    public PrintJobStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public string? ErrorMessage { get; set; }
}
