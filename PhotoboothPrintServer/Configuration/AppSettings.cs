namespace PhotoboothPrintServer.Configuration;

public class AppSettings
{
    public string SelectedPrinter { get; set; } = string.Empty;
    public int ApiPort { get; set; } = 8080;
}