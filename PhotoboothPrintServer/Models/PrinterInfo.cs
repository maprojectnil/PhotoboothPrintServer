namespace PhotoboothPrintServer.Models;

public class PrinterInfo
{
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsVirtual { get; set; }
    public bool IsOnline { get; set; }
    public bool IsReady { get; set; }
    public string StatusText { get; set; } = "Unknown";
}