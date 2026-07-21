using System.Collections.Concurrent;
using PhotoboothPrintServer.Models;

namespace PhotoboothPrintServer.Services;

/// <summary>
/// Menyimpan antrean print job secara thread-safe.
/// Dipanggil dari thread HTTP (Kestrel) saat menerima job baru,
/// dan dari PrintManager saat memproses job.
/// </summary>
public class PrintQueueService
{
    private readonly ConcurrentQueue<PrintJob> _pending = new();
    private readonly ConcurrentDictionary<string, PrintJob> _allJobs = new();
    private int _counter = 0;

    /// <summary>Dipicu setiap ada job baru masuk antrean.</summary>
    public event Action? QueueChanged;

    /// <summary>Dipicu untuk pesan log (mis. "Job JOB-001 diterima").</summary>
    public event Action<string>? LogMessage;

    public PrintJob Enqueue(string fileName, string filePath, int copies)
    {
        int number = Interlocked.Increment(ref _counter);
        string jobId = $"JOB-{number:D3}";

        var job = new PrintJob
        {
            JobId = jobId,
            FileName = fileName,
            FilePath = filePath,
            Copies = copies,
            Status = PrintJobStatus.Queued,
            CreatedAt = DateTime.Now
        };

        _allJobs[jobId] = job;
        _pending.Enqueue(job);

        LogMessage?.Invoke($"Job {jobId} diterima: {fileName} ({copies}x copy).");
        QueueChanged?.Invoke();

        return job;
    }

    public bool TryDequeue(out PrintJob? job) => _pending.TryDequeue(out job);

    public PrintJob? GetJob(string jobId) =>
        _allJobs.TryGetValue(jobId, out var job) ? job : null;

    public int PendingCount => _pending.Count;

    public List<PrintJob> GetAllJobs() =>
        _allJobs.Values.OrderBy(j => j.CreatedAt).ToList();
}