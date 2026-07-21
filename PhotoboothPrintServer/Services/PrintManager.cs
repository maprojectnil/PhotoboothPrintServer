using PhotoboothPrintServer.Configuration;
using PhotoboothPrintServer.Models;

namespace PhotoboothPrintServer.Services;

/// <summary>
/// Mengambil job dari PrintQueueService satu per satu (tidak paralel),
/// mencetak menggunakan printer aktif, lalu update status job.
/// Berjalan di background thread selama aplikasi hidup.
/// </summary>
public class PrintManager
{
    private readonly PrintQueueService _queue;
    private readonly AppSettingsService _settingsService;
    private readonly ImagePrintService _imagePrintService = new();
    private readonly SemaphoreSlim _signal = new(0);

    private CancellationTokenSource? _cts;

    public int TotalPrinted { get; private set; }
    public int TotalFailed { get; private set; }
    public PrintJob? CurrentJob { get; private set; }

    /// <summary>Dipicu setiap ada perubahan status job / counter (untuk update UI).</summary>
    public event Action? StateChanged;

    /// <summary>Dipicu untuk pesan log.</summary>
    public event Action<string>? LogMessage;

    public PrintManager(PrintQueueService queue, AppSettingsService settingsService)
    {
        _queue = queue;
        _settingsService = settingsService;
        _queue.QueueChanged += () => _signal.Release();
    }

    public void Start()
    {
        if (_cts != null) return; // sudah berjalan

        _cts = new CancellationTokenSource();
        _ = Task.Run(() => RunLoopAsync(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts = null;
    }

    private async Task RunLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await _signal.WaitAsync(token);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            while (!token.IsCancellationRequested && _queue.TryDequeue(out var job) && job != null)
            {
                await ProcessJobAsync(job);
            }
        }
    }

    private async Task ProcessJobAsync(PrintJob job)
    {
        CurrentJob = job;
        job.Status = PrintJobStatus.Printing;
        job.StartedAt = DateTime.Now;

        LogMessage?.Invoke($"Mencetak {job.JobId} ({job.FileName}) x{job.Copies}...");
        StateChanged?.Invoke();

        try
        {
            AppSettings settings = _settingsService.Load();

            await Task.Run(() =>
                _imagePrintService.PrintImage(settings.SelectedPrinter, job.FilePath, job.Copies));

            job.Status = PrintJobStatus.Completed;
            job.CompletedAt = DateTime.Now;
            TotalPrinted++;

            LogMessage?.Invoke($"{job.JobId} selesai dicetak.");

            TryDeleteTempFile(job.FilePath);
        }
        catch (Exception ex)
        {
            job.Status = PrintJobStatus.Failed;
            job.CompletedAt = DateTime.Now;
            job.ErrorMessage = ex.Message;
            TotalFailed++;

            LogMessage?.Invoke($"{job.JobId} gagal: {ex.Message}");
        }
        finally
        {
            CurrentJob = null;
            StateChanged?.Invoke();
        }
    }

    private static void TryDeleteTempFile(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // Kegagalan hapus file temporary tidak boleh membuat proses crash.
        }
    }
}