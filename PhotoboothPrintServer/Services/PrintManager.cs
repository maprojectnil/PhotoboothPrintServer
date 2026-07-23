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
    private readonly PrinterProfileStore _profileStore;
    private readonly PrintHistoryStore _historyStore;
    private readonly PrinterService _printerService;
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

    public PrintManager(
        PrintQueueService queue,
        AppSettingsService settingsService,
        PrinterProfileStore profileStore,
        PrintHistoryStore historyStore,
        PrinterService printerService)
    {
        _queue = queue;
        _settingsService = settingsService;
        _profileStore = profileStore;
        _historyStore = historyStore;
        _printerService = printerService;
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

            while (!token.IsCancellationRequested)
            {
                // Auto Reconnect (Fase 3 - STEP 6): jangan dequeue job jika printer aktif
                // sedang offline/tidak terhubung - job tetap aman menunggu di antrean sampai
                // printer tersambung kembali, bukan langsung digagalkan.
                bool printerReady = await WaitForPrinterOnlineAsync(token);
                if (!printerReady) break; // token dibatalkan saat menunggu (app shutdown)

                if (!_queue.TryDequeue(out var job) || job == null) break;

                await ProcessJobAsync(job);
            }
        }
    }

    /// <summary>
    /// Menunggu sampai printer aktif online/terhubung, mengecek berkala tanpa membebani CPU.
    /// Mengembalikan false hanya jika dibatalkan (mis. aplikasi sedang shutdown).
    /// </summary>
    private async Task<bool> WaitForPrinterOnlineAsync(CancellationToken token)
    {
        bool waitingLogged = false;

        while (!token.IsCancellationRequested)
        {
            AppSettings settings = _settingsService.Load();

            if (string.IsNullOrWhiteSpace(settings.SelectedPrinter))
            {
                if (!waitingLogged)
                {
                    LogMessage?.Invoke("Belum ada printer aktif dipilih. Print Job menunggu dengan aman di antrean...");
                    waitingLogged = true;
                }
            }
            else
            {
                PrinterInfo? info = _printerService.GetPrinterStatus(settings.SelectedPrinter);

                if (info != null && info.IsOnline)
                {
                    if (waitingLogged)
                        LogMessage?.Invoke($"Printer '{settings.SelectedPrinter}' sudah online kembali. Melanjutkan Print Queue.");

                    return true;
                }

                if (!waitingLogged)
                {
                    LogMessage?.Invoke(
                        $"Printer '{settings.SelectedPrinter}' offline / tidak terhubung. " +
                        "Print Job tetap aman di antrean, menunggu printer tersambung kembali...");
                    waitingLogged = true;
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(3), token);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        return false;
    }

    private async Task ProcessJobAsync(PrintJob job)
    {
        CurrentJob = job;
        job.Status = PrintJobStatus.Printing;
        job.StartedAt = DateTime.Now;

        LogMessage?.Invoke($"Mencetak {job.JobId} ({job.FileName}) x{job.Copies}...");
        StateChanged?.Invoke();
        _queue.NotifyStatusChanged(job);

        AppSettings settings = _settingsService.Load();
        PrinterProfile profile = _profileStore.GetOrCreate(settings.SelectedPrinter);

        try
        {
            await Task.Run(() =>
                _imagePrintService.PrintImage(
                    settings.SelectedPrinter,
                    job.FilePath,
                    job.Copies,
                    profile,
                    log: msg => LogMessage?.Invoke($"{job.JobId}: {msg}")));

            job.Status = PrintJobStatus.Completed;
            job.CompletedAt = DateTime.Now;
            TotalPrinted++;

            LogMessage?.Invoke($"{job.JobId} selesai dicetak.");
            _queue.NotifyStatusChanged(job);

            TryDeleteTempFile(job.FilePath);
        }
        catch (Exception ex)
        {
            job.Status = PrintJobStatus.Failed;
            job.CompletedAt = DateTime.Now;
            job.ErrorMessage = ex.Message;
            TotalFailed++;

            LogMessage?.Invoke($"{job.JobId} gagal: {ex.Message}");
            _queue.NotifyStatusChanged(job);
        }
        finally
        {
            SaveHistoryEntry(job, settings.SelectedPrinter, profile);
            CurrentJob = null;
            StateChanged?.Invoke();
        }
    }

    private void SaveHistoryEntry(PrintJob job, string printerName, PrinterProfile profile)
    {
        try
        {
            var entry = new PrintHistoryEntry
            {
                JobId = job.JobId,
                FileName = job.FileName,
                Copies = job.Copies,
                PrinterName = string.IsNullOrWhiteSpace(printerName) ? "(tidak ada)" : printerName,
                ProfileSummary = BuildProfileSummary(profile),
                Status = job.Status,
                CreatedAt = job.CreatedAt,
                StartedAt = job.StartedAt,
                CompletedAt = job.CompletedAt,
                ErrorMessage = job.ErrorMessage
            };

            _historyStore.Add(entry);
        }
        catch
        {
            // Kegagalan mencatat history tidak boleh mengganggu proses print yang sudah selesai.
        }
    }

    private static string BuildProfileSummary(PrinterProfile profile)
    {
        string paper = string.IsNullOrWhiteSpace(profile.PaperSizeName) ? "-" : profile.PaperSizeName;
        string color = profile.ColorMode ? "Color" : "Monochrome";
        string orientation = profile.Landscape ? "Landscape" : "Portrait";
        string borderless = profile.Borderless ? ", Borderless" : "";

        return $"{paper} | {profile.PrintQuality} | {color} | {orientation}{borderless}";
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