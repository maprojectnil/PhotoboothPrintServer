using PhotoboothPrintServer.Configuration;
using PhotoboothPrintServer.Models;

namespace PhotoboothPrintServer.Services;

/// <summary>
/// Memantau status printer aktif secara berkala (polling) agar UI dan System Tray bisa
/// mendeteksi printer dicabut/offline maupun tersambung kembali secara otomatis,
/// tanpa perlu restart aplikasi maupun aksi manual dari user.
///
/// Tidak pernah throw ke caller - kegagalan satu siklus poll diabaikan dan dicoba lagi
/// di siklus berikutnya, supaya loop watcher tidak pernah berhenti karena error sesaat.
/// </summary>
public class PrinterWatcherService : IDisposable
{
    private readonly PrinterService _printerService;
    private readonly AppSettingsService _settingsService;
    private readonly TimeSpan _interval;

    private CancellationTokenSource? _cts;
    private bool? _lastOnline;

    /// <summary>Dipicu setiap siklus poll berhasil, membawa status printer terbaru (untuk update UI/tray rutin).</summary>
    public event Action<PrinterInfo?>? StatusPolled;

    /// <summary>Dipicu HANYA saat status online/offline printer BERUBAH (edge, bukan tiap poll) - untuk log.</summary>
    public event Action<PrinterInfo>? StatusChanged;

    public PrinterWatcherService(PrinterService printerService, AppSettingsService settingsService, TimeSpan? interval = null)
    {
        _printerService = printerService;
        _settingsService = settingsService;
        _interval = interval ?? TimeSpan.FromSeconds(5);
    }

    public void Start()
    {
        if (_cts != null) return; // sudah berjalan

        _cts = new CancellationTokenSource();
        _ = Task.Run(() => PollLoopAsync(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts = null;
    }

    private async Task PollLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                AppSettings settings = _settingsService.Load();

                PrinterInfo? info = string.IsNullOrWhiteSpace(settings.SelectedPrinter)
                    ? null
                    : _printerService.GetPrinterStatus(settings.SelectedPrinter);

                StatusPolled?.Invoke(info);

                bool currentOnline = info?.IsOnline ?? false;
                if (_lastOnline.HasValue && _lastOnline.Value != currentOnline && info != null)
                {
                    StatusChanged?.Invoke(info);
                }

                _lastOnline = currentOnline;
            }
            catch
            {
                // Diabaikan - dicoba lagi di siklus poll berikutnya.
            }

            try
            {
                await Task.Delay(_interval, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public void Dispose() => Stop();
}
