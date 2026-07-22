namespace PhotoboothPrintServer.Models;

/// <summary>
/// Kemampuan aktual sebuah printer, diambil langsung dari driver Windows
/// (System.Drawing.Printing.PrinterSettings). Digunakan untuk membatasi
/// pilihan Printer Profile di UI agar tidak menawarkan opsi yang tidak
/// didukung printer tersebut.
/// </summary>
public class PrinterCapabilities
{
    public List<string> PaperSizes { get; set; } = new();
    public bool SupportsColor { get; set; }
    public List<PrinterQualityOption> QualityOptions { get; set; } = new();
}
