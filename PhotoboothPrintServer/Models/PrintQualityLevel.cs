namespace PhotoboothPrintServer.Models;

/// <summary>
/// Level kualitas cetak yang ditawarkan ke user di UI.
/// Level ini dipetakan ke PrinterResolution asli dari driver Windows
/// printer yang aktif (lihat ImagePrintService.FindResolution).
/// </summary>
public enum PrintQualityLevel
{
    Draft,
    Normal,
    High
}
