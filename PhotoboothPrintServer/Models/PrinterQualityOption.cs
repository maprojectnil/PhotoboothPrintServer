namespace PhotoboothPrintServer.Models;

/// <summary>Satu opsi kualitas cetak yang benar-benar didukung driver printer.</summary>
public class PrinterQualityOption
{
    public PrintQualityLevel Level { get; set; }

    /// <summary>Nama/keterangan resolusi asli dari driver (mis. "High", "600x600 dpi").</summary>
    public string ResolutionName { get; set; } = string.Empty;
}
