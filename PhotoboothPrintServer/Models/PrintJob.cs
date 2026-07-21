namespace PhotoboothPrintServer.Models;

public class PrintJob
{
    public string JobId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int Copies { get; set; } = 1;
    public PrintJobStatus Status { get; set; } = PrintJobStatus.Queued;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}